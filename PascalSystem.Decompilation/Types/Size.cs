namespace PascalSystem.Decompilation.Types
{
    using Model;

    public class Size : SizeRange
    {
        public Size(BitCount bits) : base(bits, bits) { }
    }
}