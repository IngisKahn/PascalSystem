﻿namespace PascalSystem.Decompilation.Expressions
{
    using System.Linq;
    using System.Reflection;
    using System.Text;

    public abstract partial class Expression
    {
        internal abstract void BuildString(StringBuilder builder);

        public override string ToString()
        {
            StringBuilder builder = new();
            this.BuildString(builder);
            builder.Append(" : ");
            builder.Append(this.Type);
            return builder.ToString();
        }

        public abstract Types.Base Type { get; }
    }
}
