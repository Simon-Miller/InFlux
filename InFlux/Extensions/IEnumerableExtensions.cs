﻿namespace InFlux.Extensions
{
    public static class IEnumerableExtensions
    {
        /// <summary>
        /// Allows you to iterate over a collection, and perform a given action.
        /// Returns the original collection in case you have some other extension to run against it.
        /// </summary>
        public static IEnumerable<T> Each<T>(this IEnumerable<T> items, Action<T> code)
        {
            foreach (var item in items)
                code(item);

            return items;
        }

        public static IEnumerable<T?> ToNullableEnumerable<T>(this IEnumerable<T> items) => 
            items.Select(x => x);
        
    }
}
