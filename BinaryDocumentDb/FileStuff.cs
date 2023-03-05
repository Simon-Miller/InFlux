using BinaryDocumentDb.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BinaryDocumentDb
{
    /// <summary>
    /// The real work applied to a file stream.
    /// Code here does not assume there is one file stream only.
    /// Despite the <see cref="ScanFile"/> method returning collections of data, they are note stored in this instance.
    /// Some data must remain instance only, and therefore it makes sense that we don't use statics for consistency across methods.
    /// </summary>
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
        const uint MINIMUM_BLOB_ENTRY_SIZE = HEADER_BYTES_SIZE + LENGTH_VALUE_BYTES_SIZE + KEY_VALUE_BYTES_SIZE;

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
        /// Make the next available key available, so the user can create a blob and know its access key before its saved,
        /// as the access key is stored as part of the blob on disk, this is a useful step.
        /// </summary>
        internal uint ReserveNextKey() =>
            this.getNextKey();
        
        internal void InsertBlobWithKey(
            Dictionary<uint, uint> keyToPhysicalOffsetInFile,
            List<FreeSpaceEntry> freeSpacesInFile,
            uint key,
            byte[] blob)
        {
            var rawLength = (uint)blob.Length + MINIMUM_BLOB_ENTRY_SIZE;
            var entryPlusEmptyLength = rawLength + FULL_EMPTY_ENTRY_BYTES_SIZE;

            var availableSpace = findAvailableSpaceInFile(freeSpacesInFile, rawLength);

            if (availableSpace?.Length == rawLength)
                saveBlobAtEmptySpaceWithNoNewEmptySpaceEntryNecessary();

            else if (availableSpace?.Length >= entryPlusEmptyLength)
                saveBlobAtEmptySpaceWithNewEmptySpaceEntryAtEnd();

            else
                saveBlobAtEndOfStream();

            #region local methods

            void saveBlobAtEmptySpaceWithNewEmptySpaceEntryAtEnd()
            {
                // write blob at available space (and add to index).
                fs.Seek(availableSpace!.Offset, SeekOrigin.Begin);
                writeBlobEntryToDiskAtCurrentPositionAndIndexDictionary(keyToPhysicalOffsetInFile, key, blob);

                // write new empty entry in remaining space.
                var rawLengthToLose = blob.Length + HEADER_BYTES_SIZE + LENGTH_VALUE_BYTES_SIZE + KEY_VALUE_BYTES_SIZE;
                var newAvailableSpaceLength = availableSpace.Length - rawLengthToLose;

                writeFreeSpaceEntryToDiskAtCurrentPositionAndListEntry(freeSpacesInFile, (uint)newAvailableSpaceLength);
                freeSpacesInFile.Remove(availableSpace); // old entry no longer relevant as we created a new one
            }

            void saveBlobAtEmptySpaceWithNoNewEmptySpaceEntryNecessary()
            {
                // need to overwrite empty space entry with blob entry.
                fs.Seek(availableSpace.Offset, SeekOrigin.Begin);
                writeBlobEntryToDiskAtCurrentPositionAndIndexDictionary(keyToPhysicalOffsetInFile, key, blob);

                // need to remove the empty entry from list of empty entries. (exactly overwritten on disk)
                freeSpacesInFile.Remove(availableSpace);
            }

            void saveBlobAtEndOfStream()
            {
                // seek the end of the file, which is where we can write this blob.
                fs.Seek(0, SeekOrigin.End);
                writeBlobEntryToDiskAtCurrentPositionAndIndexDictionary(keyToPhysicalOffsetInFile, key, blob);
            }

            #endregion
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

            var rawLength = (uint)blob.Length + MINIMUM_BLOB_ENTRY_SIZE;
            var entryPlusEmptyLength = rawLength + FULL_EMPTY_ENTRY_BYTES_SIZE;

            var availableSpace = findAvailableSpaceInFile(freeSpacesInFile, rawLength);

            if (availableSpace?.Length == rawLength)
                return saveBlobAtEmptySpaceWithNoNewEmptySpaceEntryNecessary();

            else if (availableSpace?.Length >= entryPlusEmptyLength)
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

                this.Flush();

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

                this.Flush();

                // for the user.
                return key;
            }

            uint saveBlobAtEndOfStream()
            {
                // seek the end of the file, which is where we can write this blob.
                fs.Seek(0, SeekOrigin.End);

                var key = writeBlobEntryToDiskAtCurrentPositionAndIndexDictionary(keyToPhysicalOffsetInFile, blob);

                this.Flush();

                // for the user.
                return key;
            }

            #endregion
        }

        /// <summary>
        /// Read a blob from the database file.  IF the <paramref name="key"/> doesn't exist in the dictionary,
        /// an exception will be thrown.  
        /// Exceptions are also thrown if the offset is found to NOT be pointing at a blob,
        /// or if reading the raw data appears to be corrupt - likely because you're not actually pointing at a blob entry!
        /// </summary>
        internal BlobEntry ReadBlob(Dictionary<uint, uint> keyToPhysicalOffsetInFile, uint key)
        {
            var offset = keyToPhysicalOffsetInFile[key];
            return readBlob(offset);
        }

        /// <summary>
        /// Delete a blob and mark it as empty space.  Also defragments the empty spaces.
        /// </summary>
        internal void DeleteBlob(Dictionary<uint, uint> keyToPhysicalOffsetInFile,
            List<FreeSpaceEntry> freeSpacesInFile,
            uint blobKey)
        {
            // find entry.
            var entryOffset = keyToPhysicalOffsetInFile[blobKey]; // dictionary will throw exception if not exists.
            var byteLengthOfEntry = readEntryLengthOnly(entryOffset);

            // Remove from index.
            keyToPhysicalOffsetInFile.Remove(blobKey);

            // Add to free spaces.
            freeSpacesInFile.Add(new FreeSpaceEntry(entryOffset, byteLengthOfEntry));

            // defrag free spaces.
            defragmentFreeSpaces(freeSpacesInFile);

            // determine if the deleted blob is the beginning of an entry.
            var foundAsEmptyEntry = freeSpacesInFile.FirstOrDefault(x => x.Offset == entryOffset);

            // if so, change the TYPE byte to 0.
            if (foundAsEmptyEntry != null)
                updateEntryTypeToEmptyEntry(entryOffset);
        }

        internal void UpdateBlob(
            Dictionary<uint, uint> keyToPhysicalOffsetInFile,
            List<FreeSpaceEntry> freeSpacesInFile,
            uint key,
            byte[] blobData,
            bool overrideTooSmallError = false)
        {
            var offset = keyToPhysicalOffsetInFile[key];
            var (type, length) = readEntryHeader(offset);

            if (overrideTooSmallError == false && (type != BLOB_ENTRY || length < MINIMUM_BLOB_ENTRY_SIZE))
                throw new Exception("Doesn't smell like a blob entry. Either you're trying to update a free space entry, or something wrong in the file stream as the source data is too small?");

            if (length == blobData.Length + MINIMUM_BLOB_ENTRY_SIZE)
                handleExactMatchScenario();

            else if (blobData.Length + MINIMUM_BLOB_ENTRY_SIZE + FULL_EMPTY_ENTRY_BYTES_SIZE <= length)
            {

                handleUpdateAndInsertEmptyEntryScenario(null);
            }

            else if (blobData.Length + MINIMUM_BLOB_ENTRY_SIZE > length)
            {
                fs.Seek(offset, SeekOrigin.Begin);
                writeFreeSpaceEntryToDiskAtCurrentPositionAndListEntry(freeSpacesInFile, length);

                var rawLength = (uint)blobData.Length + MINIMUM_BLOB_ENTRY_SIZE;
                var availableEntry = findAvailableSpaceInFile(freeSpacesInFile, rawLength);

                if (availableEntry != null)
                    freeSpacesInFile.Remove(availableEntry);

                if (availableEntry != null && availableEntry.Length == rawLength)
                {
                    offset = availableEntry.Offset;
                    handleExactMatchScenario();
                }
                else if (availableEntry != null && availableEntry.Length > rawLength)
                    handleUpdateAndInsertEmptyEntryScenario(availableEntry);

                else
                    handleAddingBlobToEndOfStreamScenario();
            }

            // saving data is up to 4 bytes smaller than the existing entry, meaning we can't re-use the entry
            // and need to mark it as empty space whilst adding the entry to available space or end of file.
            else if ((blobData.Length + MINIMUM_BLOB_ENTRY_SIZE) < length && (blobData.Length + MINIMUM_BLOB_ENTRY_SIZE + FULL_EMPTY_ENTRY_BYTES_SIZE) > length)
            {
                var rawLength = (uint)blobData.Length + MINIMUM_BLOB_ENTRY_SIZE;
                var availableEntry = findAvailableSpaceInFile(freeSpacesInFile, rawLength);

                // three scenarios to consider:
                // 1.  No available space
                // 2.  available space of exact size
                // 3.  Available space at least 5 bytes larger than needed.

                fs.Seek(offset, SeekOrigin.Begin); // in all cases, existing entry needs to turn into empty entry.
                writeFreeSpaceEntryToDiskAtCurrentPositionAndListEntry(freeSpacesInFile, length);

                if (availableEntry is null)
                {
                    // 1. No available space
                    handleAddingBlobToEndOfStreamScenario();
                }
                else
                {
                    if (availableEntry.Length == rawLength)
                    {
                        // 2. available space of exact size.
                        offset = availableEntry.Offset;
                        handleExactMatchScenario();

                        freeSpacesInFile.Remove(availableEntry);
                    }
                    else
                    {
                        // 3. Available space at least 5 bytes larger than needed.
                        handleUpdateAndInsertEmptyEntryScenario(availableEntry);

                        freeSpacesInFile.Remove(availableEntry);
                    }
                }
            }

            // let's ensure our index is clean before returning.
            defragmentFreeSpaces(freeSpacesInFile);

            // done.  return.
            return;

            #region scenarios handlers

            void handleExactMatchScenario()
            {
                // As we know its an exact match in size, we just need to be told where to write into the stream.
                // If we take that from 'offset' then we separate any assumption of it being an existing entry or a new empty space fill.
                fs.Seek(offset, SeekOrigin.Begin);
                writeBlobEntryAtCurrentPosition(key, blobData);

                // update index with new offset?
                updateIndexEntryOffset();
            }

            void handleUpdateAndInsertEmptyEntryScenario(FreeSpaceEntry? availableEntry)
            {
                // need to ensure this works for 2 scenarios:
                // 1.  Where entry is the original blob entry on disk, and we're updating that entry, then add empty entry
                // 2.  Where entry is a new empty space to update with blob entry, then add empty entry

                if(availableEntry != null)
                {
                    // calling code made current disk offset entry into a free space entry.  That means the offset points to the old position.
                    // we need to point to where the data will now be saved.
                    offset = availableEntry.Offset;

                    // length was the original size of the now previous position on disk.  Needs to refrect the size of the free space entry
                    // we're writing into.
                    length = availableEntry.Length;
                }

                // ensure stream is in right place to write.
                fs.Seek(offset, SeekOrigin.Begin);

                // write blob
                writeBlobEntryAtCurrentPosition(key, blobData);

                // write empty space entry for remaining data and add remaining space to freeSpaces
                var remainingSpace = (uint)(length - (blobData.Length + MINIMUM_BLOB_ENTRY_SIZE));
                writeFreeSpaceEntryToDiskAtCurrentPositionAndListEntry(freeSpacesInFile, remainingSpace);
               
                updateIndexEntryOffset();
            }

            void handleAddingBlobToEndOfStreamScenario()
            {
                // calling code will have marked the existing entry as empty space.
                // so our job is just to save to the end, and update the index.

                fs.Seek(0, SeekOrigin.End);
                offset = (uint)fs.Position;

                // write blob to disk at current position?  We must have a method for that?
                writeBlobEntryAtCurrentPosition(key, blobData);

                updateIndexEntryOffset();
            }

            void updateIndexEntryOffset()
            {
                // assumes offst updated.
                keyToPhysicalOffsetInFile[key] = offset;
            }

            #endregion
        }

        /// <summary>
        /// flushes underlying stream to disk.
        /// </summary>
        internal void Flush()
        {
            this.fs.Flush();
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

        private BlobEntry readBlob(uint offset)
        {
            fs.Seek(offset, SeekOrigin.Begin);

            var typeByte = readByte();
            if (typeByte != BLOB_ENTRY)
                throw new Exception("Offset into file stream doesn't point to a blob entry.");

            var numBytes = readUInt() - HEADER_BYTES_SIZE - LENGTH_VALUE_BYTES_SIZE - KEY_VALUE_BYTES_SIZE;
            if (numBytes < 0)
                throw new Exception("Can't read less than zero byte! Are we actually poining at a blob entry?");

            var key = readUInt();

            var bytes = new byte[numBytes];
            var readBytesCount = fs.Read(bytes, 0, bytes.Length);
            if (readBytesCount != numBytes)
                throw new Exception("Number of bytes read in does not match the expected length, suggesting bad data in the stream.  Sorry.");

            return new BlobEntry(offset, key, bytes);
        }

        private uint readEntryLengthOnly(uint offset)
        {
            fs.Seek(offset, SeekOrigin.Begin);

            var typeByte = readByte();

            // as its a byte, no need to test for <0.  
            // We know there are only 2 types. and those are 0 and 1.  So anything larger than 1...
            if (typeByte > BLOB_ENTRY)
                throw new Exception("Doesn't look like the offset provided points to the beginning of an entry?");

            return readUInt();
        }

        private (byte type, uint length) readEntryHeader(uint offset)
        {
            fs.Seek(offset, SeekOrigin.Begin);

            var typeByte = readByte();

            // as its a byte, no need to test for <0.  
            // We know there are only 2 types. and those are 0 and 1.  So anything larger than 1...
            if (typeByte > BLOB_ENTRY)
                throw new Exception("Doesn't look like the offset provided points to the beginning of an entry?");

            var length = readUInt();

            return (typeByte, length);
        }

        private void updateEntryTypeToEmptyEntry(uint entryOffset)
        {
            fs.Seek(entryOffset, SeekOrigin.Begin);
            writeByte(EMPTY_ENTRY);

            this.Flush();
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

        private void defragmentFreeSpaces(List<FreeSpaceEntry> freeSpaces)
        {
            // index of entry.  NOTE: Delete in reverse order so each index does not affect the next.
            var entriesToDelete = new List<int>();
            var entriesToAmend = new List<(int index, uint newLength)>();
            var orderedFreeSpaceMap = freeSpaceMap(freeSpaces).OrderBy(x => x.StartOffset).ToList();
            var offsetToUnorderedMap = mapOffsetToUnOrderedIndex(orderedFreeSpaceMap, freeSpaces);

            entriesToDelete.Clear();
            entriesToAmend.Clear();

            // ideally this re-iterates or does what ever it needs to so we only have to hit the DB file ONCE.
            var results = findFragmentations();

            if (results != null)
            {
                // needs to happen before deletes, so the index don't change.
                processInMemoryUpdates(results.Value.updates);

                // persist the minimum changes necessary to disk. Also needs to happen before deletes in memory.
                processStreamUpdates(results.Value.updates);

                // must happen after updates do we don't change indexes of update entries.                    
                processInMemoryDeletes(results.Value.deletes);
            }

            #region local helper methods

            List<StorageBlock> freeSpaceMap(List<FreeSpaceEntry> freeSpaces)
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

                return freeSpaces.Select(x => new StorageBlock(x.Offset, x.Offset + x.Length - 1)).ToList();
            }

            Dictionary<uint, int> mapOffsetToUnOrderedIndex(List<StorageBlock> orderedFreeSpaceMap, List<FreeSpaceEntry> freeSpaces)
            {
                var map = new Dictionary<uint, int>();

                for (int orderedIndex = 0; orderedIndex < orderedFreeSpaceMap.Count; orderedIndex++)
                {
                    var orderedItem = orderedFreeSpaceMap[orderedIndex];
                    var unorderedIndex = freeSpaces.FirstIndexOf(x => x.Offset == orderedItem.StartOffset);

                    map.Add(orderedItem.StartOffset, unorderedIndex);
                }

                return map;
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
                    // idiot check
                    if (selectedFreeSpaceIndex < (offsetToUnorderedMap.Count - 1))
                    {
                        var freeSpace = orderedFreeSpaceMap[selectedFreeSpaceIndex];
                        var freeSpaceOriginalListIndex = offsetToUnorderedMap[freeSpace.StartOffset]; //freeSpaces.FirstIndexOf(x => x.Offset == freeSpace.StartOffset);

                        var newStartOffset = freeSpace.StartOffset;
                        var newEndOffset = freeSpace.EndOffset;

                        int compareFreeSpaceIndex = selectedFreeSpaceIndex + 1;
                        var compareFreeSpace = orderedFreeSpaceMap[compareFreeSpaceIndex];

                        var compareFreeSpaceOriginalListIndex = offsetToUnorderedMap[compareFreeSpace.StartOffset];

                        if (freeSpace.EndOffset + 1 == compareFreeSpace.StartOffset)
                        {
                            // defrag needed.  We can count this as one merge, but there may be others.
                            // We than therefore iterate over the next one(s) to see if there are more.
                            // in reality the most we are likely to ever see is 3 in a row. (space)(entry just deleted)(space)

                            newEndOffset = compareFreeSpace.EndOffset;

                            deletes.Add(compareFreeSpaceOriginalListIndex);

                            var tryNext = false;
                            var skipEntriesCount = 1;
                            var nextIndex = compareFreeSpaceIndex + 1;
                            do
                            {
                                tryNext = false;
                                StorageBlock? nextFreeSpace = orderedFreeSpaceMap.Count > nextIndex ? orderedFreeSpaceMap[nextIndex] : null;

                                if (nextFreeSpace != null)
                                {
                                    if (compareFreeSpace.EndOffset + 1 == nextFreeSpace.StartOffset)
                                    {
                                        compareFreeSpaceIndex++;
                                        compareFreeSpace = orderedFreeSpaceMap[compareFreeSpaceIndex];

                                        var nextFreeSpaceOriginalListIndex = offsetToUnorderedMap[nextFreeSpace.StartOffset];

                                        deletes.Add(nextFreeSpaceOriginalListIndex);

                                        nextIndex++;
                                        skipEntriesCount++;

                                        newEndOffset = compareFreeSpace.EndOffset;

                                        tryNext = true;
                                    }
                                }
                            }
                            while (tryNext);

                            var calcLength = (newEndOffset - newStartOffset) + 1;

                            updates.Add((freeSpaceOriginalListIndex, calcLength));

                            selectedFreeSpaceIndex += skipEntriesCount;
                        }
                    }
                    selectedFreeSpaceIndex++;
                }
                while (selectedFreeSpaceIndex < orderedFreeSpaceMap.Count);


                return (deletes, updates);
            }

            void processInMemoryDeletes(IEnumerable<int> indexesToDelete)
            {
                foreach (var idx in indexesToDelete.OrderByDescending(x => x))
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

        private FreeSpaceEntry? findAvailableSpaceInFile(List<FreeSpaceEntry> freeSpacesInFile, uint rawLength)
        {
            var entryPlusEmptyLength = rawLength + FULL_EMPTY_ENTRY_BYTES_SIZE;

            return freeSpacesInFile.Where(e => e.Length == rawLength || e.Length >= entryPlusEmptyLength)
                                   .Lowest(e => e.Length);
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

        private void writeByte(byte value)
        {
            fs.WriteByte(value);
        }

        private void writeUInt(uint value)
        {
            sharedMem.UInt = value;

            fs.Write(new byte[] { sharedMem.Byte0, sharedMem.Byte1, sharedMem.Byte2, sharedMem.Byte3, }, 0, 4);
        }

        private object nextKeyLock = new object();
        private uint getNextKey()
        {
            lock (nextKeyLock)
            {
                return nextKey++;
            }
        }

        private uint writeBlobEntryToDiskAtCurrentPositionAndIndexDictionary(
            Dictionary<uint, uint> keyToPhysicalOffsetInFile,
            byte[] blob)
        {
            var key = getNextKey();
            writeBlobEntryToDiskAtCurrentPositionAndIndexDictionary(keyToPhysicalOffsetInFile, key, blob);

            // for use to keep hold of.
            return key;
        }

        private void writeBlobEntryToDiskAtCurrentPositionAndIndexDictionary(
            Dictionary<uint, uint> keyToPhysicalOffsetInFile,
            uint key,
            byte[] blob)
        {
            // remember this Position for the 'index' dictionary.
            var offset = (uint)fs.Position;

            // write blob entry (header)
            //------------------
            writeByte(BLOB_ENTRY);

            // save to disk
            insertBlobDataToDiskAtCurrentPositionWithExistingKey(key, blob);

            // add to dictionary
            keyToPhysicalOffsetInFile.Add(key, offset);
        }

        /// <summary>
        /// does not include the header.  Length, Key, and Data only and in that order.
        /// Returns the Key generated when saving.
        /// </summary>
        private void insertBlobDataToDiskAtCurrentPositionWithExistingKey(uint key, byte[] blob)
        {
            // length:
            writeUInt((uint)blob.Length + HEADER_BYTES_SIZE + LENGTH_VALUE_BYTES_SIZE + KEY_VALUE_BYTES_SIZE);

            // key:
            writeUInt(key);

            // blob data:
            fs.Write(blob, 0, blob.Length);

            this.Flush();
        }

        private void writeBlobEntryAtCurrentPosition(uint key, byte[] blobData)
        {
            writeByte(BLOB_ENTRY);
            
            var rawLength = (uint)blobData.Length + MINIMUM_BLOB_ENTRY_SIZE;
            writeUInt(rawLength);

            writeUInt(key);

            fs.Write(blobData, 0, blobData.Length);

            this.Flush();
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

            this.Flush();
        }

        #endregion
    }
}
