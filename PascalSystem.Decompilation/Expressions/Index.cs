namespace PascalSystem.Decompilation.Expressions
{
    using System.Text;

    public class Index : Expression
    {
        public Index(Expression array, Expression index)
        {
            this.Array = array;
            this.IndexValue = index;
        }

        public Expression Array { get; }
        public Expression IndexValue { get; }

        public override Types.Base Type => this.Array.Type.As<Types.Array>().BaseType;
        internal override void BuildString(StringBuilder builder)
        {
            this.Array.BuildString(builder);
            builder.Append('[');
            this.IndexValue.BuildString(builder);
            builder.Append(']');
        }
    }
}