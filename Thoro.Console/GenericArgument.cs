namespace Thoro.Console
{
    using System;
    using System.Reflection;

    public class Argument<T> : Argument
    {
        public T Value { get; set; }

        public Argument(string name)
            : base(name)
        {

        }

        public Argument(string name, T value)
            : base(name)
        {
            this.Value = value;
        }

        public override bool Parse(string value)
        {
            if (value is T)
            {
                this.Value = (T)(object)value;
                return true;
            }

            MethodInfo m = typeof(T).GetMethod("TryParse", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string), typeof(T).MakeByRefType() }, null);

            if (m != null)
            {
                var param = new object[] { value, null };
                bool result = (bool)m.Invoke(null, param);

                if (result)
                {
                    this.Value = (T)param[1];
                    return true;
                }
            }

            return base.Parse(value);
        }
    }
}
