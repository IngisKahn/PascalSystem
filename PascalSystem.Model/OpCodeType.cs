namespace PascalSystem.Model
{
    public partial class OpCode
    {
        public class Type : OpCode
        {
            public Type(OpcodeValue code, int type) : base(code) => this.TypeCode = type;
            public int TypeCode { get; }

            public string TypeName
            {
                get
                {
                    switch (this.TypeCode)
                    {
                        case 2:
                            return "REAL";
                        case 4:
                            return "STR";
                        case 6:
                            return "BOOL";
                        case 8:
                            return "SET";
                        case 10:
                            return "BYT";
                        case 12:
                            return "WORD";
                        default:
                            return "INVALID";
                    }
                }
            }

            public override int Length => 2;

            public override int GetHashCode() => base.GetHashCode() ^ this.TypeCode << 8;

            public override string ToString() => base.ToString() + this.TypeName;
        }
    }
}