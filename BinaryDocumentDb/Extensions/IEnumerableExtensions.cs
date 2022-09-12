using System;
using System.Collections.Generic;

namespace BinaryDocumentDb.Extensions
{
    internal static class IEnumerableExtensions
    {
        /// <summary>
        /// A Linq to Objects extension that returns the lowest perceived item in a collection based on you
        /// providing a selector that returns an <see cref="IComparable"/> used to determine the lowest.
        /// </summary>
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

        /// <summary>
        /// returns -1 if not found.  Otherwise returns the index within the collection of the first item
        /// matching the predicate.
        /// </summary>
        public static int FirstIndexOf<T>(this IEnumerable<T>? collection, Func<T, bool> predicate)
        {
            if (collection is null) return -1;

            int index = 0;
            foreach (var item in collection)
            {
                if (predicate(item))
                    return index;

                index++;
            }

            return -1;
        }
    }
}
