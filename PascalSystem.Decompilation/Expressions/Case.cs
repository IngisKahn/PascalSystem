namespace PascalSystem.Decompilation.Expressions
{
    using System.Collections.Generic;
    using System.Text;

    public class Case : Statement
    {
        internal Case(int minimum, BasicBlock defaultBlock, IEnumerable<BasicBlock> cases,
            Expression expression)
        {
            this.Minimum = minimum;
            this.Default = defaultBlock;
            this.Cases = cases;
            this.Expression = expression;
        }

        public int Minimum { get; }
        public BasicBlock Default { get; }
        public IEnumerable<BasicBlock> Cases { get; }
        public Expression Expression { get; }

        internal override void BuildString(StringBuilder builder)
        {
            builder.Append("CASE ");
            this.Expression.BuildString(builder);
            builder.Append(" OF ");
            //var cases = from b in this.Cases
            //    from i in b.GetIndex()
            //    group new { Block = b, Index = i + this.Minimum } by b
            //    into blockGroup
            //    select new { Block = blockGroup.Key, Indexes = blockGroup };

            //foreach (var c in cases)
            //{
            //    var first = true;
            //    foreach (var i in c.Indexes)
            //    {
            //        if (first)
            //            first = false;
            //        else
            //            builder.Append(", ");
            //        builder.Append(i.Index);
            //    }
            //    builder.AppendFormat(": block{0}; ", c.Block.Id);
            //}

            builder.Append("END;");
        }
    }
}