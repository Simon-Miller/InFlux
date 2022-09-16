namespace BinaryDocumentDb.IntegrationTests.UnitTestHelpers
{
    internal class FakeIBinaryDocumentDbFactory : IBinaryDocumentDbFactory
    {
        int instances = 0;

        public IBinaryDocumentDb Make(BdDbConfig config)
        {
            if (instances == 0)
            {
                instances++;
                return new FakeIBinaryDocumentDb();
            }

            throw new IOException("already in use");
        }
    }
}
