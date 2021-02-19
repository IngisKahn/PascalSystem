namespace PascalSystem.Decompilation.Types
{
    using System;
    using System.Diagnostics;
    using System.Text;
    using Model;

    public abstract class Base
    {
        public Proxy Proxy() => this as Proxy ?? new(this);

        public abstract BitCount Size { get; }
        public virtual Base MeetAt(ByteCount offset, Base other) => throw new DecompilationException($"Can't split this type at 0x{offset:X} to include {other}");
        public abstract Base MeetWith(Base other, ref bool hasChanged, bool setToHighestPointer = false);
        public virtual bool ResolvesTo<T>() where T : Base => this is T;
        public virtual bool IsCompatibleWith(Base other, bool testAllElements = false)
        {
            if (other.ResolvesTo<Record>() ||
                other.ResolvesTo<Array>())
                return other.IsCompatible(this, testAllElements);
            return this.IsCompatible(other, testAllElements);
        }
        public abstract bool IsCompatible(Base other, bool testAllElements);

        public virtual T As<T>() where T : Base
        {
            var result = this as T;
            Debug.Assert(result != null);
            return result;
        }
        public abstract Base Clone();


        public bool IsSubTypeOrEqual(Base other)
        {
            if (this.ResolvesTo<Void>() || this.Equals(other))
                return true;
            if (this.ResolvesTo<Record>() && other.ResolvesTo<Record>())
                return this.As<Record>().IsSubRecordOf(other);
            return false;
        }
        public abstract bool Equals(Base other);
        public virtual Base MergeWith(Base other) => throw new DecompilationException("Cannot merge with this type");
        public Base Dereference() => this.ResolvesTo<Pointer>() ? this.As<Pointer>().PointsTo : Void.Instance;
        public virtual void Display(object value, StringBuilder builder) => builder.Append(value);
    }
}
