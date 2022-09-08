using BinaryDocumentDb.Extensions;
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
            {
                importDataFromFileStream(keyToPhysicalOffsetInFile, freeSpacesInFile);
                defragmentFreeSpaces(freeSpacesInFile);
            }
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

        private void defragmentFreeSpaces(List<FreeSpaceEntry> freeSpaces)
        {
            // index of entry.  NOTE: Delete in reverse order so each index does not affect the next.
            var entriesToDelete = new List<int>();
            var entriesToAmend = new List<(int index, uint newLength)>();
            var orderedFreeSpaceMap = new List<MemoryBlock>();
            var changesCount = 0;

            do
            {
                // reset
                changesCount = 0;
                entriesToDelete.Clear();
                entriesToAmend.Clear();
                orderedFreeSpaceMap = freeSpaceMap(freeSpaces).OrderBy(x => x.StartOffset).ToList();

                // ideally this re-iterates or does what ever it needs to so we only have to hit the DB file ONCE.
                var results = findFragmentations();

                if(results != null)
                {
                    // needs to happen before deletes, so the index don't change.
                    processInMemoryUpdates(results.Value.updates);

                    // persiste the minimum changes necessary to disk. Also needs to happen before deletes in memory.
                    processStreamUpdates(results.Value.updates);

                    // must happen after updates do we don't change indexes of update entries.                    
                    processInMemoryDeletes(results.Value.deletes); 
                }
            }
            while (changesCount > 0);

            #region local helper methods

            List<MemoryBlock> freeSpaceMap(List<FreeSpaceEntry> freeSpaces)
            {
                // 0,0,0,0,0 empty entry
                //   -
                // 1,15,0,0,0,01,02,03,04,05,06,07,08,09,10 // blob entry
                //   --
                // Offset point to the TYPE byte.

                // free space []
                // 0
                // 20 ?? (if there were an entry above)

                // translation: 0 is:-
                //    from: ptr = 0 
                //    to: ptr + (length of [0] ==  5) - 1 (so the second offset is the LAST byte).
                //    confirm: 0 to 4 = 5 bytes.

                return freeSpaces.Select(x => new MemoryBlock(x.Offset, x.Offset + x.Length - 1)).ToList();
            }

            (IEnumerable<int> deletes, IEnumerable<(int idxToUpdate, uint newLength)> updates)? findFragmentations()
            {
                // idiot check
                if (orderedFreeSpaceMap is null || orderedFreeSpaceMap.Count == 1) return null;

                var deletes = new List<int>();
                var updates = new List<(int idxToUpdate, uint newLength)>();

                int selectedFreeSpaceIndex = 0;

                do
                {
                    var freeSpace = orderedFreeSpaceMap[selectedFreeSpaceIndex];
                    var newStartOffset = freeSpace.StartOffset;
                    var newEndOffset = freeSpace.EndOffset;

                    int compareFreeSpaceIndex = selectedFreeSpaceIndex + 1;
                    var compareFreeSpace = orderedFreeSpaceMap[compareFreeSpaceIndex];

                    if (freeSpace.EndOffset + 1 == compareFreeSpace.StartOffset)
                    {
                        // defrag needed.  We can count this as one merge, but there may be others.
                        // We than therefore iterate over the next one(s) to see if there are more.
                        // in reality the most we are likely to ever see is 3 in a row. (space)(entry just deleted)(space)

                        newEndOffset = compareFreeSpace.EndOffset;

                        deletes.Add(compareFreeSpaceIndex);

                        var tryNext = false;
                        var skipEntriesCount = 1;
                        var nextIndex = compareFreeSpaceIndex + 1;
                        do
                        {
                            tryNext = false;
                            MemoryBlock? nextFreeSpace = orderedFreeSpaceMap.Count > nextIndex ? orderedFreeSpaceMap[nextIndex] : null;

                            if (nextFreeSpace != null)
                            {
                                if (compareFreeSpace.EndOffset + 1 == nextFreeSpace.StartOffset)
                                {
                                    compareFreeSpaceIndex++;
                                    compareFreeSpace = orderedFreeSpaceMap[compareFreeSpaceIndex];

                                    deletes.Add(nextIndex);

                                    nextIndex++;
                                    skipEntriesCount++;

                                    newEndOffset = compareFreeSpace.EndOffset;

                                    tryNext = true;
                                }
                            }
                        }
                        while (tryNext);

                        var calcLength = (newEndOffset - newStartOffset) + 1;

                        updates.Add((selectedFreeSpaceIndex, calcLength));

                        selectedFreeSpaceIndex += skipEntriesCount;
                    }

                    selectedFreeSpaceIndex++;
                }
                while (selectedFreeSpaceIndex < orderedFreeSpaceMap.Count);

                return (deletes, updates);
            }

            void processInMemoryDeletes(IEnumerable<int> indexesToDelete)
            {
                foreach (var idx in indexesToDelete.OrderByDescending(x=>x))
                    freeSpaces.RemoveAt(idx);
            }

            void processInMemoryUpdates(IEnumerable<(int index, uint newLength)> updates)
            {
                foreach (var update in updates)
                {
                    var freeSpaceEntry = freeSpaces[update.index];
                    freeSpaces[update.index] = new FreeSpaceEntry(freeSpaceEntry.Offset, update.newLength);
                }
            }

            void processStreamUpdates(IEnumerable<(int index, uint newLength)> updates)
            {
                foreach (var update in updates)
                {
                    var freeSpaceEntry = freeSpaces[update.index];
                    fs.Seek(freeSpaceEntry.Offset + HEADER_BYTES_SIZE, SeekOrigin.Begin);
                    writeUInt(update.newLength);
                }
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
