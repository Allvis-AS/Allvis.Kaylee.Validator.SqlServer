using System;

namespace Allvis.Kaylee.Validator.SqlServer.Extensions
{
    public static class StringExtensions
    {
        public static string Indent(this string str, int levels, int spaces = 4)
            => new string(' ', Math.Max(0, levels * spaces)) + str;
    }
}