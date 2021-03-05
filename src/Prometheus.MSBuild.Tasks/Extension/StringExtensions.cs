using System;

namespace Prometheus.MSBuild.Tasks.Extension
{
    public class StringExtensions
    {
        private readonly string realString = String.Empty;

        public StringExtensions(string value)
        {
            realString = value;
        }

        public static implicit operator string(StringExtensions d) => d.realString;

        public static implicit operator StringExtensions(string b) => new StringExtensions(b);

        public override string ToString() => $"{realString}";

        public static string operator *(StringExtensions a, int b)
        {
            var r = string.Empty;

            for (int i = 0; i < b; i++)
            {
                r += a.realString;
            }

            return r;
        }

        public string RealString
        {
            get { return realString; }
        }
    }
}
