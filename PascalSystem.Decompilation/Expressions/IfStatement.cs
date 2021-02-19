namespace PascalSystem.Decompilation.Expressions
{
    using System.Text;

    public class IfStatement : Statement
    {
        public IfStatement(BasicBlock trueBlock, BasicBlock falseBlock, Expression expression)
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
}