using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace BinaryDocumentDb.Tests
{
    [TestClass]
    public class UnitTest1
    {
        //[TestMethod]
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

        //[TestMethod]
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

        //[TestMethod]
        public void testing_behaviour_of_real_FileStream()
        {
            using (var fs = new FileStream("test3.bin", FileMode.OpenOrCreate))
            {
                fs.WriteByte(255); // lenght == 1

                fs.Seek(0, SeekOrigin.End); // position reported as 1 (zero based) on a file length of 1.  So didn't ADD anything.

                fs.Seek(2, SeekOrigin.End); // position reported as 3, but Length still reports 1.  What will happen if I write here?
                // if this the 'filling' I'm worried about?

                fs.WriteByte(254); // length now repoted as 4.  Position also reported as 4.

                fs.Seek(0, SeekOrigin.Begin);
                var x = fs.ReadByte(); // 255, 0, 0, 254
            }
        }

        [TestMethod]
        public void can_map_freespace()
        {
            // Arrange
            var freeSpaces = new List<(int Offset, int Length)>() 
            {
                (1,0),
                (6,10)
            };

            var result = freeSpaces.Select(x => (Start: x.Offset - 1, End: (x.Offset - 1) + x.Length + 4)).ToList();

            Assert.AreEqual(2,result.Count);
            Assert.AreEqual(0, result[0].Start);
            Assert.AreEqual(4, result[0].End);
            Assert.AreEqual(5, result[1].Start);
            Assert.AreEqual(19, result[1].End);
        }

        [TestMethod]
        public void try_defrag_code()
        {
            var orderedMap = new List<(uint StartOffset, uint EndOffset)>() 
            {
                (0,4),
                (5,19)
            };

            // index of entry.  NOTE: Delete in reverse order so each index does not affect the next.
            var entriesToDelete = new List<int>();

            var entriesToAmend = new List<(int index, uint newLength)>();

            for (int i = 0; i < orderedMap.Count; i++)
            {
                var left = orderedMap[i];

                for (int j = 0; j < orderedMap.Count; j++)
                {
                    var right = orderedMap[j];
                    if (left.EndOffset == right.StartOffset - 1)
                    {
                        entriesToDelete.Add(j);

                        var leftOffset = left.StartOffset + 1;
                        var newLength = (right.EndOffset - leftOffset) - 4; // length value

                        entriesToAmend.Add((index: i, newLength: newLength));
                        break;
                    }
                }
            }
        }
    }
}