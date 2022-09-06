using System.IO;

namespace BinaryDocumentDb
{
    public interface IVirtualFileStream
    {
        void Close();

        long Length { get; }

        long Position { get; set; }

        int Read(byte[] array, int offset, int count);

        int ReadByte();

        long Seek(long offset, SeekOrigin origin);

        void Write(byte[] array, int offset, int count);

        void WriteByte(byte value);
    }
}
