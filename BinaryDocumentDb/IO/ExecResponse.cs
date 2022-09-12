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
        public bool Success { get; set; }

        public int ErrorCode { get; set; }

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
        public T Result { get; set; } = default(T)!;
    }
}
