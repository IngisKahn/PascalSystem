namespace PascalSystem.Model
{
    public partial class OpCode
    {
        public class Jump : OpCode
        {
            public Jump(int code, bool isInTable, int address, bool isConditional)
                : base(code)
            {
                this.Address = address;
                this.IsInTable = isInTable;
                this.IsConditional = isConditional;
            }

            public bool IsInTable { get; }
            public int Address { get; }
            public bool IsConditional { get; }

            public override int Length => 2;

            public override int GetHashCode() => base.GetHashCode() ^ this.Address << 8;

            public override string ToString() =>
                $"{base.ToString()} 0x{this.Address:X}{(this.IsInTable ? "[JTAB]" : "")}";
        }
    }
}