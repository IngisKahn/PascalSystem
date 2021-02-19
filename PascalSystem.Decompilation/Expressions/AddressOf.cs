namespace PascalSystem.Decompilation.Expressions
{
    using System.Text;
    using Types;

    public class AddressOf : Expression
    {
        public AddressOf(Expression exp)
        {
            this.Child = exp;
            this.Type = new Pointer(exp.Type).Proxy();
        }

        public Expression Child { get; }

        public override Base Type { get; }

        internal override void BuildString(StringBuilder builder)
        {
            builder.Append('@');
            this.Child.BuildString(builder);
        }
    }

    public partial class Expression
    {
        public static Expression AddressOf(Expression child) => child.Type.ResolvesTo<Array>() ? child : new AddressOf(child);
    }
}