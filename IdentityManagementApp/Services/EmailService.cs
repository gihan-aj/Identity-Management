using IdentityManagementApp.DTOs.Account;
using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace IdentityManagementApp.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task<bool> SendEmailAsync(EmailSendDto emailSend)
        {
            try
            {
                string mailServer = _config["Email:Host"];
                string fromEmail = _config["Email:From"];
                string password = _config["Email:Password"];
                string displayName = _config["Email:ApplicationName"];
                int port = int.Parse(_config["Email:Port"]);

                var client = new SmtpClient(mailServer, port)
                {
                    Credentials = new NetworkCredential(fromEmail, password),
                    EnableSsl = true
                };

                MailAddress fromAddress = new MailAddress(fromEmail, displayName);

                MailMessage mailMessage = new MailMessage()
                {
                    From = fromAddress,
                    Subject = emailSend.Subject,
                    Body = emailSend.Body,
                    IsBodyHtml = emailSend.IsBodyHtml
                };

                mailMessage.To.Add(emailSend.ToEmail);

                await client.SendMailAsync(mailMessage);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
