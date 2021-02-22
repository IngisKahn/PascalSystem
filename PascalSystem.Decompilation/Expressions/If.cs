namespace PascalSystem.Decompilation.Expressions
{
    using System.Text;

    public class If : Statement
    {
        public If(BasicBlock trueBlock, BasicBlock falseBlock, Expression expression)
        {
            this.TrueBlock = trueBlock;
            this.FalseBlock = falseBlock;
            this.Expression = expression;
        }

        public BasicBlock TrueBlock { get; }
        public BasicBlock FalseBlock { get; }
        public Expression Expression { get; }

        internal override void BuildString(StringBuilder builder)
        {
            builder.Append("IF ");
            this.Expression.BuildString(builder);
            builder.Append($" THEN {this.TrueBlock} ELSE {this.FalseBlock}");
        }
    }

    public partial class Expression
    {
        public static If If(BasicBlock trueBlock, BasicBlock falseBlock, Expression expression)
        {
            var b = false;
            expression.Type.MeetWith(new Types.Boolean(), ref b);
            return new(trueBlock, falseBlock, expression);
        }
    }
}