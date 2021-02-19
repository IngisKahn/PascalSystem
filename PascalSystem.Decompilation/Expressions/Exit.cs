namespace PascalSystem.Decompilation.Expressions
{
    using System.Text;

    public class Exit : Expression
    {
        public string? Unit { get; }
        public int Method { get; }

        internal Exit(string? unit, int method)
        {
            this.Unit = unit;
            this.Method = method;
        }


        public override Types.Base Type => Types.Void.Instance;

        internal override void BuildString(StringBuilder builder)
        {
            builder.Append("EXIT ");
            if (this.Unit is not null)
                builder.Append(this.Unit + '.');
            builder.Append("M" + this.Method);
        }
    }
}