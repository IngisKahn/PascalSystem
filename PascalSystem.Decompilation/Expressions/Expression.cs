namespace PascalSystem.Decompilation.Expressions
{
    using System.CodeDom.Compiler;
    using System.Text;
    using System.Threading.Tasks;

    public abstract partial class Expression
    {
        internal abstract void BuildString(StringBuilder builder);

        internal virtual Task Dump(IndentedTextWriter writer)
        {
            StringBuilder stringBuilder = new();
            this.BuildString(stringBuilder);
            return writer.WriteLineAsync(stringBuilder.ToString());
        }

        public override string ToString()
        {
            StringBuilder builder = new();
            this.BuildString(builder);
            builder.Append(" : ");
            builder.Append(this.Type);
            return builder.ToString();
        }

        public abstract Types.Base Type { get; }
    }
}
