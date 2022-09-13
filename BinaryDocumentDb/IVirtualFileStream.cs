using System.IO;

namespace BinaryDocumentDb
{
    /// <summary>
    /// Exposes a subset of methods to read and write bytes in a <see cref="FileStream"/>
    /// </summary>
    internal interface IVirtualFileStream
    {
        /// <summary>
        /// Closes the current stream and releases any resources (such as sockets and file
        /// handles) associated with the current stream. Instead of calling this method,
        /// ensure that the stream is properly disposed.
        /// </summary>
        void Close();

        /// <summary>
        /// Gets the length in bytes of the stream.
        /// Return a long value representing the length of the stream in bytes.
        /// </summary>
        long Length { get; }

        /// <summary>
        /// Gets or sets the current position of this stream.
        /// </summary>
        long Position { get; set; }

        /// <summary>
        /// Reads a block of bytes from the stream and writes the data in a given buffer.
        /// </summary>
        /// <param name="array">When this method returns, contains the specified byte array with the values between 
        /// offset and (offset + count - 1) replaced by the bytes read from the current source.</param>
        /// <param name="offset">The byte offset in array at which the read bytes will be placed.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <returns>The total number of bytes read into the buffer. This might be less than the number
        /// of bytes requested if that number of bytes are not currently available, or zero
        /// if the end of the stream is reached.</returns>
        int Read(byte[] array, int offset, int count);

        /// <summary>
        /// Reads a byte from the file and advances the read position one byte.
        /// Returns the byte, cast to an System.Int32, or -1 if the end of the stream has been reached.
        /// </summary>
        int ReadByte();

        /// <summary>
        /// Sets the current position of this stream to the given value.
        /// </summary>
        /// <param name="offset">The point relative to origin from which to begin seeking.</param>
        /// <param name="origin">Specifies the beginning, the end, or the current position as a reference point
        /// for offset, using a value of type System.IO.SeekOrigin.</param>
        long Seek(long offset, SeekOrigin origin);

        /// <summary>
        /// Writes a block of bytes to the file stream.
        /// </summary>
        /// <param name="array"> The buffer containing data to write to the stream.</param>
        /// <param name="offset">The zero-based byte offset in array from which to begin copying bytes 
        /// to the stream</param>
        /// <param name="count">The number of bytes to write.</param>
        void Write(byte[] array, int offset, int count);

        /// <summary>
        /// Writes a byte to the current position in the file stream,
        /// and increments the position.
        /// </summary>
        /// <param name="value">A byte to write to the stream.</param>
        void WriteByte(byte value);
    }
}
