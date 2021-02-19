namespace PascalSystem.Decompilation.Expressions
{
    public abstract class Unary : Expression
    {
        protected Unary(Expression operand) => this.Operand = operand;

        public Expression Operand { get; }

        public override Types.Base Type => this.Operand.Type;
    }
}