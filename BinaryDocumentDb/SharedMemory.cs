using System.Runtime.InteropServices;

namespace BinaryDocumentDb
{
    /// <summary>
    /// Use memory as a means of converting values.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct SharedMemory
    {
        [FieldOffset(0)] public byte Byte0;
        [FieldOffset(1)] public byte Byte1;
        [FieldOffset(2)] public byte Byte2;
        [FieldOffset(3)] public byte Byte3;

        [FieldOffset(0)] public ushort UShort0;
        [FieldOffset(2)] public ushort UShort1;

        [FieldOffset(0)] public short Short0;
        [FieldOffset(2)] public short Short1;

        [FieldOffset(0)] public int Int;
        [FieldOffset(0)] public uint UInt;
    }
}
