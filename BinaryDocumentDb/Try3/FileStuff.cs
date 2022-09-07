﻿using BinaryDocumentDb.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BinaryDocumentDb
{
    internal class FileStuff
    {
        public FileStuff(IVirtualFileStream fs)
        {
            this.fs = fs;
        }

        const byte EMPTY_ENTRY = 0;
        const byte BLOB_ENTRY = 1;
        const uint HEADER_BYTES_SIZE = 1;
        const uint LENGTH_VALUE_BYTES_SIZE = 4;
        const uint KEY_VALUE_BYTES_SIZE = 4;
        const uint FULL_EMPTY_ENTRY_BYTES_SIZE = HEADER_BYTES_SIZE + LENGTH_VALUE_BYTES_SIZE;

        private readonly IVirtualFileStream fs;
        private SharedMemory sharedMem = new SharedMemory();

        private uint nextKey = 1;

        internal (Dictionary<uint, uint> index, List<FreeSpaceEntry> freeSpaces) ScanFile()
        {
            var keyToPhysicalOffsetInFile = new Dictionary<uint, uint>();
            var freeSpacesInFile = new List<FreeSpaceEntry>();

            fs.Seek(0, SeekOrigin.Begin);

            if (fs.Length > 0)
                importDataFromFileStream(keyToPhysicalOffsetInFile, freeSpacesInFile);
            else
                saveDefaultFileForFutureUse(freeSpacesInFile);

            // reset next available key
            nextKey = (keyToPhysicalOffsetInFile.Keys?.Count > 0)
                         ? keyToPhysicalOffsetInFile.Keys.Max() + 1
                         : 1; // begin at 1 if no entries.

            return (keyToPhysicalOffsetInFile, freeSpacesInFile);
        }

        /// <summary>
        /// Insert a blob into the database file, and return a unique key bywhich we can read the blob.
        /// </summary>
        internal uint InsertBlob(
            Dictionary<uint, uint> keyToPhysicalOffsetInFile,
            List<FreeSpaceEntry> freeSpacesInFile,
            byte[] blob)
        {
            // find empty space, or move to end of file.
            // Entry needs to be an exact match for the size of the blob OR enough space for the blob + an entry for the remaining space.

            var rawLength = blob.Length + HEADER_BYTES_SIZE + LENGTH_VALUE_BYTES_SIZE + KEY_VALUE_BYTES_SIZE;
            var entryPlusEmptyLength = rawLength + FULL_EMPTY_ENTRY_BYTES_SIZE;

            var availableSpace = freeSpacesInFile.Where(e => e.Length == rawLength || e.Length >= entryPlusEmptyLength)
                                                 .Lowest(e => e.Length);

            if(availableSpace?.Length == rawLength)
                return saveBlobAtEmptySpaceWithNoNewEmptySpaceEntryNecessary();

            else if(availableSpace?.Length >= entryPlusEmptyLength)
                return saveBlobAtEmptySpaceWithNewEmptySpaceEntryAtEnd();

            else
                return saveBlobAtEndOfStream();
           
            #region local methods

            uint saveBlobAtEmptySpaceWithNewEmptySpaceEntryAtEnd()
            {
                // write blob at available space (and add to index).
                fs.Seek(availableSpace!.Offset, SeekOrigin.Begin);
                var key = writeBlobEntryToDiskAtCurrentPositionAndIndexDictionary(keyToPhysicalOffsetInFile, blob);

                // write new empty entry in remaining space.
                var rawLengthToLose = blob.Length + HEADER_BYTES_SIZE + LENGTH_VALUE_BYTES_SIZE + KEY_VALUE_BYTES_SIZE;
                var newAvailableSpaceLength = availableSpace.Length - rawLengthToLose;

                writeFreeSpaceEntryToDiskAtCurrentPositionAndListEntry(freeSpacesInFile, (uint)newAvailableSpaceLength);
                freeSpacesInFile.Remove(availableSpace); // old entry no longer relevant as we created a new one

                // for the user.
                return key;
            }

            uint saveBlobAtEmptySpaceWithNoNewEmptySpaceEntryNecessary()
            {
                // need to overwrite empty space entry with blob entry.
                fs.Seek(availableSpace.Offset, SeekOrigin.Begin);
                var key = writeBlobEntryToDiskAtCurrentPositionAndIndexDictionary(keyToPhysicalOffsetInFile, blob);

                // need to remove the empty entry from list of empty entries. (exactly overwritten on disk)
                freeSpacesInFile.Remove(availableSpace);

                // for the user.
                return key;
            }

            uint saveBlobAtEndOfStream()
            {
                // seek the end of the file, which is where we can write this blob.
                fs.Seek(0, SeekOrigin.End);

                var key = writeBlobEntryToDiskAtCurrentPositionAndIndexDictionary(keyToPhysicalOffsetInFile, blob);

                // for the user.
                return key;
            }

            #endregion
        }

        #region scan file stream

        private void importDataFromFileStream(
            Dictionary<uint, uint> keyToPhysicalOffsetInFile,
            List<FreeSpaceEntry> freeSpacesInFile)
        {
            do
            {
                var entryType = readByte();

                switch (entryType)
                {
                    case EMPTY_ENTRY:
                        processEmptyEntry(freeSpacesInFile);
                        break;

                    case BLOB_ENTRY:
                        processBlobEntry(keyToPhysicalOffsetInFile);
                        break;

                    default: throw new NotImplementedException();
                }
            }
            while (fs.Position < fs.Length);
        }

        private void processEmptyEntry(List<FreeSpaceEntry> freeSpacesInFile)
        {
            // number of bytes including header byte, so minimum of 5.
            var rawLength = readUInt();

            // 5 = entry type (1 byte) + data length (4 bytes)
            freeSpacesInFile.Add(new FreeSpaceEntry((uint)(fs.Position - 5), rawLength));

            var dataLength = rawLength - 5;

            bool anyMoreData = (fs.Position + dataLength < fs.Length);
            if (anyMoreData)
                fs.Seek(fs.Position + dataLength, SeekOrigin.Begin); // skip empty data in file, which should now point to next entry (or out of file)
            else
                fs.Seek(0, SeekOrigin.End); // seek end of file as we've no more data.
        }

        private void processBlobEntry(Dictionary<uint, uint> keyToPhysicalOffsetInFile)
        {
            var rawLength = readUInt();

            // idiot check.
            if (rawLength > 9)
            {
                var key = readUInt();

                keyToPhysicalOffsetInFile.Add(key, (uint)(fs.Position - 9));

                // seek end of blob, without accidentally adding a zero byte to the stream?
                var offset = fs.Position + rawLength - 9;
                
                fs.Seek(offset, SeekOrigin.Begin);
            }
            else
            {
                throw new Exception("Bad data? length of blob is zero or less??");
            }
        }

        private void saveDefaultFileForFutureUse(List<FreeSpaceEntry> freeSpacesInFile)
        {
            // 0,0,0,0,0  where 0 = EMPTY ENTRY, and {0,0,0,0} = 0 == length of data entry.
            // So we waste 5 bytes?  whoopy-doo!

            writeByte(EMPTY_ENTRY);
            writeUInt(5); // length of entry including header byte.

            freeSpacesInFile.Clear();
            freeSpacesInFile.Add(new FreeSpaceEntry(0, 5)); // empty entry, with length of 0.  Wastes 5 bytes!
        }

        #endregion

        #region utility methods

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

        private void writeByte(byte value)
        {
            fs.WriteByte(value);
        }

        private void writeUInt(uint value)
        {
            sharedMem.UInt = value;

            fs.Write(new byte[] { sharedMem.Byte0, sharedMem.Byte1, sharedMem.Byte2, sharedMem.Byte3, }, 0, 4);
        }

        private uint getNextKey()
        {
            return nextKey++;
        }

        private uint writeBlobEntryToDiskAtCurrentPositionAndIndexDictionary(
            Dictionary<uint, uint> keyToPhysicalOffsetInFile,
            byte[] blob)
        {
            // remember this Position for the 'index' dictionary.
            var offset = (uint)fs.Position;

            // write blob entry (header)
            //------------------
            writeByte(BLOB_ENTRY);

            // save to disk
            var key = writeBlobDataToDiskAtCurrentPosition(blob);

            // add to dictionary
            keyToPhysicalOffsetInFile.Add(key, offset);

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
            writeUInt((uint)blob.Length + HEADER_BYTES_SIZE + LENGTH_VALUE_BYTES_SIZE + KEY_VALUE_BYTES_SIZE);

            // key:
            writeUInt(key);

            // blob data:
            fs.Write(blob, 0, blob.Length);

            // key for dictionary entry in KeyToPhysicalOffsetInFile
            return key;
        }

        /// <summary>
        /// length = number of bytes on disk, therefore the space + header + length values.
        /// </summary>
        private void writeFreeSpaceEntryToDiskAtCurrentPositionAndListEntry(
            List<FreeSpaceEntry> freeSpacesInFile,
            uint remainingDataSpace)
        {
            var position = (uint)fs.Position;

            // header byte
            writeByte(EMPTY_ENTRY);

            // length of free space after the header
            writeUInt(remainingDataSpace);

            // NOTE: length is the number of bytes of free space AFTER the 'length' entry on disk.
            freeSpacesInFile.Add(new FreeSpaceEntry(position, remainingDataSpace));
        }



        #endregion
    }
}
