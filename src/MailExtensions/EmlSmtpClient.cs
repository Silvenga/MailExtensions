namespace MailExtensions
{
    using System;
    using System.Linq;
    using System.Net.Mail;
    using System.Reflection;
    using System.Security;
    using System.Security.Authentication;
    using System.Threading;

    public class EmlSmtpClient
    {
        private readonly SmtpClient _client;

        public EmlSmtpClient(SmtpClient client)
        {
            _client = client;
        }

        private const BindingFlags Binding = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;

        private T Method<T>(string methodName, params object[] args)
        {
            var dynMethod = _client.GetType().GetMethod(methodName, Binding);
            return (T) dynMethod.Invoke(_client, args);
        }

        private void Method(string methodName, params object[] args)
        {
            var dynMethod = _client.GetType().GetMethod(methodName, Binding);
            dynMethod.Invoke(_client, args);
        }

        private T GetField<T>(params string[] fieldName)
        {
            object last = _client;
            foreach (var name in fieldName)
            {
                var field = last.GetType().GetField(name, Binding);
                last = field.GetValue(last);
            }

            return (T) last;
        }

        private void SetField(object value, params string[] fieldName)
        {
            object last = _client;
            for (var index = 0; index < fieldName.Length - 1; index++)
            {
                var name = fieldName[index];
                var field = last.GetType().GetField(name, Binding);
                last = field.GetValue(last);
            }

            var prop = last.GetType().GetField(fieldName.Last(), Binding);
            prop.SetValue(last, value);
        }

        public void Send(MailMessage message)
        {
            if (GetField<bool>("disposed"))
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
            try
            {
                SmtpFailedRecipientException recipientException = null;

                if (GetField<bool>("inCall"))
                {
                    throw new InvalidOperationException("net_inasync");
                }

                if (message == null)
                {
                    throw new ArgumentNullException("message");
                }

                if (_client.DeliveryMethod == SmtpDeliveryMethod.Network)
                {
                    Method("CheckHostAndPort");
                }

                MailAddressCollection recipients = new MailAddressCollection();

                if (message.From == null)
                {
                    throw new InvalidOperationException("SmtpFromRequired");
                }

                if (message.To != null)
                {
                    foreach (MailAddress address in message.To)
                    {
                        recipients.Add(address);
                    }
                }
                if (message.Bcc != null)
                {
                    foreach (MailAddress address in message.Bcc)
                    {
                        recipients.Add(address);
                    }
                }
                if (message.CC != null)
                {
                    foreach (MailAddress address in message.CC)
                    {
                        recipients.Add(address);
                    }
                }

                if (recipients.Count == 0)
                {
                    throw new InvalidOperationException("SmtpRecipientRequired");
                }

                SetField(false, "transport", "m_IdentityRequired");
                //transport.IdentityRequired = false; // everything completes on the same thread.

                try
                {
                    SetField(true, "inCall");
                    //InCall = true;
                    SetField(false, "timedOut");
                    //timedOut = false;

                    var timeout = GetField<int>("transport", "timeout");
                    var timer = new Timer(new TimerCallback(this.TimeOutCallback), null, timeout, timeout);
                    SetField(timer, "timer");
                    bool allowUnicode = false;

                    Type mailWriterType = _client.GetType().Assembly.GetType("System.Net.Mail.MailWriter");
                    //dynamic writer = Activator.CreateInstance(mailWriterType, true);
                    //MailWriter writer;
                    Method("GetConnection");
                    //GetConnection();

                    // Detected durring GetConnection(), restrictable using the DeliveryFormat paramiter
                    allowUnicode = Method<bool>("IsUnicodeSupported");
                    // IsUnicodeSupported();

                    Method("ValidateUnicodeRequirement", message, recipients, allowUnicode);
                    //ValidateUnicodeRequirement(message, recipients, allowUnicode);
                    dynamic writer = transport.SendMail(message.Sender ?? message.From, recipients,
                        message.BuildDeliveryStatusNotificationString(), allowUnicode, out recipientException);

                    this.message = message;
                    message.Send(writer, DeliveryMethod != SmtpDeliveryMethod.Network, allowUnicode);
                    writer.Close();
                    transport.ReleaseConnection();

                    //throw if we couldn't send to any of the recipients
                    if (DeliveryMethod == SmtpDeliveryMethod.Network && recipientException != null)
                    {
                        throw recipientException;
                    }
                }
                catch (Exception e)
                {
                    if (e is SmtpFailedRecipientException && !((SmtpFailedRecipientException) e).fatal)
                    {
                        throw;
                    }

                    Method("Abort");
                    if (!GetField<bool>("timedOut"))
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
                    //InCall = false;
                    //if (timer != null)
                    //{
                    //    timer.Dispose();
                    //}
                }
            }
            finally
            {
            }
        }

        void TimeOutCallback(object state)
        {
            if (!GetField<bool>("timedOut"))
            {
                SetField(true, "timedOut");
                Method("Abort");
            }
        }
    }
}