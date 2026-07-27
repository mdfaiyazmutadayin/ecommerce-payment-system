using BLL;
using BLL.DTOs;
using Newtonsoft.Json.Linq;
using Stripe;
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
                // Call the service layer – it should now return a redirect URL for Stripe as well.
                var (transactionId, provider, rawResponse) = await ServiceFactory.PaymentData().CheckoutAsync(orderId, dto.Provider);

                string redirectUrl = null;
                // For bKash, extract the bKash payment URL from rawResponse.
                if (provider.ToLower() == "bkash")
                {
                    var json = JObject.Parse(rawResponse);
                    redirectUrl = json["bkashURL"]?.ToString();
                }
                // For Stripe, the service should already provide the Checkout Session URL in rawResponse.
                // We'll assume rawResponse contains a "url" field for Stripe.
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
                    RedirectUrl = redirectUrl   // Now always populated for both providers
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
                // This endpoint is now used ONLY for bKash (or other non‑redirect providers).
                // For Stripe, the webhook will automatically update order status, so we could
                // optionally return a "not supported" message if provider == "stripe".
                if (provider.ToLower() == "stripe")
                {
                    // Instead of a manual confirm, you can query the Payment Intent status
                    // and return the current status (but this is not a confirmation action).
                    // We'll keep it as a status check for Stripe if needed.
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

                // For bKash, we actually confirm the payment.
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

                // status will be "failure" or "cancel" per bKash's docs
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

                if (stripeEvent.Type == "payment_intent.succeeded")
                {
                    var intent = stripeEvent.Data.Object as PaymentIntent;
                    await ServiceFactory.PaymentData().ConfirmByProviderTransactionAsync("stripe", intent.Id);
                }
                else if (stripeEvent.Type == "payment_intent.payment_failed")
                {
                    var intent = stripeEvent.Data.Object as PaymentIntent;
                    await ServiceFactory.PaymentData().MarkFailedAsync("stripe", intent.Id);
                }

                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (StripeException ex)
            {
                // Signature invalid, or malformed payload — reject, don't process
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }
    }
}