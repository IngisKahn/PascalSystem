namespace PascalSystem.Decompilation.Types
{
    using Model;

    public class Void : Base
    {
        private Void() { }
        public static Void Instance { get; } = new();
        public override BitCount Size => (BitCount)0;
        public override Base MeetWith(Base other, ref bool hasChanged, bool setToHighestPointer = false)
        {
            hasChanged |= !other.ResolvesTo<Void>();
            return other.Clone();
        }

        public override bool IsCompatible(Base other, bool testAllElements) => true;

        public override Base Clone() => this;

        public override bool Equals(Base other) => other is Void;

        public override string ToString() => "VOID";
    }
}