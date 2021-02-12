namespace PascalSystem.Model
{
    using System;
    using System.Globalization;

    public struct BitCount : IFormattable, IComparable<BitCount>
    {
        public bool Equals(BitCount other) => this.value == other.value;

        public override bool Equals(object obj) => obj is BitCount b && this.Equals(b);

        public override int GetHashCode() => this.value;

        private readonly int value;

        private BitCount(int value) => this.value = value;

        public static explicit operator int(BitCount count) => count.value;

        public static explicit operator BitCount(int value) => new(value);

        public static BitCount operator +(BitCount a, BitCount b) => (BitCount)((int)a + (int)b);

        public static BitCount operator -(BitCount a, BitCount b) => (BitCount)((int)a - (int)b);

        public static bool operator <(BitCount a, BitCount b) => (int)a < (int)b;

        public static bool operator >(BitCount a, BitCount b) => (int)a > (int)b;

        public static bool operator <=(BitCount a, BitCount b) => (int)a <= (int)b;

        public static bool operator >=(BitCount a, BitCount b) => (int)a >= (int)b;

        public static bool operator ==(BitCount a, BitCount b) => (int)a == (int)b;

        public static bool operator !=(BitCount a, BitCount b) => !(a == b);

        public static implicit operator ByteCount(BitCount count) => (ByteCount)((int)count + 7 >> 3);

        public static implicit operator BitCount(ByteCount offset) => new BitCount((int)offset << 3);

        public static implicit operator WordCount(BitCount count) => (WordCount)((int)count + 0xF >> 4);

        public static implicit operator BitCount(WordCount count) => new BitCount((int)count << 4);

        public int CompareTo(BitCount other) => this.value.CompareTo(other.value);

        public override string ToString() => this.value.ToString(CultureInfo.InvariantCulture);

        public string ToString(IFormatProvider formatProvider) => this.value.ToString(formatProvider);

        public string ToString(string format, IFormatProvider formatProvider) => this.value.ToString(format,
            formatProvider);
    }
}
