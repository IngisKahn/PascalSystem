namespace PascalSystem.Model
{
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.Linq;

    public partial class OpCode
    {
        public class JumpTable : OpCode
        {
            private readonly bool isPadded;

            public JumpTable(int minimum, int[] addresses, int defaultAddress, bool isPadded)
                : base((int)OpcodeValue.XJP)
            {
                this.isPadded = isPadded;
                this.Minimum = minimum;
                this.Addresses = addresses;
                this.DefaultAddress = defaultAddress;
            }

            public int Minimum { get; }
            public int DefaultAddress { get; }
            public int[] Addresses { get; }

            public override int Length => 7 + this.Addresses.Length * 2 + (this.isPadded ? 1 : 0);

            public override void Dump(IndentedTextWriter writer)
            {
                List<(int Address, int Index)> cases = new();
                var index = this.Minimum;
                foreach (var address in this.Addresses)
                {
                    if (address != this.DefaultAddress)
                        cases.Add((address, index));
                    index++;
                }

                var caseGroups = from c in cases
                    group c by c.Address
                    into addressGroup
                    select new {Address = addressGroup.Key, Indexes = addressGroup};

                writer.WriteLine();
                foreach (var c in caseGroups)
                {
                    foreach (var i in c.Indexes)
                        writer.WriteLine("case {0}:", i.Index);
                    writer.WriteLine(" 0x{0:X4}", c.Address);
                }

                writer.WriteLine("default:");
                writer.WriteLine(" 0x{0:X4}", this.DefaultAddress);
            }

            public override int GetHashCode() => base.GetHashCode() ^ this.DefaultAddress << 8 ^
                                                 this.Addresses.Length << 24;

            public override string ToString() =>
                $"{base.ToString()} {this.Minimum}-{this.Minimum + this.Addresses.Length} X-0x{this.DefaultAddress:X}";
        }
    }
}