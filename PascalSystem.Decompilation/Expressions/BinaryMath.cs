namespace PascalSystem.Decompilation.Expressions
{
    using System.Text;
    using Model;

    public class BinaryMath : Binary
    {
        public BinaryMath(OpcodeValue ocv, Expression left, Expression right) : base(left, right) => this
            .Operation = ocv;

        public OpcodeValue Operation { get; }

        internal override void BuildString(StringBuilder builder)
        {
            var isBin = this.Left is BinaryMath;
            if (isBin)
                builder.Append('(');
            this.Left.BuildString(builder);
            if (isBin)
                builder.Append(')');
            switch (this.Operation)
            {
                case OpcodeValue.ADI:
                case OpcodeValue.ADR:
                    builder.Append(" + ");
                    break;
                case OpcodeValue.SBI:
                case OpcodeValue.SBR:
                    builder.Append(" - ");
                    break;
                case OpcodeValue.MPI:
                case OpcodeValue.MPR:
                    builder.Append(" * ");
                    break;
                case OpcodeValue.DVI:
                case OpcodeValue.DVR:
                    builder.Append(" / ");
                    break;
                case OpcodeValue.MODI:
                    builder.Append(" % ");
                    break;
                case OpcodeValue.LAND:
                    builder.Append(" & ");
                    break;
                case OpcodeValue.LOR:
                    builder.Append(" | ");
                    break;
                default:
                    throw new DecompilationException();
            }
            isBin = this.Right is BinaryMath;
            if (isBin)
                builder.Append('(');
            this.Right.BuildString(builder);
            if (isBin)
                builder.Append(')');
        }
    }

    public partial class Expression
    {
        public static BinaryMath BinaryMath(OpcodeValue opCodeValue, Expression left, Expression right) =>
            new(opCodeValue, left, right);
    }
}