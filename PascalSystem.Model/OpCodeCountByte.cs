namespace PascalSystem.Model
{
    public partial class OpCode
    {
        public class CountByte : OpCode
        {
            public CountByte(OpcodeValue code, int count) : base(code) => this.Count = count;
            public int Count { get; }

            public override int Length => 2;

            public override string ToString() => base.ToString() + " " + this.Count;
        }
    }
}