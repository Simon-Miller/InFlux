using System.Collections.Generic;

namespace BinaryDocumentDb
{
    internal class FreeSpace
    {
        /// <summary>
        /// key: a unique value we generate and give to you.  Entries are not 'named'.
        /// value: an offset (base zero) into the database file where this blob is stored.
        /// </summary>
        public readonly List<FreeSpaceEntry> Collection = new List<FreeSpaceEntry>();

        // serialize?
        public IReadOnlyList<byte> Serialize()
        {
            var data = new List<byte>();

            var sharedMem = new SharedMemory();

            foreach (var entry in Collection)
            {
                sharedMem.UInt = entry.Offset;
                data.Add(sharedMem.Byte0);
                data.Add(sharedMem.Byte1);
                data.Add(sharedMem.Byte2);
                data.Add(sharedMem.Byte3);

                sharedMem.UInt = entry.Length;
                data.Add(sharedMem.Byte0);
                data.Add(sharedMem.Byte1);
                data.Add(sharedMem.Byte2);
                data.Add(sharedMem.Byte3);
            }

            return data;
        }

        // deserialize?
        public void Deserialize(byte[] allEntries)
        {
            Collection.Clear();

            var sharedMem = new SharedMemory();

            // assumed divisible by 8.
            for (int i = 0; i < allEntries.Length; i += 8)
            {
                sharedMem.Byte0 = allEntries[i];
                sharedMem.Byte1 = allEntries[i + 1];
                sharedMem.Byte2 = allEntries[i + 2];
                sharedMem.Byte3 = allEntries[i + 3];
                uint offset = sharedMem.UInt;

                sharedMem.Byte0 = allEntries[i + 4];
                sharedMem.Byte1 = allEntries[i + 5];
                sharedMem.Byte2 = allEntries[i + 6];
                sharedMem.Byte3 = allEntries[i + 7];
                uint length = sharedMem.UInt;

                Collection.Add(new FreeSpaceEntry(offset, length));
            }
        }
    }
}
