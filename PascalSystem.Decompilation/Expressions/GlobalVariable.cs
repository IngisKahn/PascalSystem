namespace PascalSystem.Decompilation.Expressions
{
    using System.Text;
    using Model;

    public class GlobalVariable : OffsetVariable
    {
        internal GlobalVariable(Model.WordCount offset, Types.Base type) : base(offset, type) { }

        internal override void BuildString(StringBuilder builder)
        {
            builder.Append("Global" + ((int)this.Offset).ToString("X"));
        }
    }

    public partial class Expression
    {
        public static GlobalVariable Global(WordCount offset, Types.Base type) =>
            new GlobalVariable(offset, type.Proxy());
    }
}