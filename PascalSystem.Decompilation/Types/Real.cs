namespace PascalSystem.Decompilation.Types
{
    using Model;

    public class Real : Scalar 
    {
        public override BitCount Size => (BitCount)32;
        public override Base MeetWith(Base other, ref bool hasChanged, bool setToHighestPointer = false)
        {
            if (other.ResolvesTo<Void>() || other.ResolvesTo<Real>())
                return this;

            return !other.ResolvesTo<SizeRange>() || !other.As<SizeRange>().IsCompatibleWithSize((BitCount)32)
                ? throw new DecompilationException("Could not meet")
                : this;
        }

        public override bool IsCompatible(Base other, bool testAllElements) =>
            other.ResolvesTo<Void>()
            || other.ResolvesTo<SizeRange>() && other.As<SizeRange>().IsCompatibleWithSize((BitCount)32);

        public override Base Clone() => this;

        public override bool Equals(Base other) => other is Boolean;

        public override string ToString() => "INTEGER";
    }
}