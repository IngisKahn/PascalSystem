namespace PascalSystem.Model
{
    public partial class OpCode
    {
        public class ConstantString : Constant
        {
            public ConstantString(string value) : base(OpcodeValue.LSA) => this.Value = value;
            public string Value { get; }

            public override int Length => 2 + this.Value.Length;

            public override string ToString() => base.ToString() + " \"" + this.Value + '"';

            public override int GetHashCode() => base.GetHashCode() ^ this.Value.GetHashCode();
        }
    }
}