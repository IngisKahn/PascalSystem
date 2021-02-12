namespace PascalSystem.Model
{
    using System;
    using System.Globalization;

    public struct ByteCount : IFormattable
    {
        private readonly int value;

        private ByteCount(int value) => this.value = value;

        public static explicit operator int(ByteCount offset) => offset.value;

        public static explicit operator ByteCount(int value) => new(value);


        public static implicit operator WordCount(ByteCount offset) => (WordCount)((int)offset + 1 >> 1);

        public static implicit operator ByteCount(WordCount count) => new((int)count << 1);

        public bool Equals(ByteCount other) => this.value == other.value;

        public override bool Equals(object obj) => obj is ByteCount b && this.Equals(b);

        public override int GetHashCode() => this.value;

        public static ByteCount operator +(ByteCount a, ByteCount b) => (ByteCount)((int)a + (int)b);

        public static ByteCount operator -(ByteCount a, ByteCount b) => (ByteCount)((int)a - (int)b);

        public static bool operator <(ByteCount a, ByteCount b) => (int)a < (int)b;

        public static bool operator >(ByteCount a, ByteCount b) => (int)a > (int)b;

        public static bool operator <=(ByteCount a, ByteCount b) => (int)a <= (int)b;

        public static bool operator >=(ByteCount a, ByteCount b) => (int)a >= (int)b;

        public static bool operator ==(ByteCount a, ByteCount b) => (int)a == (int)b;

        public static bool operator !=(ByteCount a, ByteCount b) => !(a == b);

        public override string ToString() => this.value.ToString(CultureInfo.InvariantCulture);

        public string ToString(string format, IFormatProvider formatProvider) => this.value.ToString(format,
            formatProvider);

        public string ToString(IFormatProvider formatProvider) => this.value.ToString(formatProvider);
    }
}