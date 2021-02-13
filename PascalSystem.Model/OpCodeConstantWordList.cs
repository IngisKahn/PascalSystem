namespace PascalSystem.Model
{
    using System.Linq;
    using System.Text;

    public partial class OpCode
    {
        public class ConstantWordList : Constant
        {
            public ConstantWordList(int[] value) : base((int)OpcodeValue.LDC) => this.Value = value;
            public int[] Value { get; }

            public override int Length => 2 + this.Value.Length * 2;

            public override string ToString()
            {
                StringBuilder sb = new();
                sb.Append(base.ToString());
                sb.Append(' ');
                var first = true;
                foreach (var v in this.Value)
                {
                    if (first)
                        first = false;
                    else
                        sb.Append(", ");
                    sb.Append(v);
                }

                return sb.ToString();
            }

            public override int GetHashCode() => this.Value.Aggregate(base.GetHashCode() ^ this.Value.Length << 8, (v, a) => v ^ a);
        }
    }
}