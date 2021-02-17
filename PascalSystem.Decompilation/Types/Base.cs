namespace PascalSystem.Decompilation.Types
{
    using Model;

    public abstract class Base
    {

    }

    public class Pointer : Base { }

    public class SizeRange : Base
    {
        public BitCount Minimum { get; }
        public BitCount Maximum { get; }

        public SizeRange(BitCount minimum, BitCount maximum)
        {
            this.Minimum = minimum;
            this.Maximum = maximum;
        }
    }

    public class Size : SizeRange
    {
        public Size(BitCount bits) : base(bits, bits) { }
    }

    public class Void : Base
    {
        private Void() { }
        public static Void Instance { get; } = new();
    }

    public class Scalar : Base { }
    public class Boolean : Scalar { }
    public class Character : Scalar { }
    public class Byte : Scalar { }
    public class Integer : Scalar { }
    public class Real : Scalar { }

    public class Structured : Base { }
    public class Array : Structured { }
    public class Record : Structured { }
    public class Set : Structured { }

    public class Proxy : Base { }

    public class Interval { }
}
