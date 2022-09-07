using System;
using System.Collections.Generic;

namespace BinaryDocumentDb.Extensions
{
    internal static class IEnumerableExtensions
    {
        public static T Lowest<T>(this IEnumerable<T>? collection, Func<T, IComparable> comparableValueGetter)
        {
            IComparable? lowest = null;
            T lowestItem = default; // could be null

            if (collection != null)
                foreach (var item in collection)
                {
                    IComparable value = comparableValueGetter(item);
                    if (lowest is null)
                    {
                        lowest = value;
                        lowestItem = item;
                    }
                    else
                    {
                        var result = value.CompareTo(lowest);
                        if (result < 0)
                        {
                            lowest = value;
                            lowestItem = item;
                        }
                    }
                }           

            return lowestItem!;
        }
    }
}
