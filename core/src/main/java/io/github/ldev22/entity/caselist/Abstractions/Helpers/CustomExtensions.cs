using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ade.Club51.Case.List.Helpers
{
    public static class CustomExtensions
    {
        public static bool IsEmpty(this string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        public static bool HasValue(this string value)
        {
            return !IsEmpty(value);
        }
    }
}
