namespace PascalSystem.Decompilation.Expressions
{
    using System.Text;
    using Model;
    using Types;

    public class Constant : Expression
    {
        public Constant(object? value, Types.Base type)
        {
            this.Type = type;
            this.Value = value;
        }
        public Constant(int value)
        {
            var bits = value - (value >> 1 & 0x5555);
            bits = (bits & 0x3333) + (bits >> 2 & 0x3333);
            bits = bits + (bits >> 4) & 0xF0F;
            bits = bits * 0x101 >> 8;
            Base type = bits < 16 ? new Types.SizeRange((BitCount)bits, (BitCount)16) : new Types.Integer();
            this.Type = type.Proxy();
            this.Value = value;
        }

        public object? Value { get; }
        public override Types.Base Type { get; }
        internal override void BuildString(StringBuilder builder) => this.Type.Display(this.Value?? "NIL", builder);
    }

    public partial class Expression
    {
        public static Constant Constant(int value) => new(value);
        public static Constant Constant<T>(object? value) where T : Base, new() => new(value, new T().Proxy());
    }
}