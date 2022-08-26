namespace BinaryDocumentDb
{
    public class FreeSpaceIndexEntry
    {
        public FreeSpaceIndexEntry(uint offset, uint length)
        {
            Offset = offset;
            Length = length;
        }

        public readonly uint Offset;
        public readonly uint Length;
    }
}
