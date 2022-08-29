using BinaryDocumentDb.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BinaryDocumentDb
{
    public class DocDbFileContext
    {
        #region construct / dispose / destruct

        public DocDbFileContext(BdDbConfig config)
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

        ~DocDbFileContext()
        {
            this.Dispose();
        }

        #endregion

        const byte EMPTY_ENTRY = 0;
        const byte BLOB_ENTRY = 1;
        const uint HEADER_BYTES_SIZE = 1;
        const uint LENGTH_VALUE_BYTES_SIZE = 4;
        const uint KEY_VALUE_BYTES_SIZE = 4;
        const uint FULL_EMPTY_ENTRY_BYTES_SIZE = HEADER_BYTES_SIZE + LENGTH_VALUE_BYTES_SIZE;

        private readonly BdDbConfig config;
        private readonly FileStream fs;

        private SharedMemory sharedMem = new SharedMemory();

        /// <summary>
        /// Use this to begin with to directly access files.
        /// TODO: Cache all blobs reads for 10 seconds by default, so we can avoid reading the file.
        /// The offset value points to the LENGTH of the blob.  The blob includes the Key (to be ignored) as well as the data.
        /// </summary>
        public readonly Dictionary<uint, uint> KeyToPhysicalOffsetInFile = new Dictionary<uint, uint>();
        public readonly List<FreeSpaceEntry> FreeSpacesInFile = new List<FreeSpaceEntry>();

        private uint nextKey = 1;
        private uint getNextKey()
        {
            return nextKey++;
        }

        /// <summary>
        /// Insert a blob into the database file, and return a unique key bywhich we can read the blob.
        /// </summary>
        public uint InsertBlob(byte[] blob)
        {
            // find empty space, or move to end of file.
            // Entry needs to be an exact match for the size of the blob OR enough space for the blob + an entry for the remaining space.
            var availableSpace = FreeSpacesInFile.Where(e => e.Length == blob.Length
                                                          || e.Length >= (blob.Length + FULL_EMPTY_ENTRY_BYTES_SIZE)
                                                       )?.Lowest(x => x.Length);
            if (availableSpace is null)
            {
                // seek the end of the file, which is where we can write this blob.
                fs.Seek(fs.Length, SeekOrigin.Begin);

                var key = writeBlobEntryToDiskAtCurrentPositionAndIndexDictionary(blob);

                // for the user.
                return key;
            }
            else
            {
                // seek the position to start writing the blob. entry.
                fs.Seek(availableSpace.Offset, SeekOrigin.Begin);

                var key = writeBlobEntryToDiskAtCurrentPositionAndIndexDictionary(blob);

                // create an empty space AFTER the blob entry.
                var blobSizePlusMetaData = blob.Length + HEADER_BYTES_SIZE + LENGTH_VALUE_BYTES_SIZE + KEY_VALUE_BYTES_SIZE;

                var remainingDataSpace = (uint)(availableSpace.Length - blob.Length);
                writeFreeSpaceEntryToDiskAtCurrentPositionAndListEntry(remainingDataSpace);

                // for the user.
                return key;
            }
        }

        /// <summary>
        /// length = number of bytes on disk, therefore the space + header + length values.
        /// </summary>
        private void writeFreeSpaceEntryToDiskAtCurrentPositionAndListEntry(uint remainingDataSpace)
        {
            var position = (uint)fs.Position;

            // header byte
            writeByte(EMPTY_ENTRY);

            // length of free space after the header
            writeUInt(remainingDataSpace);

            // NOTE: length is the number of bytes of free space AFTER the 'length' entry on disk.
            FreeSpacesInFile.Add(new FreeSpaceEntry(position, remainingDataSpace));
        }

        private uint writeBlobEntryToDiskAtCurrentPositionAndIndexDictionary(byte[] blob)
        {
            // write blob entry (header)
            //------------------
            writeByte(BLOB_ENTRY);

            // remember this Position for the 'index' dictionary.
            var offset = (uint)fs.Position;

            // save to disk
            var key = writeBlobDataToDiskAtCurrentPosition(blob);

            // add to dictionary
            KeyToPhysicalOffsetInFile.Add(key, offset);

            // for use to keep hold of.
            return key;
        }

        /// <summary>
        /// does not include the header.  Length, Key, and Data only and in that order.
        /// Returns the Key generated when saving.
        /// </summary>
        private uint writeBlobDataToDiskAtCurrentPosition(byte[] blob)
        {
            var key = getNextKey();

            // length:
            writeUInt((uint)blob.Length);

            // key:
            writeUInt(key);

            // blob data:
            fs.Write(blob, 0, blob.Length);

            // key for dictionary entry in KeyToPhysicalOffsetInFile
            return key;
        }



        public byte[]? GetBlob(uint key)
        {
            var position = KeyToPhysicalOffsetInFile[key];
            fs.Seek(position, SeekOrigin.Begin);

            var length = readUInt();
            var _ = readUInt();

            byte[] blob = new byte[length];
            fs.Read(blob, 0, (int)length);

            return blob;
        }



        /// <summary>
        /// Only called from constructor.
        /// </summary>
        private void scanFile()
        {
            this.fs.Seek(0, SeekOrigin.Begin);

            if (this.fs.Length > 0)
                importDataFromFileStream();
            else
                saveDefaultFileForFutureUse();
        }

        private void importDataFromFileStream()
        {
            do
            {
                var entryType = readByte();

                switch (entryType)
                {
                    case EMPTY_ENTRY:
                        processEmptyEntry();
                        break;

                    case BLOB_ENTRY:
                        processBlobEntry();
                        break;

                    default: throw new NotImplementedException();
                }
            }
            while (fs.Position < fs.Length);

            // reset next available key
            this.nextKey = (KeyToPhysicalOffsetInFile.Keys?.Count > 0)
                         ? KeyToPhysicalOffsetInFile.Keys.Max() + 1 
                         : 1; // begin at 1 if no entries.
        }

        private void processEmptyEntry()
        {
            var dataLength = readUInt();

            if (dataLength > 0)
            {
                // 5 = entry type (1 byte) + data length (4 bytes)
                FreeSpacesInFile.Add(new FreeSpaceEntry((uint)(fs.Position - 5), dataLength + 5));
            }

            bool anyMoreData = (fs.Position + dataLength < fs.Length);
            if (anyMoreData)
                fs.Seek(fs.Position + dataLength, SeekOrigin.Begin); // skip empty data in file, which should now point to next entry (or out of file)
            else
                fs.Seek(fs.Length, SeekOrigin.Begin); // seek end of file as we've no more data.
        }

        private void processBlobEntry()
        {
            var dataLength = readUInt();
            if (dataLength > 4)
            {
                var key = readUInt();

                KeyToPhysicalOffsetInFile.Add(key, (uint)(fs.Position - 8));
            }
        }

        private void saveDefaultFileForFutureUse()
        {
            // 0,0,0,0,0  where 0 = EMPTY ENTRY, and {0,0,0,0} = 0 == length of data entry.
            // So we waste 5 bytes?  whoopy-doo!

            writeByte(EMPTY_ENTRY);
            writeUInt(0); // length of entry.
        }

        private void writeByte(byte value)
        {
            fs.WriteByte(value);
        }

        private void writeUInt(uint value)
        {
            sharedMem.UInt = value;

            fs.Write(new byte[] { sharedMem.Byte0, sharedMem.Byte1, sharedMem.Byte2, sharedMem.Byte3, }, 0, 4);
        }

        private byte readByte(int? offset = null)
        {
            if (offset != null)
                fs.Seek(offset.Value, SeekOrigin.Begin);

            return (byte)fs.ReadByte();
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
    }
}
