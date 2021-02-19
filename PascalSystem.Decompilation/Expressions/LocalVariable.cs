namespace PascalSystem.Decompilation.Expressions
{
    using System.Text;
    using Model;

    public class LocalVariable : OffsetVariable
    {
        internal LocalVariable(Model.WordCount offset, Types.Base type) : base(offset, type) { }
        internal override void BuildString(StringBuilder builder)
        {
            builder.Append("Local" + ((int)this.Offset).ToString("X"));
        }
    }

    public partial class Expression
    {
        public static LocalVariable Local(WordCount offset, Types.Base type) => new(offset, type.Proxy());
    }
}