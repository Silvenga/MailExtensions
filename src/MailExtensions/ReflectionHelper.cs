namespace MailExtensions
{
    using System.Linq;
    using System.Reflection;

    public static class ReflectionHelper
    {
        public const BindingFlags Binding = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;

        public static T Method<T>(this object targetObject, string methodName, params object[] args)
        {
            var dynMethod = targetObject.GetType().GetMethod(methodName, Binding);
            return (T) dynMethod.Invoke(targetObject, args);
        }

        public static void Method(this object targetObject, string methodName, params object[] args)
        {
            var dynMethod = targetObject.GetType().GetMethod(methodName, Binding);
            dynMethod.Invoke(targetObject, args);
        }

        public static T GetField<T>(this object targetObject, params string[] fieldName)
        {
            var last = targetObject;
            foreach (var name in fieldName)
            {
                var field = last.GetType().GetField(name, Binding);
                last = field.GetValue(last);
            }

            return (T) last;
        }

        public static void SetField(this object targetObject, object value, params string[] fieldName)
        {
            var last = targetObject;
            for (var index = 0; index < fieldName.Length - 1; index++)
            {
                var name = fieldName[index];
                var field = last.GetType().GetField(name, Binding);
                last = field.GetValue(last);
            }

            var prop = last.GetType().GetField(fieldName.Last(), Binding);
            prop.SetValue(last, value);
        }

        public static T GetProperty<T>(this object targetObject, params string[] fieldName)
        {
            var last = targetObject;
            foreach (var name in fieldName)
            {
                var field = last.GetType().GetProperty(name, Binding);
                last = field.GetValue(last);
            }

            return (T) last;
        }

        public static void SetProperty(this object targetObject, object value, params string[] fieldName)
        {
            var last = targetObject;
            for (var index = 0; index < fieldName.Length - 1; index++)
            {
                var name = fieldName[index];
                var field = last.GetType().GetProperty(name, Binding);
                last = field.GetValue(last);
            }

            var prop = last.GetType().GetProperty(fieldName.Last(), Binding);
            prop.SetValue(last, value);
        }
    }
}