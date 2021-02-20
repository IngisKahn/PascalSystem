namespace PascalSystem.Decompilation.Expressions
{
    using System.CodeDom.Compiler;
    using System.Threading.Tasks;

    public abstract class Statement : Expression
    {
        public override Types.Base Type => Types.Void.Instance;

        public async Task Dump(IndentedTextWriter writer)
        {
            //this.BuildString();
        }
    }
}