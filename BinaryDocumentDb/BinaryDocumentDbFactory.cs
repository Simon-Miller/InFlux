namespace BinaryDocumentDb
{
    /// <summary>
    /// Factory with both a static Make, and an instance version for DI purposes.
    /// </summary>
    public class BinaryDocumentDbFactory
    {
        public BinaryDocumentDbFactory(BdDbConfig config)
        {
            Config = config;
        }

        public readonly BdDbConfig Config;

        public IBinaryDocumentDb Make()
        {
            return Make(Config);
        }

        public static IBinaryDocumentDb Make(BdDbConfig config)
        {
            return new BinaryBlobContext(config);
        }
    }
}
