namespace BinaryDocumentDb.IntegrationTests
{
    [TestClass]
    public class BinaryDocumentDbFactoryTests
    {
        [TestMethod]
        public void Static_make_works()
        {
            // Arrange
            const string dbFileName = "staticTest.db";

            var blob = new byte[] { 1, 2, 3};

            if (File.Exists(dbFileName))
                File.Delete(dbFileName);

            // Act
            var instance = BinaryDocumentDbFactory.Make(new BdDbConfig() { FilePathAndName = dbFileName });

            var createResult = instance.Create(blob);

            // Assert
            Assert.IsTrue(createResult.Success);

            // Act
            var key = createResult.Result;
            var readResult = instance.Read(createResult.Result);

            // Assert
            Assert.IsTrue(readResult.Success);

            var readBlob = readResult.Result;
            Assert.IsTrue(readBlob[0] == 1);
            Assert.IsTrue(readBlob[1] == 2);
            Assert.IsTrue(readBlob[2] == 3);
        }

        [TestMethod]
        public void Instance_make_works()
        {
            // Arrange
            const string dbFileName = "staticTest2.db";

            var blob = new byte[] { 1, 2, 3 };

            if (File.Exists(dbFileName))
                File.Delete(dbFileName);

            // Act
            var factory = new BinaryDocumentDbFactory(new BdDbConfig() { FilePathAndName = dbFileName });
            var instance = factory.Make(); // GOTCHA!  Was private! LOL

            var createResult = instance.Create(blob);

            // Assert
            Assert.IsTrue(createResult.Success);

            // Act
            var key = createResult.Result;
            var readResult = instance.Read(createResult.Result);

            // Assert
            Assert.IsTrue(readResult.Success);

            var readBlob = readResult.Result;
            Assert.IsTrue(readBlob[0] == 1);
            Assert.IsTrue(readBlob[1] == 2);
            Assert.IsTrue(readBlob[2] == 3);
        }
    }
}