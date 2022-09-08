using BinaryDocumentDb.Tests.UnitTestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
                0, 5,0,0,0, // wasted 5 bytes on empty entry at beginning of file.
                
                1, 10,0,0,0, 1,0,0,0, 255 // entry of key=1, value = 255 (1 byte)
            });

            var instance = new FileStuff(fs);

            // Act
            var (index, freespace) = instance.ScanFile();

            // Assert
            Assert.AreEqual(1, index.Count);
            Assert.AreEqual(1, freespace.Count);

            Assert.AreEqual(0u, freespace[0].Offset);
            Assert.AreEqual(5u, freespace[0].Length);

            Assert.AreEqual(true, index.ContainsKey(1));
            Assert.AreEqual(5u, index[1]);
        }

        [TestMethod]
        public void Can_scan_file_with_2_blobs()
        {
            // trying to ensure that seeking the end of each entry works, and is not off-by-one.

            // Arrange
            var fs = new FakeVirtualFileStream(new byte[]
            {
              //0  1  2 3 4  5 6 7 8  9
                1, 10,0,0,0, 1,0,0,0, 255, // entry of key=1, value = 255 (1 byte)

              //10 11121314
                0, 5,0,0,0, // wasted 5 bytes on empty entry at beginning of file.

              //15 16 171819 20212223 24
                1, 10,0,0,0, 2,0,0,0, 254, // entry of key=1, value = 255 (1 byte)
            });

            var instance = new FileStuff(fs);

            // Act
            var (index, freespace) = instance.ScanFile();

            // Assert
            Assert.AreEqual(2, index.Count);
            Assert.AreEqual(1, freespace.Count);

            // Not sure I like that we point to the entity data instead of the entity type.
            // I think the address containing the type byte is part of each entity, and we should include that as the address.
            // The length could even be the entire length of the entity includingf its type byte?  It makes sense we translate that 
            // length to what ever we need by subtracting the byte (1) and subtracting the length (5) to get the actual data length.

            Assert.AreEqual(00u, index[1]);
            Assert.AreEqual(15u, index[2]);
            Assert.AreEqual(10u, freespace[0].Offset);

            Assert.AreEqual(5u, freespace[0].Length); // include type byte
        }

        [TestMethod]
        public void Can_defrag_empty_space_entries()
        {
            // Arrange
            var fs = new FakeVirtualFileStream(new byte[]
            {
                0, 5,0,0,0,    // empty 5 bytes entry
                0, 6,0,0,0,0,  // empty 6 bytes entry
                0, 7,0,0,0,0,0,// empty 7 bytes entry

                1, 10,0,0,0, 1,0,0,0, 123, // blob entry in middle.

                0, 5,0,0,0,    // empty 5 bytes entry
                0, 6,0,0,0,0,  // empty 6 bytes entry

                1, 12,0,0,0, 2,0,0,0, 1,2,3 // blob entry at end.
            });

            var instance = new FileStuff(fs);
            var (index, freespace) = instance.ScanFile();

            Assert.AreEqual(2, freespace.Count);

            // Gotcha! Off by one?  reports 17 when should be 18?
            // Gotcha! 4th free space entry NOT merged with 5th?
            Assert.IsTrue(IEnumerableComparer.AreEqual(fs.Data, new byte[] 
                { 0,18,0,0,0, 0,6,0,0,0,0, 0,7,0,0,0,0,0, // defrag'd entry with old data unchanged
                  1, 10,0,0,0, 1,0,0,0, 123,              // existing entry unchanged
                  0, 11,0,0,0, 0,6,0,0,0,0,               // defrag'd entry with old data unchanged
                  1, 12,0,0,0, 2,0,0,0, 1,2,3             // existing entry unchanged
                }));
        }

        [TestMethod]
        public void Can_insert_blob_at_end()
        {
            // Arrange
            var fs = new FakeVirtualFileStream(new byte[]
            {
                0, 5,0,0,0, // wasted 5 bytes on empty entry at beginning of file.
            });

            var instance = new FileStuff(fs);
            var (index, freespace) = instance.ScanFile();

            // Act

            // should generate bytes for type, raw length, and data length.
            var key = instance.InsertBlob(index, freespace, new byte[] { 1, 2, 3 });

            // Assert
            Assert.AreEqual(1, index.Count);
            Assert.AreEqual(1, freespace.Count);
            Assert.AreEqual(5u, index[key]);
            Assert.AreEqual(17, fs.Length); // free space entry + blob entry with 3 data bytes

            Assert.IsTrue(IEnumerableComparer.AreEqual(fs.Data, new byte[] { 0,5,0,0,0, 1, 12,0,0,0, 1,0,0,0, 1,2,3 }));
        }

        [TestMethod]
        public void Can_insert_blob_in_available_space_of_exact_size()
        {
            // Arrange
            var fs = new FakeVirtualFileStream(new byte[]
            {
                0, 12,0,0,0,0,0,0,0,0,0,0 // empty 12 bytes entry at beginning of file.
            });

            var instance = new FileStuff(fs);
            var (index, freespace) = instance.ScanFile();

            // Act
            var numberOfFreeSpacesBeforeAct = freespace.Count;
            var numberOfBlobsBeforeAct = index.Count;

            // should generate bytes for type, raw length, and data length.
            var key = instance.InsertBlob(index, freespace, new byte[] { 1, 2, 3 });

            // Assert
            Assert.AreEqual(1, numberOfFreeSpacesBeforeAct);
            Assert.AreEqual(0, numberOfBlobsBeforeAct);

            Assert.AreEqual(0, freespace.Count);
            Assert.AreEqual(1, index.Count);
            
            Assert.AreEqual(0u, index[key]);

            Assert.IsTrue(IEnumerableComparer.AreEqual(fs.Data, new byte[] { 1, 12,0,0,0, 1,0,0,0, 1,2,3 }));
        }

        [TestMethod]
        public void Can_insert_blob_at_end_when_no_space_found()
        {
            // Arrange
            var fs = new FakeVirtualFileStream(new byte[]
            {
                0, 5,0,0,0 // empty 5 bytes entry at beginning of file.
            });

            var instance = new FileStuff(fs);
            var (index, freespace) = instance.ScanFile();

            // Act
            var numberOfFreeSpacesBeforeAct = freespace.Count;
            var numberOfBlobsBeforeAct = index.Count;

            // should generate bytes for type, raw length, and data length.
            var key = instance.InsertBlob(index, freespace, new byte[] { 1, 2, 3 });

            // Assert
            Assert.AreEqual(1, numberOfFreeSpacesBeforeAct);
            Assert.AreEqual(0, numberOfBlobsBeforeAct);

            Assert.AreEqual(1, freespace.Count);
            Assert.AreEqual(1, index.Count);

            Assert.AreEqual(5u, index[key]);

            Assert.IsTrue(IEnumerableComparer.AreEqual(fs.Data, new byte[] { 0, 5,0,0,0, 1, 12,0,0,0, 1,0,0,0, 1,2,3 }));
        }

        [TestMethod]
        public void Can_insert_blob_in_big_empty_space()
        {
            // Arrange
            var fs = new FakeVirtualFileStream(new byte[]
            {
                0, 20,0,0,0, 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0 // empty 20 bytes entry at beginning of file.
            });

            var instance = new FileStuff(fs);
            var (index, freespace) = instance.ScanFile();

            // Act
            var numberOfFreeSpacesBeforeAct = freespace.Count;
            var numberOfBlobsBeforeAct = index.Count;

            // should generate bytes for type, raw length, and data length.
            var key = instance.InsertBlob(index, freespace, new byte[] { 1, 2, 3 });

            // Assert
            Assert.AreEqual(1, numberOfFreeSpacesBeforeAct);
            Assert.AreEqual(0, numberOfBlobsBeforeAct);

            Assert.AreEqual(1, freespace.Count);
            Assert.AreEqual(1, index.Count);

            Assert.AreEqual(0u, index[key]);

            Assert.IsTrue(IEnumerableComparer.AreEqual(fs.Data, new byte[] { 1, 12, 0, 0, 0, 1, 0, 0, 0, 1, 2, 3,   0, 8,0,0,0, 0,0,0 }));
        }

        [TestMethod]
        public void Can_insert_blob_but_space_not_at_least_5_bytes_larger()
        {
            // Arrange
            var fs = new FakeVirtualFileStream(new byte[]
            {
                0, 14,0,0,0, 0,0,0,0,0,0,0,0,0 // empty 14 bytes entry at beginning of file.
            });

            var instance = new FileStuff(fs);
            var (index, freespace) = instance.ScanFile();

            // Act
            var numberOfFreeSpacesBeforeAct = freespace.Count;
            var numberOfBlobsBeforeAct = index.Count;

            // should generate bytes for type, raw length, and data length.
            var key = instance.InsertBlob(index, freespace, new byte[] { 1, 2, 3 });

            // Assert
            Assert.AreEqual(1, numberOfFreeSpacesBeforeAct);
            Assert.AreEqual(0, numberOfBlobsBeforeAct);

            Assert.AreEqual(1, freespace.Count);
            Assert.AreEqual(1, index.Count);

            Assert.AreEqual(14u, index[key]);

            Assert.IsTrue(IEnumerableComparer.AreEqual(fs.Data, new byte[] { 0, 14, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  1, 12, 0, 0, 0, 1, 0, 0, 0, 1, 2, 3}));
        }
    }
}
