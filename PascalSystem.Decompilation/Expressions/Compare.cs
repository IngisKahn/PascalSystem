namespace PascalSystem.Decompilation.Expressions
{
    using System.Text;

    public class Compare : Binary
    {
        internal Compare(int operation, int type, Expression left, Expression right)
            : base(left, right)
        {
            this.Operation = operation;
            Types.Base myType;
            switch (type)
            {
                case 0:
                    myType = new Types.Integer();
                    break;
                case 2:
                    myType = new Types.Real();
                    break;
                case 4:
                    myType = new Types.String();
                    break;
                case 6:
                    myType = new Types.Boolean();
                    break;
                //case 8:
                //    myType = new Set();
                //    break;
                case 10:
                    myType = new Types.Byte();
                    break;
                case 12:
                    myType = new Types.Array();
                    break;
                default:
                    throw new DecompilationException();
            }
            var b = false;
            left.Type.MeetWith(myType, ref b);
            right.Type.MeetWith(myType, ref b);
        }

        public int Operation { get; }
        public Types.Base ResultType => new Types.Boolean();

        internal override void BuildString(StringBuilder builder)
        {
            this.Left.BuildString(builder);
            switch (this.Operation)
            {
                case 0:
                    builder.Append(" = ");
                    break;
                case 1:
                    builder.Append(" >= ");
                    break;
                case 2:
                    builder.Append(" > ");
                    break;
                case 5:
                    builder.Append(" <= ");
                    break;
                case 6:
                    builder.Append(" < ");
                    break;
                case 8:
                    builder.Append(" <> ");
                    break;
                default:
                    throw new DecompilationException();
            }
            this.Right.BuildString(builder);
        }
    }
}