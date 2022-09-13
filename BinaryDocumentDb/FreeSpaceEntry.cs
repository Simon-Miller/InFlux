namespace BinaryDocumentDb
{
    internal class FreeSpaceEntry
    {
        /// <summary>
        /// represents an entry on disk. but from the perspective of actual available space to store data,
        /// not including any meta data required.  Therefore the 'length' should be smaller than actual space requirement on disk.
        /// </summary>
        /// <param name="offset">where the first byte of the meta data header can be found. (Entry type)</param>
        /// <param name="length">should be number of bytes we can write as data.  Not including any meta data</param>
        public FreeSpaceEntry(uint offset, uint length)
        {
            Offset = offset;
            Length = length;
        }

        /// <summary>
        /// Offset from the beginning of the database file that we can say is the first byte of available space.  (header byte)
        /// </summary>
        public readonly uint Offset;

        /// <summary>
        /// should be number of bytes we can write into the 'space' within the database file.
        /// </summary>
        public readonly uint Length;
    }
}
