namespace PascalSystem.Model
{
    public partial class OpCode
    {
        public abstract class Constant : OpCode
        {
            protected Constant(OpCodeValue code) : base(code) { }
        }
    }
}