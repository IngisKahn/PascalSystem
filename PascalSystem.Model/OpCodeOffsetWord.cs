namespace PascalSystem.Model
{
    public partial class OpCode
    {
        public abstract class OffsetWord : OpCode
        {
            protected OffsetWord(OpcodeValue code, int offset)
                : base(code) => this.Offset = offset;

            public int Offset { get; }

            public override int Length => this.Offset > 0x7F ? 3 : 2;

            public override int GetHashCode() => base.GetHashCode() ^ this.Offset << 8;
        }
    }
}