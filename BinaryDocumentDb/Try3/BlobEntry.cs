using System.Collections.Generic;

namespace BinaryDocumentDb
{
    internal class BlobEntry
    {
        public BlobEntry(uint offset, uint key, IReadOnlyList<byte> data)
        {
            this.offset = offset;
            this.key = key;
            this.data = data;
        }

        internal readonly uint offset;
        internal readonly uint key;

        internal readonly IReadOnlyList<byte> data;
    }
}
