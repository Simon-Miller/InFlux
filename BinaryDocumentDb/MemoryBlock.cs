namespace BinaryDocumentDb
{
    /// <summary>
    /// represents a range of offsets into storage.  This could represent an empty space in the database,
    /// or a record of some kind.  Its just a range where the EndOffset should be larger than the startOffset.
    /// No validation is done to ensure this.
    /// </summary>
    internal class StorageBlock
    {
        /// <summary>
        /// represents a range of offsets into storage (array or stream)
        /// </summary>
        /// <param name="startOffset">The first bit of data is found here</param>
        /// <param name="endOffset">The last bit of data.  NOT one before or after! this IS the last.</param>
        public StorageBlock(uint startOffset, uint endOffset)
        {
            StartOffset = startOffset;
            EndOffset = endOffset;
        }

        internal readonly uint StartOffset;
        internal readonly uint EndOffset;
    }
}
