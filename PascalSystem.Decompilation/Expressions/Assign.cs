namespace PascalSystem.Decompilation.Expressions
{
    using System.Text;

    public class Assign : Binary
    {
        internal Assign(Expression left, Expression right)
            : base(left, right)
        {
            var b = false;
            left.Type.MeetWith(right.Type, ref b);
        }

        internal override void BuildString(StringBuilder builder)
        {
            this.Left.BuildString(builder);
            builder.Append(" := ");
            this.Right.BuildString(builder);
        }
    }

    public partial class Expression
    {
        public static Assign Assign(Expression target, Expression source) => new(target, source);
    }
}