namespace PascalSystem.Model
{
    public partial class OpCode
    {
        public class Exit : OpCode
        {
            public Exit(OpcodeValue code, bool isNormal = true)
                : base(code) => this.IsNormal = isNormal;

            public bool IsNormal { get; }
        }
    }
}