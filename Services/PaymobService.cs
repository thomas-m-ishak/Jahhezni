using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

public class PaymobService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public PaymobService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<string> GetAuthToken()
    {
        // تم التعديل ليقرأ من القسم القديم "Paymob:ApiKey"
        var apiKey = _config["Paymob:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new Exception("خطأ: لم يتم العثور على الـ ApiKey في الـ appsettings.");
        }

        var data = new { api_key = apiKey };

        var response = await _httpClient.PostAsync(
            "https://accept.paymob.com/api/auth/tokens",
            new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json")
        );

        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"خطوة التوكن فشلت: StatusCode = {response.StatusCode}\n\n{json}");
        }

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("token").GetString();
    }

    public async Task<int> CreateOrder(string token, decimal amount)
    {
        var data = new
        {
            auth_token = token,
            delivery_needed = "false",
            amount_cents = (int)(amount * 100),
            currency = "EGP",
            items = new object[] { }
        };

        var response = await _httpClient.PostAsync(
            "https://accept.paymob.com/api/ecommerce/orders",
            new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json")
        );

        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"خطوة إنشاء الأوردر فشلت: {json}");
        }

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("id").GetInt32();
    }

    public async Task<string> GetPaymentKey(string token, int orderId, decimal amount, int integrationId)
    {
        var data = new
        {
            auth_token = token,
            amount_cents = (int)(amount * 100),
            expiration = 3600,
            order_id = orderId,
            billing_data = new
            {
                first_name = "User",
                last_name = "Test",
                email = "test@test.com",
                phone_number = "01000000000",
                apartment = "NA",
                floor = "NA",
                street = "NA",
                building = "NA",
                shipping_method = "NA",
                postal_code = "NA",
                city = "Cairo",
                country = "EG",
                state = "NA"
            },
            currency = "EGP",
            integration_id = integrationId
        };

        var response = await _httpClient.PostAsync(
            "https://accept.paymob.com/api/acceptance/payment_keys",
            new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json")
        );

        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"خطوة الـ Payment Key فشلت: {json}");
        }

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("token").GetString();
    }

    public async Task<string> GetWalletPaymentUrl(string paymentToken)
    {
        var data = new
        {
            source = new { identifier = "01272984363", subtype = "WALLET" },
            payment_token = paymentToken
        };

        var response = await _httpClient.PostAsync(
            "https://accept.paymob.com/api/acceptance/payments/pay",
            new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json")
        );

        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"خطوة المحفظة فشلت: {json}");
        }

        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("iframe_url", out var urlProp))
            return urlProp.GetString();

        return doc.RootElement.GetProperty("redirection_url").GetString();
    }
}