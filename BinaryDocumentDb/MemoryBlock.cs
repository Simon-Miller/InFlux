namespace BinaryDocumentDb
{
        public class MemoryBlock
        {
            public MemoryBlock(uint startOffset, uint endOffset)
            {
                StartOffset = startOffset;
                EndOffset = endOffset;
            }

            internal readonly uint StartOffset;
            internal readonly uint EndOffset;
        }   
}
