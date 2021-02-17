namespace PascalSystem.Model
{
    public partial class OpCode
    {
        public class GlobalWord : OffsetWord
        {
            public GlobalWord(OpcodeValue code, int offset) : base(code, offset) { }

            public override string ToString() => base.ToString() + " Global" + this.Offset.ToString("X");
        }
    }
}