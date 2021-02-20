namespace PascalSystem.Decompilation
{
    using System.CodeDom.Compiler;
    using System.Threading.Tasks;

    public class BasicBlock
    {
        private readonly MethodAnalyzer methodAnalyzer;
        private readonly int id;
        private readonly int startIndex;
        private readonly int endIndex;

        public BasicBlock(MethodAnalyzer methodAnalyzer, int id, int startIndex, int endIndex)
        {
            this.methodAnalyzer = methodAnalyzer;
            this.id = id;
            this.startIndex = startIndex;
            this.endIndex = endIndex;
        }
    }
}
