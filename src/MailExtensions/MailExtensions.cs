namespace MailExtensions
{
    using System;
    using System.IO;
    using System.Net.Mail;
    using System.Reflection;
    using System.Text;

    public static class MailExtensions
    {
        public static string ToEml(this MailMessage mail)
        {
            var stream = new MemoryStream();
            var mailWriterType = mail.GetType().Assembly.GetType("System.Net.Mail.MailWriter");
            var mailWriter = Activator.CreateInstance(
                type: mailWriterType,
                bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                args: new object[] {stream},
                culture: null,
                activationAttributes: null);

            mail.GetType().InvokeMember(
                name: "Send",
                invokeAttr: BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod,
                binder: null,
                target: mail,
                args: new object[] {mailWriter, true, true});

            return Encoding.UTF8.GetString(stream.ToArray());
        }

        public static Stream ToEmlStream(this MailMessage mail)
        {
            var stream = new MemoryStream();
            var mailWriterType = mail.GetType().Assembly.GetType("System.Net.Mail.MailWriter");
            var mailWriter = Activator.CreateInstance(
                type: mailWriterType,
                bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                args: new object[] {stream},
                culture: null,
                activationAttributes: null);

            mail.GetType().InvokeMember(
                name: "Send",
                invokeAttr: BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod,
                binder: null,
                target: mail,
                args: new[] {mailWriter, true, true});
            
            var buffer = stream.ToArray();
            var real = new MemoryStream(buffer);

            return real;
        }
    }
}