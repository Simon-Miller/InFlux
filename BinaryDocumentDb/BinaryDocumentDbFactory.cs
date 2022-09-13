namespace BinaryDocumentDb
{
    /// <summary>
    /// Factory method to instantiate a class for you, exposed as an <see cref="IBinaryDocumentDb"/>.
    /// </summary>
    public class BinaryDocumentDbFactory
    {
        /// <summary>
        /// Provide configuration, and receive an <see cref="IBinaryDocumentDb"/> instance in return.
        /// <para>If the file you provide is already in use, an exception will be thrown.</para>
        /// </summary>
        public static IBinaryDocumentDb Make(BdDbConfig config)
        {
            return new BinaryBlobContext(config);
        }
    }
}
