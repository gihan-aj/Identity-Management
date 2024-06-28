namespace IdentityManagementApp.DTOs.Account
{
    public class EmailSendDto
    {
        public EmailSendDto(string toEmail, string subject, string body, bool isBodyHtml)
        {
            ToEmail = toEmail;
            Subject = subject;
            Body = body;
            IsBodyHtml = isBodyHtml;
        }

        public string ToEmail { get; }
        public string Subject { get; }
        public string Body { get; }
        public bool IsBodyHtml { get; }
    }
}
