using BinaryDocumentDb.IntegrationTests.UnitTestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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

        [TestMethod]
        public void Write_and_quit_without_proper_shutdown_work()
        {
            // Arrange
            const string dbFileName = "staticTest.db";
            var factory = DIContainer.DI.GetService<IBinaryDocumentDbFactory>();

            var instance = factory!.Make(new BdDbConfig() { FilePathAndName = dbFileName });

            // Act
            var key = instance.ReserveNextKey();
            var data = new byte[] { 1, 2, 3 };

            instance.Create(key.Result, data);
            var response = instance.Flush();
           
            if(true)
            {
                // kill the app here, and see if we can read the data in another test?

                // yeah, doesn't save!  ARGH!
                // So, need to flush the stream?
            }
        }

        [TestMethod]
        public void Write_and_quit_without_proper_shutdown_work_part_2()
        {
            // Arrange
            const string dbFileName = "staticTest.db";
            var factory = DIContainer.DI.GetService<IBinaryDocumentDbFactory>();
            var instance = factory!.Make(new BdDbConfig() { FilePathAndName = dbFileName });

            // Act
            uint key = 1;

            var data = instance.Read(key);
        }
    }
}