namespace PascalSystem.Model
{
    public partial class OpCode
    {
        public class ExternalWord : OffsetWord
        {
            public ExternalWord(int code, string segment, int segNum, int offset)
                : base(code, offset)
            {
                this.Segment = segment;
                this.SegNumber = segNum;
            }

            public string Segment { get; }
            public int SegNumber { get; }

            public override int Length => base.Length + 1;

            public override string ToString() => base.ToString() +
                                                 $" {this.Segment}.Global{this.Offset:X}";

            public override int GetHashCode() => base.GetHashCode() ^ this.Segment.GetHashCode();
        }
    }
}