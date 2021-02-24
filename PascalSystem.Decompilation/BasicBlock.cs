namespace PascalSystem.Decompilation
{
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using Expressions;

    public class BasicBlock
    {
        private readonly MethodAnalyzer methodAnalyzer;
        public int Id { get; set; }
        public int StartIndex { get; }
        public int EndIndex { get; private set; }
        public string Label { get; }
        public List<BasicBlock> Dominates { get; } = new();

        public BasicBlock(MethodAnalyzer methodAnalyzer, int id, int startIndex, int endIndex)
            : this(methodAnalyzer.Signature.Name, methodAnalyzer, id, startIndex, endIndex) { }
        public BasicBlock(string label, MethodAnalyzer methodAnalyzer, int id, int startIndex, int endIndex)
        {
            this.methodAnalyzer = methodAnalyzer;
            this.Id = id;
            this.StartIndex = startIndex;
            this.EndIndex = endIndex;
            this.Label = label;
        }
        public int Length => this.EndIndex - this.StartIndex + 1;

        public List<ControlEdge> EdgesOut { get; private init; } = new();

        public List<ControlEdge> EdgesIn { get; } = new();
        public List<Expression> Statements { get; } = new();

        //public override int GetHashCode() => this.Id;

        public void AddEdge(BasicBlock destination)
        {
            if (this.EdgesOut.Any(e => e.Destination == destination))
                return;
            // have we been there before?
            var visted = new HashSet<BasicBlock>();
            var pending = new Queue<BasicBlock>();
            visted.Add(this);
            pending.Enqueue(this);

            var isBack = false;
            while (pending.Count != 0)
            {
                var bb = pending.Dequeue();
                foreach (var inBasicBlock in bb.EdgesIn.Select(e => e.Source))
                {
                    if (destination == inBasicBlock)
                    {
                        isBack = true;
                        pending.Clear();
                        break;
                    }
                    if (visted.Contains(inBasicBlock))
                        break;
                    visted.Add(inBasicBlock);
                    pending.Enqueue(inBasicBlock);
                }
            }

            var edge = new ControlEdge(this, destination, isBack);
            this.EdgesOut.Add(edge);
            destination.EdgesIn.Add(edge);
        }
        internal BasicBlock? SplitAt(int index)
        {
            if (index > this.EndIndex || index == this.StartIndex)
                return null;
            var endIndex = this.EndIndex;
            this.EndIndex = index - 1;
            var result = new BasicBlock(this.methodAnalyzer, this.methodAnalyzer.BlockList.Count, index, endIndex) { EdgesOut = new(this.EdgesOut) };
            foreach (var controlEdge in this.EdgesOut)
                controlEdge.Source = result;
            this.EdgesOut.Clear();
            return result;
        }

        public class ControlEdge
        {
            public BasicBlock Source { get; set; }
            public BasicBlock Destination { get; }
            public bool IsBack { get; }

            public ControlEdge(BasicBlock source, BasicBlock destination, bool isBack)
            {
                this.Source = source;
                this.Destination = destination;
                this.IsBack = isBack;
            }
        }

        public async Task Dump(IndentedTextWriter writer)
        {
            if (this.Statements.Count > 1)
                await writer.WriteLineAsync("BEGIN");
            writer.Indent++;
            foreach (var statement in this.Statements)
                await statement.Dump(writer);
            writer.Indent--; 
            if (this.Statements.Count > 1)
                await writer.WriteLineAsync("END; {" + this.Label + "}");
        }
        public override string ToString() => $"BB{this.Id}: Size:{this.Length} "
                                             + $"In:[{string.Join(", ", this.EdgesIn.Select(ce => ce.Source.Id.ToString(CultureInfo.InvariantCulture)))}] "
                                             + $"Out:[{string.Join(", ", this.EdgesOut.Select(ce => ce.Destination.Id.ToString(CultureInfo.InvariantCulture)))}]";
    }
}
