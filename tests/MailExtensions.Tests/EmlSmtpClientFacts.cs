namespace MailExtensions.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Mail;
    using System.Text.RegularExpressions;

    using FluentAssertions;

    using Ploeh.AutoFixture;

    using Xunit;

    public class EmlSmtpClientFacts : IDisposable
    {
        private const string MirrorServer = "mxmirror.net";

        private static readonly Fixture AutFixture = new Fixture();
        private readonly SmtpClient _smtpClient;
        private HttpClient _client;

        public EmlSmtpClientFacts()
        {
            _smtpClient = new SmtpClient(MirrorServer, 25);
            _client = new HttpClient {BaseAddress = new Uri($"http://{MirrorServer}")};
        }

        [Fact]
        public void Can_send_mail()
        {
            var messageIdOne = AutFixture.Create<string>();
            var messageIdTwo = AutFixture.Create<string>();
            var subject = AutFixture.Create<string>();
            var body = AutFixture.Create<string>();

            var one = new MailMessage("from-test@silvenga.com", "test-to@silvenga.com", subject, body)
            {
                IsBodyHtml = true
            };
            one.Headers.Add("X-MessageId", messageIdOne);

            var two = new MailMessage("from-test@silvenga.com", "test-to@silvenga.com", subject, body)
            {
                IsBodyHtml = true
            };
            two.Headers.Add("X-MessageId", messageIdTwo);

            var emlSmtpClient = new EmlSmtpClient(_smtpClient);

            // Act
            _smtpClient.Send(one);

            var stream = two.ToEmlStream();
            emlSmtpClient.Send("from-test@silvenga.com", new List<string> {"test-to@silvenga.com"}, stream1 => { stream.CopyTo(stream1); });

            // Assert
            var resultOne = _client.GetAsync($"api/messages/id/{messageIdOne}/eml").Result;
            var emlOne = resultOne.Content.ReadAsStringAsync().Result;
            emlOne = Regex.Replace(emlOne, ".*quoted-printable", "", RegexOptions.Singleline);

            var resultTwo = _client.GetAsync($"api/messages/id/{messageIdTwo}/eml").Result;
            var emlTwo = resultTwo.Content.ReadAsStringAsync().Result;
            emlTwo = Regex.Replace(emlTwo, ".*quoted-printable", "", RegexOptions.Singleline);

            emlOne.Should().Be(emlTwo);
        }

        public void Dispose()
        {
            _smtpClient.Dispose();
            _client.Dispose();
        }
    }
}