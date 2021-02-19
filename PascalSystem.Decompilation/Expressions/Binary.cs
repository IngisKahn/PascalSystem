namespace PascalSystem.Decompilation.Expressions
{
    public abstract class Binary : Expression
    {
        protected Binary(Expression left, Expression right)
        {
            this.Left = left;
            this.Right = right;
        }

        public Expression Left { get; }
        public Expression Right { get; }

        public override Types.Base Type => this.Left.Type;
    }
}