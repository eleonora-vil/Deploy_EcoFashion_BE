using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using EcoFashionBackEnd.Helpers;
using EcoFashionBackEnd.Services;
using Microsoft.Extensions.Configuration;


public class EmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public EmailService(IConfiguration config)
    {
        _httpClient = new HttpClient();
        _apiKey = config["RESEND_API_KEY"]; // đọc từ ENV Railway
        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException("RESEND_API_KEY không được null.");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
    }

    public async Task<bool> SendEmailAsync(MailData mailData)
    {
        try
        {
            var payload = new
            {
                from = "onboarding@resend.dev", // test ban đầu
                to = new[] { mailData.EmailToId },
                subject = mailData.EmailSubject,
                html = mailData.EmailBody // support HTML
            };

            var response = await _httpClient.PostAsJsonAsync("https://api.resend.com/emails", payload);
            var body = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"[Resend] Status: {response.StatusCode}, Body: {body}");

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EmailService] Error: {ex.Message}");
            return false;
        }
    }
}
