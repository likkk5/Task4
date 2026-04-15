using System.Net.Mail;
using Microsoft.AspNetCore.Http;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace UserManagement.Services
{
    public interface IEmailService
    {
        Task SendVerificationEmailAsync(string toEmail, string toName, string verificationToken);
    }

    public class SendGridEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SendGridEmailService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SendGridEmailService(IConfiguration configuration, ILogger<SendGridEmailService> logger, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task SendVerificationEmailAsync(string toEmail, string toName, string verificationToken)
        {
            try
            {
                var request = _httpContextAccessor.HttpContext?.Request;
                var baseUrl = $"{request?.Scheme}://{request?.Host}";
                var verificationLink = $"{baseUrl}/Account/VerifyEmail?token={verificationToken}&email={Uri.EscapeDataString(toEmail)}";

                var apiKey = _configuration["SendGrid:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new Exception("SendGrid API key is not configured.");
                }

                var client = new SendGridClient(apiKey);
                var from = new EmailAddress(_configuration["SendGrid:FromEmail"], _configuration["SendGrid:FromName"]);
                var subject = "Verify your email address";
                var to = new EmailAddress(toEmail, toName);

                var plainTextContent = $"Please verify your email by clicking this link: {verificationLink}";
                var htmlContent = $@"
                <h2>Welcome to User Management System!</h2>
                <p>Please verify your email address by clicking the link below:</p>
                <p><a href='{verificationLink}'>Verify Email Address</a></p>";

                var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
                var response = await client.SendEmailAsync(msg);

                if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
                {
                    _logger.LogInformation($"Verification email sent to {toEmail}");
                }
                else
                {
                    var errorBody = await response.Body.ReadAsStringAsync();
                    _logger.LogError($"Failed to send email to {toEmail}. Status: {response.StatusCode}, Error: {errorBody}");
                    throw new Exception($"SendGrid responded with {response.StatusCode}: {errorBody}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending verification email to {toEmail}");
                throw;
            }
        }
    }
}