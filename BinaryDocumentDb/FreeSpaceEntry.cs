namespace BinaryDocumentDb
{
    public class FreeSpaceEntry
    {
        public FreeSpaceEntry(uint offset, uint length)
        {
            Offset = offset;
            Length = length;
        }

        public readonly uint Offset;
        public readonly uint Length;
    }
}
