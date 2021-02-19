namespace PascalSystem.Decompilation.Expressions
{
    using System.Text;

    public class FunctionReturn : Statement
    {
        internal FunctionReturn(MethodSignature mti) => this.Name = mti.Name;
        public string Name { get; }
        internal override void BuildString(StringBuilder builder) => builder.Append(this.Name + " := ");
    }
}