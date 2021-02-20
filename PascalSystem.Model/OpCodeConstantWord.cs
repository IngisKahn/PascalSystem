namespace PascalSystem.Model
{
    public partial class OpCode
    {
        public class ConstantWord : Constant
        {
            public ConstantWord(int value) : base(OpCodeValue.LDCI) => this.Value = value;
            public int Value { get; }

            public override int Length => 3;

            public override string ToString() => base.ToString() + " " + this.Value;

            public override int GetHashCode() => base.GetHashCode() ^ this.Value << 8;
        }
    }
}