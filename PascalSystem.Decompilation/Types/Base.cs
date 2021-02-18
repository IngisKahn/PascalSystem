namespace PascalSystem.Decompilation.Types
{
    using System;
    using System.Diagnostics;
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
    }

    public class Proxy : Base
    {
        public Base Value { get; private set; }

        public Proxy(Base value) => this.Value = value;
        public override BitCount Size => this.Value.Size;

        public override string? ToString() => this.Value.ToString(); 
        
        public override bool ResolvesTo<T>() => this.Value.ResolvesTo<T>();

        public override T As<T>() => this.Value.As<T>();

        public override bool IsCompatibleWith(Base other, bool testAllElements = false) => this.Value
            .IsCompatibleWith(other, testAllElements);

        public override Base MergeWith(Base other) => this.Value.MergeWith(other);
        public override Base Clone() => this;
        public override bool Equals(Base other) => this.Value.Equals(other); 
        public override Base MeetWith(Base other, ref bool hasChanged, bool setToHighestPointer = false)
        {
            var ot = other as Proxy;
            if (ot != null)
                other = ot.Value;
            this.Value = this.Value.MeetWith(other, ref hasChanged, setToHighestPointer);
            if (ot != null)
                ot.Value = this.Value;
            return this;
        }

        public override bool IsCompatible(Base other, bool testAllElements) => this.Value.IsCompatible(other,
            testAllElements);

        public override Base MeetAt(ByteCount offset, Base other) => this.Value.MeetAt(offset, other);
    }
}
