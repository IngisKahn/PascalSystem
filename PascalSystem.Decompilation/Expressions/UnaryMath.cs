namespace PascalSystem.Decompilation.Expressions
{
    using System.Text;
    using Model;

    public class UnaryMath : Unary
    {
        public UnaryMath(OpcodeValue ocv, Expression operand) : base(operand) => this
            .Operation = ocv;

        public OpcodeValue Operation { get; }

        internal override void BuildString(StringBuilder builder)
        {
            var isFunction = false;
            switch (this.Operation)
            {
                case OpcodeValue.LNOT:
                    builder.Append('~');
                    break;
                case OpcodeValue.NGI:
                case OpcodeValue.NGR:
                    builder.Append('-');
                    break;
                case OpcodeValue.ABI:
                case OpcodeValue.ABR:
                    builder.Append("Abs(");
                    isFunction = true;
                    break;
                case OpcodeValue.SQI:
                case OpcodeValue.SQR:
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
        public static UnaryMath UnaryMath(OpcodeValue opCodeValue, Expression child) => new(opCodeValue, child);
    }
}