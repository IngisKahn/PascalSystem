namespace PascalSystem.Decompilation.Expressions
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class Case : Statement
    {
        internal Case(int minimum, BasicBlock defaultBlock, IList<BasicBlock> cases,
            Expression expression)
        {
            this.Minimum = minimum;
            this.Default = defaultBlock;
            this.cases = cases;
            this.Expression = expression;
        }

        public int Minimum { get; }
        public BasicBlock Default { get; private set; }
        private readonly IList<BasicBlock> cases;

        public void ReplaceBlock(BasicBlock oldBlock, BasicBlock newBlock)
        {
            if (this.Default == oldBlock)
                this.Default = newBlock;
            for (var x = 0; x < this.cases.Count; x++)
                if (this.cases[x] == oldBlock)
                    this.cases[x] = newBlock;
        }

        public IEnumerable<(BasicBlock, int[])> Cases => this.cases
            .Select((b, i) => new {Block = b, Index = i + this.Minimum})
            .Where(b => b.Block != this.Default)
            .GroupBy(bi => bi.Block)
            .Select(g => (g.Key, g.Select(gi => gi.Index).ToArray()));
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