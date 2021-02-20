namespace PascalSystem.Decompilation.Expressions
{
    using System.Text;
    using Model;

    public class UnaryMath : Unary
    {
        public UnaryMath(OpCodeValue ocv, Expression operand) : base(operand) => this
            .Operation = ocv;

        public OpCodeValue Operation { get; }

        internal override void BuildString(StringBuilder builder)
        {
            var isFunction = false;
            switch (this.Operation)
            {
                case OpCodeValue.LNOT:
                    builder.Append('~');
                    break;
                case OpCodeValue.NGI:
                case OpCodeValue.NGR:
                    builder.Append('-');
                    break;
                case OpCodeValue.ABI:
                case OpCodeValue.ABR:
                    builder.Append("Abs(");
                    isFunction = true;
                    break;
                case OpCodeValue.SQI:
                case OpCodeValue.SQR:
                    builder.Append("Sqr(");
                    isFunction = true;
                    break;
                default:
                    throw new DecompilationException();
            }
            var isBin = this.Operand is BinaryMath;
            if (isBin)
                builder.Append('(');
            this.Operand.BuildString(builder);
            if (isBin || isFunction)
                builder.Append(')');
        }
    }

    public partial class Expression
    {
        public static UnaryMath UnaryMath(OpCodeValue opCodeValue, Expression child) => new(opCodeValue, child);
    }
}