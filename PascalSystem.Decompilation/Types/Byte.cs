namespace PascalSystem.Decompilation.Types
{
    using Model;

    public class Byte : Scalar
    {
        public override BitCount Size => (BitCount)8;
        public override Base MeetWith(Base other, ref bool hasChanged, bool setToHighestPointer = false)
        {
            if (other.ResolvesTo<Void>() || other.ResolvesTo<Byte>())
                return this;

            return !other.ResolvesTo<SizeRange>() || !other.As<SizeRange>().IsCompatibleWithSize((BitCount)8)
                ? throw new DecompilationException("Could not meet")
                : this;
        }

        public override bool IsCompatible(Base other, bool testAllElements) =>
            other.ResolvesTo<Void>()
            || other.ResolvesTo<Boolean>()
            || other.ResolvesTo<Integer>()
            || other.ResolvesTo<Character>()
            || other.ResolvesTo<SizeRange>() && other.As<SizeRange>().IsCompatibleWithSize((BitCount)8);

        public override Base Clone() => this;

        public override bool Equals(Base other) => other is Boolean;

        public override string ToString() => "BYTE";
    }
}