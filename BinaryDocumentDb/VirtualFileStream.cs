using System.IO;

namespace BinaryDocumentDb
{
    /// <summary>
    /// adds the <see cref="IVirtualFileStream"/> to the inherited <see cref="FileStream"/>.
    /// This makes faking it a lot easier, and therefore we can do unit tests against file-streams.
    /// </summary>
    internal class VirtualFileStream : FileStream, IVirtualFileStream
    {
        /// <summary>
        /// Provide the name of the file in the current path, or provide a full or relative path ending in the file name
        /// of the database file to read or create.
        /// </summary>
        /// <param name="fileName"></param>
        internal VirtualFileStream(string fileName) : base(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite)
        {
        }
    }
}
