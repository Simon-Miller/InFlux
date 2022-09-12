using System.Collections.Generic;

namespace BinaryDocumentDb.IO
{
    /// <summary>
    /// represents the result of calling code that has no meaningful response,
    /// other than knowing if exceptions were thrown.  The meaning of 'Success' is often contentious,
    /// and we suggest its meaning assume no exceptions were thrown, and therefore the golden path, or
    /// expected anticipated result is the outcome.
    /// </summary>
    public class ExecResponse
    {
        /// <summary>
        /// The meaning of 'Success' is often contentious, and we suggest its meaning assume no exceptions 
        /// were thrown, and therefore the golden path, or expected anticipated result is the outcome.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// A api-defined value indicating the kind of error that occurred.
        /// </summary>
        public int ErrorCode { get; set; }

        /// <summary>
        /// If Success is FALSE, you should see one or more error messages in this collection.
        /// </summary>
        public List<string> Messages { get; set; } = new List<string>();
    }

    /// <summary>
    /// represents the result of calling code that returns a <typeparamref name="T"/>.
    /// The meaning of 'Success' is often contentious,
    /// and we suggest its meaning assume no exceptions were thrown, and therefore the golden path, or
    /// expected anticipated result is the outcome.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ExecResponse<T> : ExecResponse
    {
        /// <summary>
        /// If Success is true, you should have the expected resulting value here.
        /// </summary>
        public T Result { get; set; } = default(T)!;
    }
}
