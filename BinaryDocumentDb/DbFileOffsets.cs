using System;
using System.Collections.Generic;
using System.Text;

namespace BinaryDocumentDb
{
    internal static class DbFileOffsets
    {
        /// <summary>
        /// The area of the file where an index of key (uint) value(uint) pairs are stored, that represent the actual blobs stored in the file.
        /// </summary>
        public static uint BlobsIndexOffset = 8;

        public static uint FreeSpaceIndexOffset = 100;
    }
}
