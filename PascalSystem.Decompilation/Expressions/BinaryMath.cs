namespace PascalSystem.Decompilation.Expressions
{
    using System.Text;
    using Model;

    public class BinaryMath : Binary
    {
        public BinaryMath(OpCodeValue ocv, Expression left, Expression right) : base(left, right) => this
            .Operation = ocv;

        public OpCodeValue Operation { get; }

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
                case OpCodeValue.ADI:
                case OpCodeValue.ADR:
                    builder.Append(" + ");
                    break;
                case OpCodeValue.SBI:
                case OpCodeValue.SBR:
                    builder.Append(" - ");
                    break;
                case OpCodeValue.MPI:
                case OpCodeValue.MPR:
                    builder.Append(" * ");
                    break;
                case OpCodeValue.DVI:
                case OpCodeValue.DVR:
                    builder.Append(" / ");
                    break;
                case OpCodeValue.MODI:
                    builder.Append(" % ");
                    break;
                case OpCodeValue.LAND:
                    builder.Append(" & ");
                    break;
                case OpCodeValue.LOR:
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
        public static BinaryMath BinaryIMath(OpCodeValue ocv, Expression left, Expression right)
        {
            var b = false;
            if (!left.Type.ResolvesTo<Types.Pointer>())
                left.Type.MeetWith(new Types.Integer(), ref b);
            if (!right.Type.ResolvesTo<Types.Pointer>())
                right.Type.MeetWith(new Types.Integer(), ref b);
            return new(ocv, left, right);
        }
        public static BinaryMath BinaryRMath(OpCodeValue ocv, Expression left, Expression right)
        {
            var b = false;
            left.Type.MeetWith(new Types.Real(), ref b);
            right.Type.MeetWith(new Types.Real(), ref b);
            return new(ocv, left, right);
        }
    }
}