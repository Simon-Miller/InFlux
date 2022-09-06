using BinaryDocumentDb.Tests.UnitTestHelpers;

namespace BinaryDocumentDb.Tests.Try3
{
    [TestClass]
    public class FileStuffTests
    {
        [TestMethod]
        public void Can_scan_file()
        {
            // Arrange
            var fs = new FakeVirtualFileStream(new byte[]
            {
                0, 0,0,0,0, // wasted 5 bytes on empty entry at beginning of file.
                
                1, 5,0,0,0, 1,0,0,0, 255 // entry of key=1, value = 255 (1 byte)
            });

            var instance = new FileStuff(fs);

            // Act
            var (index, freespace) = instance.ScanFile();

            // Assert
            Assert.AreEqual(1, index.Count);
            Assert.AreEqual(1, freespace.Count);

            Assert.AreEqual((uint)0, freespace[0].Offset);
            Assert.AreEqual((uint)5, freespace[0].Length);

            Assert.AreEqual(true, index.ContainsKey(1));
            Assert.AreEqual((uint)6, index[1]);
        }
    }
}
