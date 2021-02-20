namespace PascalSystem.Model
{
    public partial class OpCode
    {
        public class IndexPacked : OpCode
        {
            public IndexPacked(int count, int stride)
                : base(OpCodeValue.IXP)
            {
                this.Count = count;
                this.Stride = stride;
            }

            public int Count { get; }
            public int Stride { get; }

            public override int Length => 3;

            public override int GetHashCode() => base.GetHashCode() ^ this.Count << 8 ^ this.Stride << 16;

            public override string ToString() => $"{base.ToString()} PerWord:{this.Count} Bits:{this.Stride}";
        }
    }
}