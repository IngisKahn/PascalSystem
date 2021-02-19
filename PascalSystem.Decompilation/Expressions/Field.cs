namespace PascalSystem.Decompilation.Expressions
{
    using System.Text;
    using Model;

    public class Field : Expression
    {
        public Field(Expression recordExpression, Types.Base fieldType, WordCount offset)
        {
            this.RecordExpression = recordExpression;
            this.FieldType = fieldType;
            this.Offset = offset;
        }

        public Expression RecordExpression { get; }
        public Types.Base FieldType { get; }

        public WordCount Offset { get; }

        public override Types.Base Type => this.FieldType;

        internal override void BuildString(StringBuilder builder)
        {
            this.RecordExpression.BuildString(builder);
            builder.Append(".Field");
            builder.Append(((int)this.Offset).ToString("X"));
        }
    }
}