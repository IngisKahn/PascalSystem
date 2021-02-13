namespace PascalSystem.Model
{
    public partial class OpCode
    {
        public class ExitByte : CountByte
        {
            public ExitByte(int count)
                : base((int)OpcodeValue.RNP, count) { }
        }
    }
}