namespace PascalSystem.Decompilation.Expressions
{
    using System.Text;

    public abstract partial class Expression
    {
        protected abstract string ToString(StringBuilder builder);

        public override string ToString() => this.ToString(new());

        public abstract Types.Base Type { get; }
    }

    public abstract class Statement : Expression
    {
        public override Types.Base Type => Types.Void.Instance;
    }
}
