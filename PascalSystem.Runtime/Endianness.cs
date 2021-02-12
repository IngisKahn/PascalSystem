namespace PascalSystem.Runtime
{
    internal enum Endianness
    {
        Big,
        Little
    }

    internal static class EndiannessExtensions
    {
        public static string Name(this Endianness value) => value == Endianness.Little ? "little-endian" : "big-endian";

        public static unsafe ushort GetWord(this Endianness endianness, byte* data) => endianness == Endianness.Little
            ? ByteEndian.GetWordLittle(data)
            : ByteEndian.GetWordBig(data);

        public static unsafe void PutWord(this Endianness endianness, byte* data, ushort value)
        {
            if (endianness == Endianness.Little)
                ByteEndian.PutWordLittle(data, value);
            else
                ByteEndian.PutWordBig(data, value);
        }

        public static Endianness Other(this Endianness value) => value == Endianness.Little
            ? Endianness.Big
            : Endianness.Little;
    }
}
