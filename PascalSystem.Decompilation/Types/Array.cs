namespace PascalSystem.Decompilation.Types
{
    using Model;

    public class Array : Structured
    {
        public const int NoBound = 999999;

        private Base baseType;

        public Array(Base type, int length)
        {
            this.baseType = type;
            this.Length = length;
        }

        public Array(Base type)
        {
            this.Length = Array.NoBound;
            this.baseType = type;
        }

        public Base BaseType
        {
            get => this.baseType;
            private set
            {
                if (this.Length != Array.NoBound)
                {
                    var baseSize = this.baseType.Size;
                    if ((int)baseSize == 0)
                        baseSize = (BitCount)1;
                    baseSize = (BitCount)((int)baseSize * this.Length);
                    var newSize = value.Size;
                    if ((int)newSize == 0)
                        newSize = (BitCount)1;
                    this.Length = (int)baseSize / (int)newSize;
                }

                this.baseType = value;
            }
        }

        public int Length { get; private set; }

        public bool IsUnbounded => this.Length == Array.NoBound;

        public override BitCount Size => (BitCount)((int)this.baseType.Size * this.Length);

        public void FixBaseType(Base type)
        {
            if (this.baseType is Void)
                this.baseType = type;
            else
                this.baseType.As<Array>().FixBaseType(type);
        }

        public override Base Clone() => new Array(this.baseType.Clone(), this.Length);

        public override bool Equals(Base other) =>
            other is Array otherArr && this.baseType.Equals(otherArr.baseType)
                                    && otherArr.Length == this.Length;

        public override string ToString() => "ARRAY[" + (this.IsUnbounded ? string.Empty : this.Length) + "] OF " + this.baseType;


        public override Base MeetWith(Base other, ref bool hasChanged, bool setToHighestPointer = false)
        {
            if (other.ResolvesTo<Void>())
                return this;
            if (other.ResolvesTo<SizeRange>() && other.As<SizeRange>().IsCompatibleWithSize(this.Size))
                return this;
            if (!other.ResolvesTo<Array>())
                return this.baseType.IsCompatible(other, true)
                    ? this
                    : throw new DecompilationException("Could not meet");
            var otherArr = other.As<Array>();
            var newBase = this.baseType.Clone().MeetWith(otherArr.baseType, ref hasChanged, setToHighestPointer);
            if (!newBase.Equals(this.baseType))
            {
                hasChanged = true;
                this.BaseType = newBase;
            }
            if (otherArr.Length < this.Length)
                this.Length = otherArr.Length;
            return this;
        }

        public override bool IsCompatibleWith(Base other, bool testAllElements = false) => this.IsCompatible(other,
            testAllElements);

        public override bool IsCompatible(Base other, bool testAllElements)
        {
            if (other.ResolvesTo<Void>() || other.ResolvesTo<Array>() &&
                this.baseType.IsCompatibleWith(other.As<Array>().baseType))
                return true;
            return !testAllElements && this.baseType.IsCompatibleWith(other);
        }
    }
}