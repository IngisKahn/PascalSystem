namespace PascalSystem.Model
{
    public partial class OpCode
    {
        public class CountBig : CountByte
        {
            public CountBig(OpCodeValue code, int count) : base(code, count) { }

            public override int Length => base.Length + (this.Count > 127 ? 1 : 0);
        }
    }
}