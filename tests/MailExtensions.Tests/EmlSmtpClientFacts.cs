namespace MailExtensions.Tests
{
    using System.Net.Mail;

    using Xunit;

    public class EmlSmtpClientFacts
    {
        [Fact]
        public void Can_send_mail()
        {
            var smtpClient = new SmtpClient("10.0.5.100", 25);

            var eml = new EmlSmtpClient(smtpClient);

            // Act
            eml.Send(new MailMessage("from-test@silvenga.com", "test-to@silvenga.com"));

            // Assert
        }
    }
}