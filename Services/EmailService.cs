using System.Net.Mail;
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

        public SendGridEmailService(IConfiguration configuration, ILogger<SendGridEmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendVerificationEmailAsync(string toEmail, string toName, string verificationToken)
        {
            try
            {
                var apiKey = _configuration["SendGrid:ApiKey"];
                var client = new SendGridClient(apiKey);
                var from = new EmailAddress(_configuration["SendGrid:FromEmail"], _configuration["SendGrid:FromName"]);
                var subject = "Verify your email address";
                var to = new EmailAddress(toEmail, toName);

                var verificationLink = $"{_configuration["BaseUrl"]}/Account/VerifyEmail?token={verificationToken}&email={Uri.EscapeDataString(toEmail)}";

                var plainTextContent = $"Please verify your email by clicking this link: {verificationLink}";
                var htmlContent = $@"
                    <h2>Welcome to User Management System!</h2>
                    <p>Please verify your email address by clicking the link below:</p>
                    <p><a href='{verificationLink}'>Verify Email Address</a></p>
                    <p>If you did not create an account, please ignore this email.</p>
                    <p>This link will expire in 24 hours.</p>";

                var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
                var response = await client.SendEmailAsync(msg);

                if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
                {
                    _logger.LogInformation($"Verification email sent to {toEmail}");
                }
                else
                {
                    _logger.LogError($"Failed to send email to {toEmail}. Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending verification email to {toEmail}");
            }
        }
    }
}