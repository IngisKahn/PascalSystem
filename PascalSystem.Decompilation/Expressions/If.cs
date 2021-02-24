namespace PascalSystem.Decompilation.Expressions
{
    using System.CodeDom.Compiler;
    using System.Text;
    using System.Threading.Tasks;

    public class If : Statement
    {
        public If(BasicBlock trueBlock, BasicBlock falseBlock, Expression expression, bool hasElse)
        {
            this.TrueBlock = trueBlock;
            this.FalseBlock = falseBlock;
            this.Expression = expression;
            this.HasElse = hasElse;
        }

        public BasicBlock TrueBlock { get; }
        public BasicBlock FalseBlock { get; }
        public Expression Expression { get; }
        public bool HasElse { get; }

        internal override void BuildString(StringBuilder builder)
        {
            builder.Append("IF ");
            this.Expression.BuildString(builder);
            builder.Append($" THEN {this.TrueBlock} ELSE {this.FalseBlock}");
        }

        internal override async Task Dump(IndentedTextWriter writer)
        {
            await writer.WriteAsync("IF ");
            await this.Expression.Dump(writer);
            await writer.WriteLineAsync(" THEN");
            await WriteChildren("IF", this.TrueBlock.Statements, writer);
            if (this.HasElse)
            {
                await writer.WriteLineAsync("ELSE");
                await WriteChildren("ELSE", this.FalseBlock.Statements, writer);
            }
            else
                foreach (var statement in this.FalseBlock.Statements)
                    await statement.Dump(writer);
        }
    }

    public partial class Expression
    {
        public static If If(BasicBlock trueBlock, BasicBlock falseBlock, Expression expression, bool hasElse)
        {
            var b = false;
            expression.Type.MeetWith(new Types.Boolean(), ref b);
            return new(trueBlock, falseBlock, expression, hasElse);
        }
    }
}