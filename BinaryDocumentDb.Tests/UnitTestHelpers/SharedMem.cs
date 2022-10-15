using System.Runtime.InteropServices;

namespace BinaryDocumentDb.Tests.UnitTestHelpers
{
    [StructLayout(LayoutKind.Explicit)]
    public struct SharedMem
    {
        [FieldOffset(0)]
        public int IntValue;

        [FieldOffset(0)]
        public uint UIntValue;

        [FieldOffset(0)]
        public byte Byte0;

        [FieldOffset(1)]
        public byte Byte1;

        [FieldOffset(2)]
        public byte Byte2;

        [FieldOffset(3)]
        public byte Byte3;

        public byte[] GetBytes(int value)
        {
            this.IntValue = value;
            return new byte[] { Byte0, Byte1, Byte2, Byte3 };
        }

        public byte[] GetBytes(uint value)
        {
            this.UIntValue = value;
            return new byte[] { Byte0, Byte1, Byte2, Byte3 };
        }
    }
}
