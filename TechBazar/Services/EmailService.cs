using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace TechBazar.Services
{
    public class EmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _configuration;

        private readonly string _host;
        private readonly int _port;
        private readonly bool _enableSsl;
        private readonly string _userName;
        private readonly string _password;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            _host = _configuration["SmtpSettings:Host"];
            _port = int.Parse(_configuration["SmtpSettings:Port"] ?? "587");
            _enableSsl = bool.Parse(_configuration["SmtpSettings:EnableSsl"] ?? "true");
            _userName = _configuration["SmtpSettings:UserName"];
            _password = _configuration["SmtpSettings:Password"];
            _fromEmail = _configuration["SmtpSettings:FromEmail"];
            _fromName = _configuration["SmtpSettings:FromName"];
        }

        public async Task<bool> SendOrderConfirmationAsync(string email, string orderDetails)
        {
            try
            {
                // Simulate email sending
                _logger.LogInformation($"Sending order confirmation to: {email}");
                _logger.LogInformation($"Order details: {orderDetails}");

                await Task.Delay(100); // Simulate email sending delay
                _logger.LogInformation("Order confirmation email sent successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send order confirmation email");
                return false;
            }
        }

        public async Task<bool> SendWelcomeEmailAsync(string email, string userName)
        {
            try
            {
                _logger.LogInformation($"Sending welcome email to: {userName} ({email})");
                await Task.Delay(100);
                _logger.LogInformation("Welcome email sent successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send welcome email");
                return false;
            }
        }

        public async Task<bool> SendOtpEmailAsync(string email, string otp)
        {
            try
            {
                _logger.LogInformation($"Sending OTP email to: {email}");

                using (var client = new SmtpClient(_host, _port)
                {
                    Credentials = new NetworkCredential(_userName, _password),
                    EnableSsl = _enableSsl
                })
                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(_fromEmail, _fromName);
                    message.To.Add(new MailAddress(email));
                    message.Subject = "Your OTP Code";
                    message.Body = $"Your OTP code is: {otp}";
                    message.IsBodyHtml = false;

                    await client.SendMailAsync(message);
                }

                _logger.LogInformation($"OTP email sent successfully to: {email}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP email");
                return false;
            }
        }
    }
}
