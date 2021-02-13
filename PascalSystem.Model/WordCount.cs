namespace PascalSystem.Model
{
    using System;
    using System.Globalization;

    public struct WordCount : IFormattable
    {
        private readonly int value;

        private WordCount(int value) => this.value = value;

        public static explicit operator int(WordCount count) => count.value;

        public static explicit operator WordCount(int value) => new(value);

        public bool Equals(WordCount other) => this.value == other.value;

        public override bool Equals(object? obj) => obj is WordCount w && this.Equals(w);

        public override int GetHashCode() => this.value;

        public static WordCount operator +(WordCount a, WordCount b) => (WordCount)((int)a + (int)b);

        public static WordCount operator -(WordCount a, WordCount b) => (WordCount)((int)a - (int)b);

        public static bool operator <(WordCount a, WordCount b) => (int)a < (int)b;

        public static bool operator >(WordCount a, WordCount b) => (int)a > (int)b;

        public static bool operator <=(WordCount a, WordCount b) => (int)a <= (int)b;

        public static bool operator >=(WordCount a, WordCount b) => (int)a >= (int)b;

        public static bool operator ==(WordCount a, WordCount b) => (int)a == (int)b;

        public static bool operator !=(WordCount a, WordCount b) => !(a == b);

        public override string ToString() => this.value.ToString(CultureInfo.InvariantCulture);

        public string ToString(string? format, IFormatProvider? formatProvider) => this.value.ToString(format,
            formatProvider);

        public string ToString(IFormatProvider formatProvider) => this.value.ToString(formatProvider);
    }
}