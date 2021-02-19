namespace PascalSystem.Decompilation.Types
{
    using System.Text;

    public class String : Pointer
    {
        //public int Length { get { return 2; } }

        //public string Name { get { return "string"; } }

        public String()
            : base(new Character()) { }

        public override Base Clone() => this;

        public override void Display(object value, StringBuilder builder)
        {
            builder.Append('\'');
            var index = builder.Length;
            var valueString = value.ToString() ?? string.Empty;
            builder.Append(valueString);
            builder.Replace("'", "''", index, valueString.Length);
            builder.Append('\'');
        }
        //    return type.Name == this.Name && type.Length == this.Length;
        //{
        //public bool IsIdentical(IType type)
        //}

        //public bool IsCompatable(IType type)
        //{
        //    return this.IsIdentical(type);
        //}


        //public bool IsAssignableFrom(IType type)
        //{
        //    throw new NotImplementedException();
        //}
    }
}