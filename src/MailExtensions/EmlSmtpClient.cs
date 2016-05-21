namespace MailExtensions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Mail;
    using System.Security;
    using System.Security.Authentication;
    using System.Text;
    using System.Threading;

    public class EmlSmtpClient
    {
        private readonly SmtpClient _client;

        public EmlSmtpClient(SmtpClient client)
        {
            _client = client;
        }

        public void Send(string from, List<string> to, Action<Stream> writeEmlString, DeliveryNotificationOptions options = DeliveryNotificationOptions.None)
        {
            if (_client.GetField<bool>("disposed"))
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
            try
            {
                if (_client.GetField<bool>("inCall"))
                {
                    throw new InvalidOperationException("net_inasync");
                }

                if (_client.DeliveryMethod == SmtpDeliveryMethod.Network)
                {
                    _client.Method("CheckHostAndPort");
                }

                var recipients = new MailAddressCollection();
                foreach (var email in to)
                {
                    // TODO - display name?
                    recipients.Add(new MailAddress(email));
                }
                if (recipients.Count == 0)
                {
                    throw new InvalidOperationException("SmtpRecipientRequired");
                }

                var transport = _client.GetField<object>("transport");
                //transport.SetProperty(false, "IdentityRequired");
                transport.SetProperty(false, "IdentityRequired");
                //transport.IdentityRequired = false; // everything completes on the same thread.

                try
                {
                    _client.SetField(true, "inCall");
                    //InCall = true;
                    _client.SetField(false, "timedOut");
                    //timedOut = false;

                    var timeout = _client.GetField<int>("transport", "timeout");
                    var timer = new Timer(new TimerCallback(this.TimeOutCallback), null, timeout, timeout);
                    _client.SetField(timer, "timer");
                    var allowUnicode = false;

                    _client.Method("GetConnection");
                    //GetConnection();

                    // Detected durring GetConnection(), restrictable using the DeliveryFormat paramiter
                    allowUnicode = _client.Method<bool>("IsUnicodeSupported");
                    // IsUnicodeSupported();

                    //_client.Method("ValidateUnicodeRequirement", message, recipients, allowUnicode);
                    //ValidateUnicodeRequirement(message, recipients, allowUnicode);

                    var args = new object[]
                    {
                        new MailAddress(from),
                        recipients,
                        BuildDeliveryStatusNotificationString(options),
                        allowUnicode,
                        null
                    };
                    var recipientException = (SmtpFailedRecipientException) args.Last();

                    var writer = transport.Method<object>("SendMail", args);

                    //var writer = transport.SendMail(message.Sender ?? message.From, recipients,
                    //    message.BuildDeliveryStatusNotificationString(), allowUnicode, out recipientException);

                    //_client.SetField(message, "message");
                    //message.Send(writer, DeliveryMethod != SmtpDeliveryMethod.Network, allowUnicode);

                    var mailStream = writer.GetField<Stream>("stream"); //writer.GetField<Stream>("stream");
                    writeEmlString.Invoke(mailStream);

                    writer.Method("Close");
                    transport.Method("ReleaseConnection");
                    //transport.ReleaseConnection();

                    //throw if we couldn't send to any of the recipients
                    if (recipientException != null)
                    {
                        throw recipientException;
                    }
                }
                catch (Exception e)
                {
                    if (e is SmtpFailedRecipientException && !e.GetField<bool>("fatal"))
                    {
                        throw;
                    }

                    _client.Method("Abort");
                    if (_client.GetField<bool>("timedOut"))
                    {
                        throw new SmtpException("net_timeout");
                    }

                    if (e is SecurityException ||
                        e is AuthenticationException ||
                        e is SmtpException)
                    {
                        throw;
                    }

                    throw new SmtpException("SmtpSendMailFailure", e);
                }
                finally
                {
                    _client.SetProperty(false, "InCall");
                    //InCall = false;
                    var timer = _client.GetField<IDisposable>("timer");
                    if (timer != null)
                    {
                        timer.Dispose();
                    }
                }
            }
            finally
            {
            }
        }

        void TimeOutCallback(object state)
        {
            if (!_client.GetField<bool>("timedOut"))
            {
                _client.SetField(true, "timedOut");
                _client.Method("Abort");
            }
        }

        private string BuildDeliveryStatusNotificationString(DeliveryNotificationOptions deliveryStatusNotification)
        {
            if (deliveryStatusNotification != DeliveryNotificationOptions.None)
            {
                var s = new StringBuilder(" NOTIFY=");

                var oneSet = false;

                //none
                if (deliveryStatusNotification == DeliveryNotificationOptions.Never)
                {
                    s.Append("NEVER");
                    return s.ToString();
                }

                if ((((int) deliveryStatusNotification) & (int) DeliveryNotificationOptions.OnSuccess) > 0)
                {
                    s.Append("SUCCESS");
                    oneSet = true;
                }
                if ((((int) deliveryStatusNotification) & (int) DeliveryNotificationOptions.OnFailure) > 0)
                {
                    if (oneSet)
                    {
                        s.Append(",");
                    }
                    s.Append("FAILURE");
                    oneSet = true;
                }
                if ((((int) deliveryStatusNotification) & (int) DeliveryNotificationOptions.Delay) > 0)
                {
                    if (oneSet)
                    {
                        s.Append(",");
                    }
                    s.Append("DELAY");
                }
                return s.ToString();
            }
            return string.Empty;
        }
    }
}