using System.Collections.Generic;

namespace InFlux.Extensions
{
    /// <summary>
    /// Extension methods for Objects.
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// turn a single object into a list containing that single object.
        /// </summary>
        public static List<T> AsList<T>(this T obj) => new List<T> { obj };
    }
}
