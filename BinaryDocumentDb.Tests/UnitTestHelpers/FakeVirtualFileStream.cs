using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace BinaryDocumentDb.Tests.UnitTestHelpers
{
    internal class FakeVirtualFileStream : IVirtualFileStream
    {
        public FakeVirtualFileStream(IEnumerable<byte>? startData = null)
        {
            Data = new List<byte>();

            if (startData != null)
                Data.AddRange(startData);
        }

        public readonly List<byte> Data;

        public long Length => Data.Count;

        public long Position { get; set; } = 0;

        public void Close()
        {
            // nothing to close in this fake.
        }

        public int Read(byte[] array, int offset, int count)
        {
            var bytesRead = 0;
            for (int i = 0; i < count; i++)
            {
                if (Position < Length)
                {
                    array[offset + i] = Data[(int)Position];
                    Position++;
                    bytesRead++;
                }
            }

            return bytesRead;
        }

        public int ReadByte()
        {
            int value = -1;
            if (Position < Length)
            {
                value = (int)Data[(int)Position];
                Position++;
            }

            return value; // -1 == end of stream, OR the byte value.
        }

        public long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = Length + offset; // expects negative numbers.  So length-1 should read the last byte when asked.
                    break;
                default:
                    throw new NotImplementedException();
            }

            return Position;
        }

        public void Write(byte[] array, int offset, int count)
        {
            var end = offset + count;
            for (int i = offset; i < end; i++)
                WriteByte(array[i]);
        }

        public void WriteByte(byte value)
        {
            if (Position < Length)
                Data[(int)Position] = value;
            else
            {
                // add empty 0's if the position is beyond the end of the file.
                var emptySpaces = Position - Length;
                for (int i = 0; i < emptySpaces; i++)
                    Data.Add(0);

                // add the byte now in the correct position.
                Data.Add(value);
            }

            Position++;
        }
    }

    [TestClass]
    public class FakeVirtualFileStreamTests
    {
        [TestMethod]
        public void Length_Correct()
        {
            // Arrange
            var target1 = new FakeVirtualFileStream(); // no data
            var target2 = new FakeVirtualFileStream(new byte[] { 1, 2, 3 });

            var target3 = new FakeVirtualFileStream(new byte[] { 1, 2, 3 });
            var target4 = new FakeVirtualFileStream(new byte[] { 1, 2, 3 });

            // Act
            target3.Seek(2, SeekOrigin.Begin);
            target3.WriteByte(4);

            target4.Seek(3, SeekOrigin.Begin);
            target4.WriteByte(4);

            // Assert
            Assert.AreEqual(0, target1.Length);
            Assert.AreEqual(3, target2.Length);

            Assert.AreEqual(1, target2.Data[0]);
            Assert.AreEqual(2, target2.Data[1]);
            Assert.AreEqual(3, target2.Data[2]);

            Assert.AreEqual(3, target3.Length);
            Assert.AreEqual(4, target3.Data[2]);
            Assert.AreEqual(4, target4.Length);
            Assert.AreEqual(4, target4.Data[3]);
        }

        [TestMethod]
        public void ReadByte_works()
        {
            // Arrange
            var source = new FakeVirtualFileStream(new byte[] { 1, 2, 3 });

            // Act
            var byte0 = source.ReadByte();
            var byte1 = source.ReadByte();
            var byte2 = source.ReadByte();
            var byte3 = source.ReadByte(); // should be -1

            // Assert
            Assert.AreEqual(1, byte0);
            Assert.AreEqual(2, byte1);
            Assert.AreEqual(3, byte2);
            Assert.AreEqual(-1, byte3);
        }

        [TestMethod]
        public void Read_works()
        {
            // Arrange
            var source = new FakeVirtualFileStream(new byte[] { 1, 2, 3 });

            var target = new byte[] { 5, 5, 5, 5, 5, 5, 5, 5, 5, 5 }; // 10 x 5's

            // Act
            int bytesRead = source.Read(target, 2, 3);

            // Assert
            Assert.AreEqual(5, target[0]);
            Assert.AreEqual(5, target[1]);
            Assert.AreEqual(1, target[2]); // inserted
            Assert.AreEqual(2, target[3]); // inserted
            Assert.AreEqual(3, target[4]); // inserted
            Assert.AreEqual(5, target[5]);
            Assert.AreEqual(5, target[6]);
            Assert.AreEqual(5, target[7]);
            Assert.AreEqual(5, target[8]);
            Assert.AreEqual(5, target[9]);

            Assert.AreEqual(3, bytesRead);
        }

        [TestMethod]
        public void WriteByte_works()
        {
            // Arrange
            var target1 = new FakeVirtualFileStream();
            var target2 = new FakeVirtualFileStream(new byte[] { 1, 2, 3 });

            // Act
            target1.WriteByte(255);
            var pos1 = target1.Position;

            var pos2 = target2.Seek(3, SeekOrigin.Begin);
            target2.WriteByte(255);

            // Assert
            Assert.AreEqual(1, pos1);
            Assert.AreEqual(3, pos2);
            Assert.AreEqual(255, target1.Data[0]);
            Assert.AreEqual(255, target2.Data[3]);
        }

        /// <summary>
        /// unit test saved the day!
        /// </summary>
        [TestMethod]
        public void Write_works()
        {
            // Arrange
            var target1 = new FakeVirtualFileStream(); // write all to end of stream
            var target2 = new FakeVirtualFileStream(new byte[] { 1, 2 }); // half current data, half new.
            var target3 = new FakeVirtualFileStream(new byte[] { 1, 2, 3, 4 }); // completely overwrite current
            var target4 = new FakeVirtualFileStream(new byte[] { 1, 2 }); // to write to beyond end of stream

            var dataToWrite = new byte[] { 5, 6, 7, 8 };

            // Act
            target1.Write(dataToWrite, 1, 2); // write 2 bytes to end of stream
            target2.Write(dataToWrite, 0, 4); // overwrite 2 bytes of current data, and extend stream by 2 bytes.
            target3.Write(dataToWrite, 0, 4); // overwrite existing data, but stream should remain the same length.
            target4.Seek(2, SeekOrigin.End);  // positioned on 3rd byte beyond last byte in stream. 
            target4.WriteByte(255);           // should fill previous 2 spaces with a zero, the target space with 255, and position +1

            // BOOM!  Found writing was wrong.  
            Assert.AreEqual(6, target1.Data[0]);
            Assert.AreEqual(7, target1.Data[1]);
            Assert.AreEqual(2, target1.Length);

            Assert.AreEqual(5, target2.Data[0]);
            Assert.AreEqual(6, target2.Data[1]);
            Assert.AreEqual(7, target2.Data[2]);
            Assert.AreEqual(8, target2.Data[3]);
            Assert.AreEqual(4, target2.Length);

            Assert.AreEqual(5, target3.Data[0]);
            Assert.AreEqual(6, target3.Data[1]);
            Assert.AreEqual(7, target3.Data[2]);
            Assert.AreEqual(8, target3.Data[3]);
            Assert.AreEqual(4, target3.Length);

            Assert.AreEqual(5, target4.Length);
            Assert.AreEqual(5, target4.Position); // 5th position = end + 1
            Assert.IsTrue(IEnumerableComparer.AreEqual(target4.Data, new byte[] { 1, 2, 0, 0, 255 }));
        }

        /// <summary>
        /// unit test saved the day! x 2
        /// </summary>
        [TestMethod]
        public void Seek_works()
        {
            // Arrange
            var target = new FakeVirtualFileStream(new byte[] { 1, 2, 3, 4 });

            // Act
            target.Seek(1, SeekOrigin.Begin);
            var result1 = target.ReadByte();

            target.Seek(1, SeekOrigin.Current); // as we just read a byte (0x02) we should be at position 2, so this means offset to 3, please.
            var result2 = target.ReadByte();

            target.Seek(-2, SeekOrigin.End); // 2 from end = 1 == 0x02 ??
            var result3 = target.ReadByte();

            // behaviour of seeking beyond end of stream.
            target.Seek(5, SeekOrigin.Begin); // position beyond length, but do nothing unless you Write or WriteByte.

            // Assert
            Assert.AreEqual(2, result1);
            Assert.AreEqual(4, result2);
            Assert.AreEqual(3, result3);
            Assert.AreEqual(4, target.Length); // still the same despite pointing beyond stream.
        }
    }
}
