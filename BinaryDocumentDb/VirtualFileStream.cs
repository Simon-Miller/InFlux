using System.IO;

namespace BinaryDocumentDb
{
    /// <summary>
    /// adds the <see cref="IVirtualFileStream"/> to the inherited <see cref="FileStream"/>.
    /// This makes faking it a lot easier, and therefore we can do unit tests against file-streams.
    /// </summary>
    public class VirtualFileStream : FileStream, IVirtualFileStream
    {
        internal VirtualFileStream(string fileName) : base(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite)
        {
        }
    }
}
