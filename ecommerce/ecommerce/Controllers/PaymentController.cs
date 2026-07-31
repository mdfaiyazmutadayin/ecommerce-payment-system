using BLL;
using BLL.DTOs;
using Newtonsoft.Json.Linq;
using Stripe;
using Stripe.Checkout;
using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace ecommerce.Controllers
{
    [RoutePrefix("api/Payment")]
    public class PaymentController : ApiController
    {
        [HttpPost]
        [Route("checkout/{orderId}")]
        public async Task<HttpResponseMessage> Checkout(int orderId, CheckoutDto dto)
        {
            if (dto == null) return Request.CreateResponse(HttpStatusCode.BadRequest, "Request body is required");

            try
            {
                var (transactionId, provider, rawResponse) = await ServiceFactory.PaymentData().CheckoutAsync(orderId, dto.Provider);

                string redirectUrl = null;
                if (provider.ToLower() == "bkash")
                {
                    var json = JObject.Parse(rawResponse);
                    redirectUrl = json["bkashURL"]?.ToString();
                }
                else if (provider.ToLower() == "stripe")
                {
                    var json = JObject.Parse(rawResponse);
                    redirectUrl = json["url"]?.ToString();
                }

                var result = new CheckoutResponseDto
                {
                    Provider = provider,
                    TransactionId = transactionId,
                    Status = "pending",
                    RedirectUrl = redirectUrl
                };
                return Request.CreateResponse(HttpStatusCode.OK, result);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }

        [HttpPost]
        [Route("confirm")]
        public async Task<HttpResponseMessage> Confirm(string provider, string transactionId)
        {
            try
            {
                if (provider.ToLower() == "stripe")
                {
                    var status = await ServiceFactory.PaymentData().GetPaymentStatusAsync(provider, transactionId);
                    var result = new PaymentConfirmResponseDto
                    {
                        Success = status == "success",
                        Provider = provider,
                        TransactionId = transactionId,
                        PaymentStatus = status,
                        OrderStatus = status == "success" ? "paid" : "pending"
                    };
                    return Request.CreateResponse(HttpStatusCode.OK, result);
                }

                var success = await ServiceFactory.PaymentData().ConfirmPaymentAsync(provider, transactionId);
                var resultBkash = new PaymentConfirmResponseDto
                {
                    Success = success,
                    Provider = provider,
                    TransactionId = transactionId,
                    PaymentStatus = success ? "success" : "failed",
                    OrderStatus = success ? "paid" : "pending"
                };
                return Request.CreateResponse(HttpStatusCode.OK, resultBkash);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }

        [HttpGet]
        [Route("bkash-callback")]
        public async Task<HttpResponseMessage> BkashCallback(string paymentID, string status)
        {
            if (string.IsNullOrEmpty(paymentID))
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Missing paymentID");

            try
            {
                if (status == "success")
                {
                    var confirmed = await ServiceFactory.PaymentData().ConfirmPaymentAsync("bkash", paymentID);
                    return Request.CreateResponse(HttpStatusCode.OK, new { paymentID, confirmed });
                }

                await ServiceFactory.PaymentData().MarkFailedAsync("bkash", paymentID);
                return Request.CreateResponse(HttpStatusCode.OK, new { paymentID, status = "failed" });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }

        [HttpPost]
        [Route("stripe-webhook")]
        public async Task<HttpResponseMessage> StripeWebhook()
        {
            var json = await Request.Content.ReadAsStringAsync();
            var signatureHeader = Request.Headers.Contains("Stripe-Signature")
                ? Request.Headers.GetValues("Stripe-Signature").FirstOrDefault()
                : null;

            if (signatureHeader == null)
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Missing Stripe-Signature header");

            var webhookSecret = ConfigurationManager.AppSettings["Stripe:WebhookSecret"];

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(json, signatureHeader, webhookSecret);

                // ─────────────────────────────────────────────────────────────
                // BUG 1 FIX: Was listening for "payment_intent.succeeded" but
                // Stripe Checkout fires "checkout.session.completed" instead.
                // payment_intent.succeeded never fires for Checkout Sessions,
                // so ConfirmByProviderTransactionAsync was NEVER called.
                // ─────────────────────────────────────────────────────────────
                if (stripeEvent.Type == "checkout.session.completed")
                {
                    var session = stripeEvent.Data.Object as Session;

                    // ─────────────────────────────────────────────────────────
                    // BUG 2 FIX: Was passing intent.Id (pi_xxx — PaymentIntent
                    // ID) but your DB stores session.Id (cs_xxx — Session ID)
                    // from CheckoutAsync. The lookup returned null every time,
                    // so stock was never touched.
                    // ─────────────────────────────────────────────────────────
                    if (session?.PaymentStatus == "paid")
                    {
                        await ServiceFactory.PaymentData()
                            .ConfirmByProviderTransactionAsync("stripe", session.Id); // cs_xxx
                    }
                }
                else if (stripeEvent.Type == "checkout.session.async_payment_failed")
                {
                    // Handle async payment failure (e.g. bank redirects)
                    var session = stripeEvent.Data.Object as Session;
                    if (session != null)
                        await ServiceFactory.PaymentData().MarkFailedAsync("stripe", session.Id);
                }

                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (StripeException ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }
    }
}