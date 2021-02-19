namespace PascalSystem.Decompilation.Expressions
{
    using System.Text;
    using Types;

    public class Dereference : Expression
    {
        public Dereference(Expression exp)
        {
            this.Child = exp;
            this.Type = exp.Type.As<Pointer>().Dereference().Proxy();
        }

        public Expression Child { get; }

        public override Base Type { get; }

        internal override void BuildString(StringBuilder builder)
        {
            this.Child.BuildString(builder);
            builder.Append('^');
        }
    }

    public partial class Expression
    {
        public static Dereference Dereference(Expression child) => new(child);
    }
}