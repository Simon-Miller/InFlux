using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace BinaryDocumentDb.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            // Arrange
            var config = new BdDbConfig() { FilePathAndName="testDb.bin" };

            // Act
            var context = new BinaryDocumentDbContext(config);
            context.Dispose();

            // TODO: Consider a free-space area in the file.
            // The initial file size should be 4k.
            // Therefore, we should expect a free index (need another offset for that at beginning of file)
            // and the index itself will have one entry (expect 10 entries as a minimum)
            // the one entry (non-zero offset) will point to the free space, and the size.
            // We'll want to watch this in memory.  Sounds like fun!

            // basically, as we ask to save a blob, we look to its size, and see if there's any free space than minimally fits.
            // If we put the data there, the free space entry changes offset, and space left.
            // If we don't have enough space, we have to write to the end of the file, as much as needed, but take the growth into account,
            // which means we should end up with more free space in the index.
            // Watch the index entries don't get bigger than 10, otherwise we need to declare the index space on disk as free space,
            // and write the index somewhere else in the available file space.

            // Assert
        }

        [TestMethod]
        public void TestMethod2()
        {
            // Arrange
            var config = new BdDbConfig() { FilePathAndName = "testDb2.bin" };
            var context = new DocDbFileContext(config);

            // Act
            byte[] blob = new byte[] { 0, 1, 2, 254, 255 };
            var blobKey = context.InsertBlob(blob);

            var target = context.GetBlob(blobKey);

            context.Dispose();
        }

        [TestMethod]
        public void Can_read_blob()
        {
            // Arrange
            var config = new BdDbConfig() { FilePathAndName = "testDb2.bin" };
            var context = new DocDbFileContext(config);

            // Act
            var target = context.GetBlob(1); // known existing blob from previous test
        }
    }
}