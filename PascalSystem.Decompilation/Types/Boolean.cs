namespace PascalSystem.Decompilation.Types
{
    using Model;

    public class Boolean : Scalar
    {
        public override BitCount Size => (BitCount)1;
        public override Base MeetWith(Base other, ref bool hasChanged, bool setToHighestPointer = false)
        {
            if (other.ResolvesTo<Void>() || other.ResolvesTo<Boolean>())
                return this;

            if (!other.ResolvesTo<SizeRange>() || other.As<SizeRange>().Minimum > (BitCount)1)
                throw new DecompilationException("Could not meet");

            return this;
        }

        public override bool IsCompatible(Base other, bool testAllElements) => other.ResolvesTo<Void>()
                                                                               || other.ResolvesTo<Boolean>()
                                                                               || other.ResolvesTo<Integer>()
                                                                               || other.ResolvesTo<Byte>()
                                                                               || other.ResolvesTo<Character>()
                                                                               || other.ResolvesTo<SizeRange>() && other.As<SizeRange>().IsCompatibleWithSize((BitCount)1);

        public override Base Clone() => this;

        public override bool Equals(Base other) => other is Boolean;

        public override string ToString() => "BOOLEAN";
    }
}