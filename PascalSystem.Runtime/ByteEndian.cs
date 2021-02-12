namespace PascalSystem.Runtime
{
    internal static class ByteEndian
    {
        public static unsafe ushort GetWordLittle(byte* data) => (ushort)(data[0] | data[1] << 8);
        public static unsafe ushort GetWordBig(byte* data) => (ushort)(data[1] | data[0] << 8);

        public static unsafe void PutWordLittle(byte* data, ushort value)
        {
            data[0] = (byte)value;
            data[1] = (byte)(value << 8);
        }

        public static unsafe void PutWordBig(byte* data, ushort value)
        {
            data[1] = (byte)value;
            data[0] = (byte)(value << 8);
        }
    }
}