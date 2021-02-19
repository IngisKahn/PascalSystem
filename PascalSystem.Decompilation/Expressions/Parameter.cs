namespace PascalSystem.Decompilation.Expressions
{
    using System.Text;
    using Model;

    public class Parameter : OffsetVariable
    {
        internal Parameter(Model.WordCount offset, Types.Base type) : base(offset, type) { }
        internal override void BuildString(StringBuilder builder)
        {
            builder.Append("Parameter" + ((int)this.Offset).ToString("X"));
        }
    }

    public partial class Expression
    {
        public static Parameter Parameter(WordCount offset, Types.Base type) => new(offset, type.Proxy());
    }
}