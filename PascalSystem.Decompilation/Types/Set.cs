namespace PascalSystem.Decompilation.Types
{
    using Model;

    public class Set : Structured
    {
        public override BitCount Size { get; }
        public override Base MeetWith(Base other, ref bool hasChanged, bool setToHighestPointer = false) => throw new System.NotImplementedException();

        public override bool IsCompatible(Base other, bool testAllElements) => throw new System.NotImplementedException();

        public override Base Clone() => throw new System.NotImplementedException();

        public override bool Equals(Base other) => throw new System.NotImplementedException();
    }
}