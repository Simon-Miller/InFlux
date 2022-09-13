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
            var fs = new FakeVirtualFileStream(new byte[] { 1,  12, 0, 0, 0,  1, 0, 0, 0,  1, 2, 3 });

            var instance = new BinaryBlobContext(fs);
            var count = 0;
            instance.OnUpdated.Subscribe((O, N) => count++);

            // Act
            var exec = instance.Update(1, new byte[] { 4, 5, 6 });

            // Assert
            Assert.AreEqual(1, count);

            Assert.IsTrue(exec.Success);
            Assert.AreEqual(0, exec.ErrorCode);
            Assert.AreEqual(0, exec.Messages.Count);

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
            var exec = instance.Delete(1);

            // Assert
            Assert.AreEqual(1, count);

            Assert.IsTrue(exec.Success);
            Assert.AreEqual(0, exec.ErrorCode);
            Assert.AreEqual(0, exec.Messages.Count);

            Assert.IsTrue(IEnumerableComparer.AreEqual(fs.Data, new byte[]
                { 0, 12,0,0,0, 1,0,0,0, 1,2,3 }));
        }
    }
}
