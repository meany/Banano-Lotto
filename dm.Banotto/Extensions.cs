using System;
using System.Collections.Generic;
using System.Text;

namespace dm.Banotto
{
    public static class Extensions
    {
        public static string AddCommas(this int source)
        {
            return string.Format("{0:n0}", source);
        }

        public static string ToDate(this DateTime? source)
        {
            return (source.HasValue) ? source.Value.ToUniversalTime().ToString("r") : null;
        }
    }
}
