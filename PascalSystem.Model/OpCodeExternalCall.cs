namespace PascalSystem.Model
{
    public partial class OpCode
    {
        public class ExternalCall : OpCode
        {
            public ExternalCall(string segment, int segNum, int proc)
                : base(OpCodeValue.CXP)
            {
                this.Segment = segment;
                this.SegNumber = segNum;
                this.Proc = proc;
            }

            public string Segment { get; }
            public int SegNumber { get; }
            public int Proc { get; }

            public override int Length => base.Length + 2;

            public override string ToString() => base.ToString() + $" {this.Segment}.P{this.Proc}";

            public override int GetHashCode() => base.GetHashCode() ^ this.Segment.GetHashCode();
        }
    }
}