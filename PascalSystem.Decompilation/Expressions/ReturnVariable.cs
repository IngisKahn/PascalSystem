namespace PascalSystem.Decompilation.Expressions
{
    using System.Text;

    public class ReturnVariable : Expression
    {
        internal ReturnVariable(MethodSignature method, Types.Base type)
        {
            this.Type = type;
            this.Method = method;
        }

        public override Types.Base Type { get; }

        public MethodSignature Method { get; }
        internal override void BuildString(StringBuilder builder)
        {
            builder.Append(this.Method.Name);
        }
    }
}