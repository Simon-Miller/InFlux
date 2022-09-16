using BinaryDocumentDb.IntegrationTests.UnitTestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace BinaryDocumentDb.IntegrationTests
{
    [TestClass]
    public class BinaryDocumentDbFactoryTests
    {
        [TestMethod]
        public void Static_Make_works()
        {
            // Arrange
            const string dbFileName = "staticTest.db";

            var blob = new byte[] { 1, 2, 3 };

            if (File.Exists(dbFileName))
                File.Delete(dbFileName);

            var factory = DIContainer.DI.GetService<IBinaryDocumentDbFactory>();

            // Act
            var instance = factory!.Make(new BdDbConfig() { FilePathAndName = dbFileName });

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
        [ExpectedException(typeof(IOException))]
        public void Creating_more_than_one_instance_of_same_database_fails()
        {
            // Arrange
            const string dbFileName = "staticTest.db";
            var factory = DIContainer.DI.GetService<IBinaryDocumentDbFactory>();

            // Act
            var instanceOne = factory!.Make(new BdDbConfig() { FilePathAndName = dbFileName });

            // should fail
            var instanceTwo = factory.Make(new BdDbConfig() { FilePathAndName = dbFileName });
        }
    }
}