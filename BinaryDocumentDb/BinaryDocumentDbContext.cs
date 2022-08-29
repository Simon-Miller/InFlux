using System;
using System.IO;

namespace BinaryDocumentDb
{
    public class BinaryDocumentDbContext : IDisposable
    {
        #region construct / dispose / destruct

        public BinaryDocumentDbContext(BdDbConfig config)
        {
            this.config = config;

            this.fs = new FileStream(config.FilePathAndName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            this.scanFile();
        }

        private bool isDisposed = false;

        public void Dispose()
        {
            if (this.isDisposed == false)
            {
                this.fs.Close();
            }
        }

        ~BinaryDocumentDbContext()
        {
            this.Dispose();
        }

        #endregion

        #region fields

        private readonly BdDbConfig config;
        private readonly FileStream fs;

        private SharedMemory sharedMem = new SharedMemory();

        private readonly BlobIndex blobIndex = new BlobIndex();
        private readonly FreeSpace freeSpace = new FreeSpace();

        #endregion



        #region helper methods

        private void scanFile()
        {
            this.fs.Seek(0, SeekOrigin.Begin);
            if (this.fs.Length > 0)
            {
                int blobIndexFileOffset = readInt();
                int freeSpaceIndexOffset = readInt();

                var numberOfDataIndexEntriesOnDisk = readInt(blobIndexFileOffset);
                blobIndex.Index.Clear();
                var numberOfBytes = numberOfDataIndexEntriesOnDisk * 8;
                var bytes = new byte[numberOfBytes];
                fs.Read(bytes, 0, numberOfBytes);

                blobIndex.Deserialize(bytes);

                var numberOfFreeSpaceEntries = readInt(freeSpaceIndexOffset);
                freeSpace.Collection.Clear();
                numberOfBytes = numberOfFreeSpaceEntries * 8;
                bytes = new byte[numberOfBytes];
                fs.Read(bytes, 0, numberOfBytes);

                freeSpace.Deserialize(bytes);
            }
            else
            {
                // create defaults, and save to file.
                this.blobIndex.Index.Clear();
                this.freeSpace.Collection.Clear();

                // Bytes 0 to 3
                const uint BlobIndexFileOffset = 8; // after two pointers.

                // bytes 4 to 7
                const uint FreeSpaceIndexOffset = 12; // blob index size is 0. So only 4 bytes stored.  Therefore offset of 12 bytes.

                fs.Seek(0, SeekOrigin.Begin);

                // 0:
                writeUInt(BlobIndexFileOffset); // after pointer to free space

                // 4:
                writeUInt(FreeSpaceIndexOffset);

                // 8: BlobIndex
                writeUInt(0); // zero entries!

                // Free space index here.
                // calculate where free space begins as after the ONE entry in the table.  So 4 bytes for number of entries, then 8 bytes for entry.
                // So 12 bytes.  
                var freeSpaceEntry = new FreeSpaceEntry(24, 4096 - 24);

                // 12: free space index number of entries
                writeUInt(1); // one entry.

                // 16: entry offset
                writeUInt(freeSpaceEntry.Offset);

                // 20: length of free space
                writeUInt(freeSpaceEntry.Length);

                // 24 - 4k - empty space.
                for (int i = 0; i < 4096 - 24; i++)
                    fs.WriteByte(0);

                // there's nothing else to write to the file.
            }
        }

        private uint readUInt(int? offset = null)
        {
            if (offset is null)
                offset = (int)fs.Position;
            else
                fs.Seek(offset.Value, SeekOrigin.Begin);

            var uintBuffer = new byte[4];
            fs.Read(uintBuffer, 0, 4);

            sharedMem.Byte0 = uintBuffer[0];
            sharedMem.Byte1 = uintBuffer[1];
            sharedMem.Byte2 = uintBuffer[2];
            sharedMem.Byte3 = uintBuffer[3];

            return sharedMem.UInt;
        }

        private int readInt(int? offset = null)
        {
            if (offset is null)
                offset = (int)fs.Position;
            else
                fs.Seek(offset.Value, SeekOrigin.Begin);

            var uintBuffer = new byte[4];
            fs.Read(uintBuffer, 0, 4);

            sharedMem.Byte0 = uintBuffer[0];
            sharedMem.Byte1 = uintBuffer[1];
            sharedMem.Byte2 = uintBuffer[2];
            sharedMem.Byte3 = uintBuffer[3];

            return sharedMem.Int;
        }

        private void writeUInt(uint value)
        {
            sharedMem.UInt = value;

            fs.Write(new byte[] { sharedMem.Byte0, sharedMem.Byte1, sharedMem.Byte2, sharedMem.Byte3, }, 0, 4);
        }

        #endregion
    }
}
