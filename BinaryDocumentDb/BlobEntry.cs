using System.Collections.Generic;

namespace BinaryDocumentDb
{
    /// <summary>
    /// Represents the physical data stored on disk, within the context of the file stream itself.
    /// So you provide the <see cref="Offset"/> (from the beginning of the stream) of where the Blob can be found.
    /// You provide the <see cref="Key"/> by which the blob's <see cref="Offset"/> can be found within an in-memory index,
    /// and lastly, the <see cref="Data"/> (BlobData) stored for later retrieval.
    /// <para>This class is immutable by design.</para>
    /// </summary>
    internal class BlobEntry
    {
        public BlobEntry(uint offset, uint key, IReadOnlyList<byte> data)
        {
            this.Offset = offset;
            this.Key = key;
            this.Data = data;
        }

        /// <summary>
        /// Byte offset (from the beginning) into the underlying file stream.
        /// </summary>
        internal readonly uint Offset;

        /// <summary>
        /// identifier for this data blob.
        /// </summary>
        internal readonly uint Key;

        /// <summary>
        /// Actual data stored, NOT INCLUDING the header byte, length, or other meta data, just the data originally asked to be stored.
        /// </summary>
        internal readonly IReadOnlyList<byte> Data;
    }
}
