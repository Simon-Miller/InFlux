using System;
using System.Collections.Generic;
using System.Text;

namespace BinaryDocumentDb
{
    internal class BlobIndex
    {
        /// <summary>
        /// key: a unique value we generate and give to you.  Entries are not 'named'.
        /// value: an offset (base zero) into the database file where this blob is stored.
        /// </summary>
        public readonly Dictionary<uint, uint> Index = new Dictionary<uint, uint>();


        // GetNextKey?  I can see why guids are nice at this point, but too much space if you ask me!

        // serialize?
        public IReadOnlyList<byte> Serialize()
        {
            var data = new List<byte>();

            var sharedMem = new SharedMemory();

            foreach (var key in Index.Keys)
            {
                sharedMem.UInt = key;

                data.Add(sharedMem.Byte0);
                data.Add(sharedMem.Byte1);
                data.Add(sharedMem.Byte2);
                data.Add(sharedMem.Byte3);

                sharedMem.UInt = Index[key];

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
            Index.Clear();

            var sharedMem = new SharedMemory();

            // assumed divisible by 8.
            for (int i = 0; i < allEntries.Length; i += 8)
            {
                sharedMem.Byte0 = allEntries[i];
                sharedMem.Byte1 = allEntries[i + 1];
                sharedMem.Byte2 = allEntries[i + 2];
                sharedMem.Byte3 = allEntries[i + 3];

                uint key = 0; key = sharedMem.UInt; // want to ensure we don't just create a variable pointer to the same UInt!

                sharedMem.Byte0 = allEntries[i + 4];
                sharedMem.Byte1 = allEntries[i + 5];
                sharedMem.Byte2 = allEntries[i + 6];
                sharedMem.Byte3 = allEntries[i + 7];

                uint value = 0; value = sharedMem.UInt;

                Index.Add(key, value);
            }
        }

    }
}
