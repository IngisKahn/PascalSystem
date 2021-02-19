namespace PascalSystem.Decompilation.Expressions
{
    public abstract class OffsetVariable : Expression
    {
        internal OffsetVariable(Model.WordCount offset, Types.Base type)
        {
            this.Type = type;
            this.Offset = offset;
        }

        public override Types.Base Type { get; }

        public Model.WordCount Offset { get; }
    }
}