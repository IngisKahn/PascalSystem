namespace PascalSystem.Decompilation.Expressions
{
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;

    public abstract class Statement : Expression
    {
        public override Types.Base Type => Types.Void.Instance;
        
    }

    public class Block : Statement
    {
        public List<Expression> Statements { get; } = new();
        public string Label { get; }

        public Block(string label) => this.Label = label;

        internal override void BuildString(StringBuilder builder)
        {
            foreach (var statement in this.Statements)
            {
                statement.BuildString(builder);
                builder.AppendLine();
            }
        }

        internal override async Task Dump(IndentedTextWriter writer)
        {
            await writer.WriteLineAsync("BEGIN");
            writer.Indent++;
            foreach (var statement in this.Statements)
            {
                await statement.Dump(writer);
                await writer.WriteLineAsync();
            }
            writer.Indent--;
            await writer.WriteLineAsync($"END; {{{this.Label}}}");
        }
    }
}