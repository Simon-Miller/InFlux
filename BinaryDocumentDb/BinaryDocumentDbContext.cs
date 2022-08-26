using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BinaryDocumentDb
{
    public class BinaryDocumentDbContext : IDisposable
    {
        public BinaryDocumentDbContext(BdDbConfig config)
        {
            this.config = config;

            this.fs = new FileStream(config.FilePathAndName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            this.scanFile();
        }

        ~BinaryDocumentDbContext()
        {
            this.Dispose();
        }

        private readonly BdDbConfig config;
        private readonly FileStream fs;

        private bool isDisposed = false;

        private SharedMemory sharedMem = new SharedMemory();

        private readonly Dictionary<uint, uint> indexEntries = new Dictionary<uint, uint>();
        private int numberOfDataIndexEntriesOnDisk = 0;

        private struct FreeSpaceEntry 
        {
            public FreeSpaceEntry(uint offset, uint length)
            {
                this.Offset = offset;
                this.Length = length;
            }

            public uint Offset;
            public uint Length;
        };

        //private readonly Dictionary<uint, {uint offset, uint length}> freeSpaceIndexEntries = new Dictionary<uint, {uint offset uint length}>();
        private readonly Dictionary<uint, FreeSpaceEntry> freeSpaceIndexEntries = new Dictionary<uint, FreeSpaceEntry>();
        private int numberOfFreeSpaceIndexEntriesOnDisk = 0;

        // when we want to save data, this is returned as the index key to use t
        private int FindNextKey = 1;

        public void Dispose()
        {
            if (this.isDisposed == false)
            {
                this.fs.Close();
            }
        }

        private void scanFile()
        {
            this.fs.Seek(0, SeekOrigin.Begin);
            if(this.fs.Length > 0)
            {
                int indexDataOffset = readInt();
                int freeSpaceIndexOffset = readInt();

                numberOfDataIndexEntriesOnDisk = readInt(indexDataOffset);
                this.indexEntries.Clear();
                for(int i=0; i< numberOfDataIndexEntriesOnDisk; i++)
                {
                    var key = readUInt();
                    var value = readUInt();

                    // zero indicates the index is not used, and waiting to be filled.
                    if(key != 0)
                        this.indexEntries.Add(key, value);
                }

                // TODO: this is wrong.  says 96!  Must have missed something??
                this.numberOfFreeSpaceIndexEntriesOnDisk = readInt(freeSpaceIndexOffset);
                for(uint i=0; i< numberOfFreeSpaceIndexEntriesOnDisk; i++)
                {
                    this.freeSpaceIndexEntries.Add(i, new FreeSpaceEntry(readUInt(), readUInt()));
                }
            }
            else
            {
                // create defaults, and save to file.
                this.indexEntries.Clear();
                this.freeSpaceIndexEntries.Clear();

                fs.Seek(0, SeekOrigin.Begin);
                writeUInt(8); // after pointer to free space!

                var emptyBuffer = new byte[config.InitialIndexLength * 4 * 2];

                // Write pointer to free space index? 
                var freeSpaceIndexOffset = (uint)(emptyBuffer.Length + 12);
                writeUInt(freeSpaceIndexOffset); // hope its right.

                writeUInt(config.InitialIndexLength);
                            
                for (int i = 0; i < emptyBuffer.Length; i++)
                    emptyBuffer[i] = 0;

                fs.Write(emptyBuffer, 0, emptyBuffer.Length);

                // write free space index count, here.
                this.freeSpaceIndexEntries.Add(1, new FreeSpaceEntry(freeSpaceIndexOffset + 4, 2048));
                writeUInt((uint)(freeSpaceIndexOffset + 4));
                writeUInt(2048);

                // write free space data!  All 2k of it?

                for (int i = 0; i < 2048; i++)
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
    }
}
