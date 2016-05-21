namespace MailExtensions.Tests
{
    using System.Collections.Generic;
    using System.Net.Mail;

    using Xunit;

    public class EmlSmtpClientFacts
    {
        [Fact]
        public void Can_send_mail()
        {
            var smtpClient = new SmtpClient("dev1.silvenga.net", 27);

            var eml = new EmlSmtpClient(smtpClient);

            // Act

            var message = new MailMessage("from-test@silvenga.com", "test-to@silvenga.com", "subject", "body");
            message.IsBodyHtml = true;
            var stream = message.ToEmlStream();

            //smtpClient.Send(message);

            eml.Send("from-test@silvenga.com", new List<string> {"test-to@silvenga.com"}, stream1 => { stream.CopyTo(stream1); });

            // Assert
        }
    }
}