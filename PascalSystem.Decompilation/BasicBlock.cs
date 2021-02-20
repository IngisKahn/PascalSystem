namespace PascalSystem.Decompilation
{
    public class BasicBlock
    {
        private readonly MethodAnalyzer methodAnalyzer;
        private readonly int id;
        public int StartIndex { get; }
        public int EndIndex { get; }

        public BasicBlock(MethodAnalyzer methodAnalyzer, int id, int startIndex, int endIndex)
        {
            this.methodAnalyzer = methodAnalyzer;
            this.id = id;
            this.StartIndex = startIndex;
            this.EndIndex = endIndex;
        }
    }
}
