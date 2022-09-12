using System.Collections.Generic;

namespace BinaryDocumentDb
{
    internal class BlobEntry
    {
        public BlobEntry(uint offset, uint key, IReadOnlyList<byte> data)
        {
            this.Offset = offset;
            this.Key = key;
            this.Data = data;
        }

        internal readonly uint Offset;
        internal readonly uint Key;

        internal readonly IReadOnlyList<byte> Data;
    }
}
