namespace PascalSystem.Decompilation.Types
{
    using System;
    using Model;

    public class SizeRange : Base
    {
        public BitCount Minimum { get; private set; }
        public BitCount Maximum { get; private set; }
        public override BitCount Size => this.Maximum;
        public override Base Clone() => new SizeRange(this.Minimum, this.Maximum);
        public override bool Equals(Base other) => other is SizeRange r && this.Minimum == r.Minimum && this.Maximum == r.Maximum;

        public bool IsCompatibleWithSize(BitCount size) => this.Minimum <= size && this.Maximum >= size;
        public override string ToString()
        {
            if (this.Minimum != this.Maximum)
                return $"SizeRange{this.Minimum}To{this.Maximum}Bits";

            var bits = (int)this.Size;
            if ((bits & 0x7) != 0)
                return "Size" + bits + "Bits";
            var bytes = bits >> 3;
            if ((bytes & 1) != 0)
                return "Size" + bytes + "Bytes";
            var words = bytes >> 1;
            return words == 1 ? "Word" : "Size" + words + "Words";
        }

        public SizeRange(BitCount minimum, BitCount maximum)
        {
            this.Minimum = minimum;
            this.Maximum = maximum;
        }
        public override Base MergeWith(Base other)
        {
            var ret = other.Clone();
            if (ret is not SizeRange rs)
                return ret;

            rs.Minimum = this.Minimum;
            rs.Maximum = this.Maximum;

            return ret;
        }
        public override Base MeetWith(Base other, ref bool hasChanged, bool setToHighestPointer = false)
        {
            if (other.ResolvesTo<Void>())
                return this;
            if (other.ResolvesTo<SizeRange>())
            {
                var otherRange = other.As<SizeRange>();
                var oldMin = this.Minimum;
                var oldMax = this.Maximum;
                this.Minimum = (BitCount)Math.Max((int)this.Minimum, (int)otherRange.Minimum);
                this.Maximum = (BitCount)Math.Min((int)this.Maximum, (int)otherRange.Maximum);
                hasChanged |= this.Minimum != oldMin || this.Maximum != oldMax;
                return this;
            }
            hasChanged = true;
            if (other.ResolvesTo<Integer>() || other.ResolvesTo<Real>() || other.ResolvesTo<Pointer>() ||
                other.ResolvesTo<Boolean>() || other.ResolvesTo<Character>() || other.ResolvesTo<Record>())
                return other.Clone(); // check sizes
            throw new DecompilationException("Size could not meet types");
        }

        public override bool IsCompatible(Base other, bool testAllElements) =>
            other.ResolvesTo<Void>() || (other.Size == this.Size || (int)other.Size == 0 ||
                                         (!other.ResolvesTo<Array>() ||
                                          this.IsCompatibleWith(other.As<Array>().BaseType)));

        public override Base MeetAt(ByteCount offset, Base other) =>
            (BitCount)offset + other.Size > this.Size
                ? throw new DecompilationException("Types overlap")
                : base.MeetAt(offset, other);
    }
}