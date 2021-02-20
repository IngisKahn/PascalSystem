namespace PascalSystem.Model
{
    public partial class OpCode
    {
        public class Exit : OpCode
        {
            public Exit(OpCodeValue code, bool isNormal = true)
                : base(code) => this.IsNormal = isNormal;

            public bool IsNormal { get; }
        }
    }
}