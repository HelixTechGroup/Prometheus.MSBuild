using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prometheus.MSBuild.Tasks
{
    public class StringExtensions
    {
        private readonly string realString = String.Empty;

        public StringExtensions(string @string)
        {
            realString = @string;
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
