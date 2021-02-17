namespace PascalSystem.Decompilation.Expressions
{
    public abstract class Statement : Expression
    {
        public override Types.Base Type => Types.Void.Instance;
    }
}