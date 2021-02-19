namespace PascalSystem.Decompilation.Expressions
{
    using System.Collections.Generic;
    using System.Text;

    public class Call : Expression
    {
        internal Call(MethodAnalyzer mti, IList<Expression> parameters, bool isExternal = false)
        {
            this.MethodInfo = mti;
            this.Parameters = parameters;
            this.IsExternal = isExternal;
        }

        public MethodAnalyzer MethodInfo { get; }
        public bool IsExternal { get; }
        public IList<Expression> Parameters { get; }

        public override Types.Base Type => this.MethodInfo.Signature.ReturnType;

        internal override void BuildString(StringBuilder builder)
        {
            builder.Append(this.IsExternal ? this.MethodInfo.Signature.FullName : this.MethodInfo.Signature.Name);
            if (this.Parameters.Count == 0)
                return;
            builder.Append('(');
            for (var i = 1; i < this.Parameters.Count; i++)
            {
                builder.Append(", ");
                this.Parameters[i].BuildString(builder);
            }
            builder.Append(')');
            if (this.Parameters[0].Type is Types.Void)
                return;
            builder.Append(" : ");
            builder.Append(this.Parameters[0]);
        }
    }
}