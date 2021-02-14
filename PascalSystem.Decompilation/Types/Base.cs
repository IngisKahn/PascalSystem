namespace PascalSystem.Decompilation.Types
{
    public abstract class Base
    {

    }

    public class Pointer : Base { }
    public class Size : Base { }

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
