using System;

namespace BinaryDocumentDb.IO
{
    /// <summary>
    /// Helpers to wrap a Func or Action in a try/catch block, and responds with a wrapper class detailing success or failure.
    /// </summary>
    internal class TryCatch
    {
        /// <summary>
        /// wraps your code in a try/catch, and guarantees a response as such.
        /// Exceptions are added to the response, is an exception is thrown.
        /// </summary>

        public static ExecResponse Wrap(Action code, int errorCode = 13)
        {
            var response = new ExecResponse
            {
                Success = true
            };

            try
            {
                code();
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.ErrorCode = errorCode;
                response.Messages.Add(ex.Message);
            }

            return response;
        }

        /// <summary>
        /// wraps your code in a try/catch, and guarantees a response as such.
        /// Exceptions are added to the response, is an exception is thrown.
        /// </summary>
        public static ExecResponse<T> Wrap<T>(Func<T> code, int errorCode = 13)
        {
            var response = new ExecResponse<T>
            {
                Success = true
            };

            try
            {
                response.Result = code();
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.ErrorCode = errorCode;
                response.Messages.Add(ex.Message);
            }

            return response;
        }
    }
}
