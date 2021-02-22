namespace PascalSystem.Decompilation.Expressions
{
    using System.Collections.Generic;
    using System.Text;
    using Model;

    public class Call : Expression
    {
        internal Call(MethodSignature mti, IList<Expression> parameters, bool isExternal = false)
        {
            this.MethodInfo = mti;
            this.Parameters = parameters;
            this.IsExternal = isExternal;
        }

        public MethodSignature MethodInfo { get; }
        public bool IsExternal { get; }
        public IList<Expression> Parameters { get; }

        public override Types.Base Type => this.MethodInfo.ReturnType;

        internal override void BuildString(StringBuilder builder)
        {
            builder.Append(this.IsExternal ? this.MethodInfo.FullName : this.MethodInfo.Name);
            if (this.Parameters.Count == 0)
            {
                builder.Append("()");
                return;
            }

            builder.Append('(');
            var once = false;
            foreach (var p in this.Parameters)
            {
                if (once)
                    builder.Append(", ");
                else
                    once = true;
                p.BuildString(builder);
            }

            builder.Append(')');
        }
    }

    public partial class Expression
    {
        public static Call Call(MethodSignature mti, IList<Expression> parameters, bool isExternal = false)
        {
            BitCount pos = default;
            foreach (var parameter in parameters)
            {
                mti.Parameters.MeetAt(pos, parameter.Type);
                pos += (BitCount)16;
            }
            // start analyzing child
            //if (mti.Method != null)
            //    mti.ProcessDataFlow(mti.Site.TopSite.ToString());
            return new(mti, parameters, isExternal);
        }
    }
}