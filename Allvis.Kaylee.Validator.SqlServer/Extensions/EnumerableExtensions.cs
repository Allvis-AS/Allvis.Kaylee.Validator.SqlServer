using System.Collections.Generic;
using System.Linq;

namespace Allvis.Kaylee.Validator.SqlServer.Extensions
{
    public delegate void ForEachAction<in T>(T obj, int index, bool last);

    public static class EnumerableExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> source, ForEachAction<T> action)
        {
            using var iter = source.GetEnumerator();
            var index = 0;
            if (iter.MoveNext())
            {
                var prev = iter.Current;
                while (iter.MoveNext())
                {
                    action(prev, index++, false);
                    prev = iter.Current;
                }
                action(prev, index++, true);
            }
        }

        public static IEnumerable<string> AlignLeft(this IEnumerable<string> values, int tabSize = 4)
        {
            var width = values.GetAlignPadWidth(tabSize);
            return values.Select(v => v.PadRight(width));
        }
        
        public static int GetAlignPadWidth(this IEnumerable<string> values, int tabSize = 4)
        {
            var width = values.Max(v => v.Length);
            var remainder = width % tabSize;
            return width - remainder + tabSize;
        }
    }
}