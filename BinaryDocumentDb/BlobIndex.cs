﻿using System;
using System.Collections.Generic;
using System.Linq;

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
        private uint nextKey = 1; // 0 is considered null?

        public uint GetNextKey()
        {
            // Perhaps in future we can track deleted keys, and have a stack or queue of ready to use entries,
            // and as a last resort, to give out a new value?
            return nextKey++;
        }

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
                uint key = sharedMem.UInt;

                sharedMem.Byte0 = allEntries[i + 4];
                sharedMem.Byte1 = allEntries[i + 5];
                sharedMem.Byte2 = allEntries[i + 6];
                sharedMem.Byte3 = allEntries[i + 7];
                uint value = sharedMem.UInt;

                Index.Add(key, value);
            }

            // reset the next available key.  
            this.nextKey = (Index.Keys != null && Index.Keys.Count > 0)
                ? Index.Keys.Max() + 1 
                : 1;
        }
    }
}
