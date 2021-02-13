namespace PascalSystem.Model
{
    public partial class OpCode
    {
        public class LocalWord : OffsetWord
        {
            public LocalWord(int code, int offset) : base(code, offset) { }

            public override string ToString() => base.ToString() + " Local" + this.Offset.ToString("X");
        }
    }
}