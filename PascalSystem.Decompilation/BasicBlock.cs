namespace PascalSystem.Decompilation
{
    using System.Collections.Generic;
    using System.Linq;

    public class BasicBlock
    {
        private readonly MethodAnalyzer methodAnalyzer;
        private readonly int id;
        public int StartIndex { get; }
        public int EndIndex { get; private set; }

        public BasicBlock(MethodAnalyzer methodAnalyzer, int id, int startIndex, int endIndex)
        {
            this.methodAnalyzer = methodAnalyzer;
            this.id = id;
            this.StartIndex = startIndex;
            this.EndIndex = endIndex;
        }
        public int Length => this.EndIndex - this.StartIndex + 1;

        public List<ControlEdge> EdgesOut { get; private set; } = new();

        public List<ControlEdge> EdgesIn { get; } = new();

        public override int GetHashCode() => this.id;

        public void AddEdge(BasicBlock destination)
        {
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
            var result = new BasicBlock(this.methodAnalyzer, this.methodAnalyzer.BlockList.Count, index, endIndex) { EdgesOut = this.EdgesOut };
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
    }
}
