using DAL.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Stripe;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DAL
{
    public class PaymentStrategyFactory : IPaymentStrategyFactory
    {
        public IPaymentStrategy GetStrategy(string provider)
        {
            switch (provider.ToLower())
            {
                case "stripe": return new StripeStrategy();
                case "bkash": return new BkashStrategy();
                default: throw new NotSupportedException($"Unknown provider '{provider}'");
            }
        }
    }

    // ============================================================
    // STRIPE — real calls via Stripe.net SDK, test mode.
    // Uses Stripe's test payment method token so it can confirm
    // synchronously without a frontend card form.
    // ============================================================
    public class StripeStrategy : IPaymentStrategy
    {
        private readonly SessionService _sessionService;

        public StripeStrategy()
        {
            StripeConfiguration.ApiKey = ConfigurationManager.AppSettings["Stripe:SecretKey"];
            _sessionService = new SessionService();
        }

        public async Task<(string transactionId, string rawResponse)> CheckoutAsync(decimal amount)
        {
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Order Payment",
                        },
                        UnitAmount = (long)(amount * 100), // convert to cents
                    },
                    Quantity = 1,
                }
            },
                Mode = "payment",
                // ⚠️ IMPORTANT: Replace these URLs with your real frontend URLs
                SuccessUrl = "https://ecommerce-payment-system.vercel.app/order-success?session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = "https://ecommerce-payment-system.vercel.app/order-cancel",
            };

            var session = await _sessionService.CreateAsync(options);
            var rawResponse = $"{{\"url\":\"{session.Url}\"}}"; // For the controller to extract

            return (session.Id, rawResponse);
        }

        // These two methods are not used for Stripe (webhooks handle confirmation),
        // but they are required by the interface. They can simply return the session status.
        public async Task<bool> ExecutePaymentAsync(string transactionId)
        {
            var session = await _sessionService.GetAsync(transactionId);
            return session.PaymentStatus == "paid";
        }

        public async Task<bool> QueryPaymentAsync(string transactionId)
        {
            var session = await _sessionService.GetAsync(transactionId);
            return session.PaymentStatus == "paid";
        }
    }

    // ============================================================
    // BKASH — real calls to bKash Tokenized Checkout sandbox
    // (v1.2.0-beta). Grant Token -> Create Payment -> Execute -> Query.
    // ============================================================
    public class BkashStrategy : IPaymentStrategy
    {
        private static readonly HttpClient _http = new HttpClient();
        private readonly string _baseUrl = ConfigurationManager.AppSettings["Bkash:BaseUrl"];
        private readonly string _appKey = ConfigurationManager.AppSettings["Bkash:AppKey"];
        private readonly string _appSecret = ConfigurationManager.AppSettings["Bkash:AppSecret"];
        private readonly string _username = ConfigurationManager.AppSettings["Bkash:Username"];
        private readonly string _password = ConfigurationManager.AppSettings["Bkash:Password"];

        private async Task<string> GrantTokenAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/tokenized/checkout/token/grant");
            request.Headers.Add("username", _username);
            request.Headers.Add("password", _password);

            var body = new JObject
            {
                ["app_key"] = _appKey,
                ["app_secret"] = _appSecret
            };
            request.Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(request);
            var json = JObject.Parse(await response.Content.ReadAsStringAsync());

            if (!response.IsSuccessStatusCode || json["id_token"] == null)
                throw new Exception($"bKash grant token failed: {json}");

            return json["id_token"].ToString();
        }

        public async Task<(string transactionId, string rawResponse)> CheckoutAsync(decimal amount)
        {
            var token = await GrantTokenAsync();

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/tokenized/checkout/create");
            request.Headers.Add("Authorization", token);
            request.Headers.Add("X-App-Key", _appKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var body = new JObject
            {
                ["mode"] = "0011",
                ["payerReference"] = "01619777283", // sandbox test wallet number
                ["callbackURL"] = "https://localhost:44326/api/Payment/bkash-callback",
                ["amount"] = amount.ToString("0.00"),
                ["currency"] = "BDT",
                ["intent"] = "sale",
                ["merchantInvoiceNumber"] = "INV" + DateTime.UtcNow.Ticks
            };
            request.Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(request);
            var rawResponse = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(rawResponse);

            if (json["paymentID"] == null)
                throw new Exception($"bKash create payment failed: {rawResponse}");

            return (json["paymentID"].ToString(), rawResponse);
        }

        public async Task<bool> ExecutePaymentAsync(string transactionId)
        {
            var token = await GrantTokenAsync();

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/tokenized/checkout/execute");
            request.Headers.Add("Authorization", token);
            request.Headers.Add("X-App-Key", _appKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var body = new JObject { ["paymentID"] = transactionId };
            request.Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(request);
            var raw = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"[bKash Execute] {raw}");   // <-- TEMP: see it in Output window

            var json = JObject.Parse(raw);
            var status = json["transactionStatus"]?.ToString();
            var statusCode = json["statusCode"]?.ToString();
            var statusMessage = json["statusMessage"]?.ToString();

            if (status != "Completed")
                throw new Exception($"bKash Execute failed: statusCode={statusCode}, statusMessage={statusMessage}, raw={raw}");

            return true;
        }

        public async Task<bool> QueryPaymentAsync(string transactionId)
        {
            var token = await GrantTokenAsync();

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/tokenized/checkout/payment/status");
            request.Headers.Add("Authorization", token);
            request.Headers.Add("X-App-Key", _appKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var body = new JObject { ["paymentID"] = transactionId };
            request.Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(request);
            var raw = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"[bKash Query] {raw}");   // <-- TEMP: see it in Output window

            var json = JObject.Parse(raw);
            var status = json["transactionStatus"]?.ToString();
            var statusCode = json["statusCode"]?.ToString();
            var statusMessage = json["statusMessage"]?.ToString();

            if (status != "Completed")
                throw new Exception($"bKash Query failed: statusCode={statusCode}, statusMessage={statusMessage}, raw={raw}");

            return true;
        }
    }
}