namespace PascalSystem.Decompilation.Expressions
{
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml.Linq;

    public class If : Statement
    {
        public If(BasicBlock parent, BasicBlock trueBlock, BasicBlock falseBlock, Expression expression)
        {
            this.TrueBlock = trueBlock;
            this.FalseBlock = falseBlock;
            this.Expression = expression;
            //this.HasElse = this.TrueBlock.CommonImmediatePostDominator(this.FalseBlock) != this.FalseBlock;
            //switch (parent.Dominates.Count)
            //{
            //    case 2:
            //        this.NextBlock = falseBlock;
            //        break;
            //    case 3:
            //        this.HasElse = true;
            //        this.NextBlock = parent.Dominates.First(d => d != trueBlock && d != falseBlock);
            //        break;
            //    default:
            //        throw new DecompilationException();
            //}
        }

        public BasicBlock TrueBlock { get; set; }
        public BasicBlock FalseBlock { get; set; }
        public BasicBlock NextBlock { get; }
        public Expression Expression { get; }
        //public bool HasElse { get; }

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
            //await WriteChildren("IF", this.TrueBlock.Statements, writer);
            await this.TrueBlock.Dump(writer);
            //if (this.HasElse)
            //{
            //    await writer.WriteLineAsync("ELSE");
            //    //await WriteChildren("ELSE", this.FalseBlock, writer);
            //    await this.FalseBlock.Dump(writer);
            //}

            await this.NextBlock.Dump(writer, false);
            //else
            //    foreach (var statement in this.FalseBlock)
            //        await statement.Dump(writer);
        }
    }

    public partial class Expression
    {
        public static If If(BasicBlock parent, BasicBlock trueBlock, BasicBlock falseBlock, Expression expression)
        {
            var b = false;
            expression.Type.MeetWith(new Types.Boolean(), ref b);
            return new(parent, trueBlock, falseBlock, expression);
        }
    }

    public class Repeat : Statement
    {
        public Repeat(BasicBlock loop, Expression test, BasicBlock nextBlock)
        {
            this.Loop = loop;
            this.NextBlock = nextBlock;
            this.Test = test;
        }

        public BasicBlock Loop { get; }
        public BasicBlock NextBlock { get; }
        public Expression Test { get; }

        internal override void BuildString(StringBuilder builder)
        {
            builder.AppendLine("REPEAT " + this.Loop);
            builder.Append("UNTIL ");
            this.Test.BuildString(builder);
            builder.AppendLine();
            builder.Append($"{this.NextBlock}");
        }

        internal override async Task Dump(IndentedTextWriter writer)
        {
            await writer.WriteAsync("REPEAT");
            await this.Loop.Dump(writer);
            await writer.WriteAsync("UNTIL ");
            await this.Test.Dump(writer);
            await writer.WriteLineAsync(";");

            await this.NextBlock.Dump(writer, false);
        }
    }
}