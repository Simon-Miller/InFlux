using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace BinaryDocumentDb.Tests
{
    [TestClass]
    public class BinaryBlobContextTests
    {
        [TestMethod]
        public void Fires_OnCreated_event()
        {
            // Arrange
            var fs = new FakeVirtualFileStream(new byte[] { 0, 5, 0, 0, 0 });

            var instance = new BinaryBlobContext(fs);
            var count = 0;
            instance.OnCreated.Subscribe((O, N) => count++);

            // Act
            var exec = instance.Create(new byte[] { 1, 2, 3 });

            // Assert
            Assert.AreEqual(1, count);

            Assert.IsTrue(exec.Success);
            Assert.AreEqual(0, exec.ErrorCode);
            Assert.AreEqual(0, exec.Messages.Count);

            Assert.IsTrue(IEnumerableComparer.AreEqual(fs.Data, new byte[]
                { 0,5,0,0,0, 1,12,0,0,0, 1,0,0,0, 1,2,3 }));
        }

        [TestMethod]
        public void Fires_OnUpdated_event()
        {
            // Arrange
            var fs = new FakeVirtualFileStream(new byte[] { 1, 12, 0, 0, 0, 1, 0, 0, 0, 1, 2, 3 });

            var instance = new BinaryBlobContext(fs);
            var count = 0;
            instance.OnUpdated.Subscribe((O, N) => count++);

            // Act
            var response = instance.Update(1, new byte[] { 4, 5, 6 });

            // Assert
            Assert.AreEqual(1, count);

            Assert.IsTrue(response.Success);
            Assert.AreEqual(0, response.ErrorCode);
            Assert.AreEqual(0, response.Messages.Count);

            Assert.IsTrue(IEnumerableComparer.AreEqual(fs.Data, new byte[]
                { 1, 12,0,0,0, 1,0,0,0, 4,5,6 }));
        }

        [TestMethod]
        public void Fires_OnDeleted_event()
        {
            // Arrange
            var fs = new FakeVirtualFileStream(new byte[] { 1, 12, 0, 0, 0, 1, 0, 0, 0, 1, 2, 3 });

            var instance = new BinaryBlobContext(fs);
            var count = 0;
            instance.OnDeleted.Subscribe((O, N) => count++);

            // Act
            var response = instance.Delete(1);

            // Assert
            Assert.AreEqual(1, count);

            Assert.IsTrue(response.Success);
            Assert.AreEqual(0, response.ErrorCode);
            Assert.AreEqual(0, response.Messages.Count);

            Assert.IsTrue(IEnumerableComparer.AreEqual(fs.Data, new byte[]
                { 0, 12,0,0,0, 1,0,0,0, 1,2,3 }));
        }

        [TestMethod]
        public void Can_check_key_exists()
        {
            // Arrange
            var fs = new FakeVirtualFileStream(new byte[]
                { 0, 5,0,0,0, 1, 12,0,0,0, 123,0,0,0, 1, 2, 3 });

            var instance = new BinaryBlobContext(fs);

            // Act
            var goodResponse = instance.Exists(123);
            var badResponse = instance.Exists(0);

            // Assert
            Assert.IsTrue(goodResponse.Success);
            Assert.IsTrue(badResponse.Success);

            Assert.IsTrue(goodResponse.Result);
            Assert.IsFalse(badResponse.Result);
        }

        [TestMethod]
        public void Can_get_all_cache_keys()
        {
            // Arrange
            var fs = new FakeVirtualFileStream(new byte[]
                { 0, 5,0,0,0,   1, 12,0,0,0, 123,0,0,0, 1, 2, 3,   1, 10,0,0,0, 125,0,0,0, 123,   0, 5,0,0,0});

            var instance = new BinaryBlobContext(fs);

            // Act -- as it's a unit test, we can skip checking the exec response, because any failure is a unit test failure.
            var keys = instance.CacheKeys().Result.ToList();

            // Assert
            Assert.AreEqual(2, keys.Count);
            Assert.AreEqual(123u, keys[0]);
            Assert.AreEqual(125u, keys[1]);
        }

        [TestMethod]
        public void Can_get_next_available_key_and_save()
        {
            // Arrange
            var fs = new FakeVirtualFileStream(new byte[] { 0, 5, 0, 0, 0 });
            var instance = new BinaryBlobContext(fs);
            var onCreatedCalledCount = 0;
            instance.OnCreated.Subscribe((O, N) => onCreatedCalledCount++);

            // Act
            var reservedKey = instance.ReserveNextKey().Result;
            

            var blob = new byte[] { 1, 2, 3 };
            var response = instance.Create(reservedKey, blob);

            var readResponse = instance.Read(reservedKey);

            // TODO: Create data scenarios of empty spaces in file to account for exact match for space required,
            // as well as bigger than needed.

            // DONE: This test will store the entry at the END of the file.

            Assert.IsTrue(response.Success);
            Assert.IsTrue(readResponse.Success);
            Assert.AreEqual(1, onCreatedCalledCount);
            Assert.IsTrue(IEnumerableComparer.AreEqual(fs.Data, new byte[] { 0, 5, 0, 0, 0, 1, 12, 0, 0, 0, 1, 0, 0, 0, 1, 2, 3 }));
        }

        [TestMethod]
        public void Can_get_next_available_key_and_save_in_exact_free_space()
        {
            // Arrange
            var fs = new FakeVirtualFileStream(new byte[] { 0, 12, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
            var instance = new BinaryBlobContext(fs);
            var onCreatedCalledCount = 0;
            instance.OnCreated.Subscribe((O, N) => onCreatedCalledCount++);

            // Act
            var reservedKey = instance.ReserveNextKey().Result;

            var blob = new byte[] { 1, 2, 3 };
            var response = instance.Create(reservedKey, blob);

            var readResponse = instance.Read(reservedKey);

            // DONE: This test will store the entry in an existing free space of exact match size.

            Assert.IsTrue(response.Success);
            Assert.IsTrue(readResponse.Success);
            Assert.AreEqual(1, onCreatedCalledCount);
            Assert.IsTrue(IEnumerableComparer.AreEqual(fs.Data, new byte[] { 1, 12, 0, 0, 0, 1, 0, 0, 0, 1, 2, 3 }));
        }

        [TestMethod]
        public void Can_get_next_available_key_and_save_in_larger_free_space()
        {
            // Arrange
            var fs = new FakeVirtualFileStream(new byte[] { 0, 17, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
            var instance = new BinaryBlobContext(fs);
            var onCreatedCalledCount = 0;
            instance.OnCreated.Subscribe((O, N) => onCreatedCalledCount++);

            // Act
            var reservedKey = instance.ReserveNextKey().Result;

            var blob = new byte[] { 1, 2, 3 };
            var response = instance.Create(reservedKey, blob);

            var readResponse = instance.Read(reservedKey);

            // DONE: This test will store the entry in an existing free space plus free space entry of 5.

            Assert.IsTrue(response.Success);
            Assert.IsTrue(readResponse.Success);
            Assert.AreEqual(1, onCreatedCalledCount);            
            Assert.IsTrue(IEnumerableComparer.AreEqual(fs.Data, new byte[] { 1, 12, 0, 0, 0, 1, 0, 0, 0, 1, 2, 3,   0, 5, 0, 0, 0 }));
        }
    }
}
