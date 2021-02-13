namespace PascalSystem.Model
{
    public partial class OpCode
    {
        public class IntermediateWord : OffsetWord
        {
            public IntermediateWord(int code, int level, int offset) : base(code, offset) => this.Level = level;
            public int Level { get; }

            public override int Length => base.Length + 1;

            public override string ToString() => base.ToString() + $" Parent{this.Level:X}_Local{this.Offset:X}";

            public override int GetHashCode() => base.GetHashCode() ^ this.Level << 24;
        }
    }
}