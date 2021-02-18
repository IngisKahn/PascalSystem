namespace PascalSystem.Decompilation.Types
{
    using Model;

    public class Pointer : Base
    {
        public static BitCount StandardSize { get; } = (WordCount)1;

        public Base PointsTo { get; private set; }

        public Pointer() => this.PointsTo = Void.Instance;

        public Pointer(Base pointsTo) => this.PointsTo = pointsTo;
        public override Base Clone() => new Pointer(this.PointsTo.Clone());

        private static int pointerNesting;

        public override bool Equals(Base other)
        {
            if (other is not Pointer pt)
                return false;
            if (++Pointer.pointerNesting >= 20)
                return true;
            var result = this.PointsTo.Equals(pt.PointsTo);
            Pointer.pointerNesting--;
            return result;
        }

        public int PointerDepth
        {
            get
            {
                var d = 1;
                var pt = this.PointsTo as Pointer;
                while (pt != null)
                {
                    pt = pt.PointsTo as Pointer;
                    d++;
                }
                return d;
            }
        }

        public Base FinalPointsTo
        {
            get
            {
                var pt = this.PointsTo;
                var ptt = pt as Pointer;
                while (ptt != null)
                {
                    pt = ptt.PointsTo;
                    ptt = ptt.PointsTo as Pointer;
                }
                return pt;
            }
        }

        public bool PointsToAlpha => this.PointsTo is Void;

        public override Base MeetWith(Base other, ref bool hasChanged, bool setToHighestPointer = false)
        {
            if (other.ResolvesTo<Void>() || other.ResolvesTo<SizeRange>() && other.Size <= Pointer.StandardSize)
                return this; 
            if (other.ResolvesTo<Array>())
                return this.PointsTo.IsCompatibleWith(other.As<Array>().BaseType)
                    ? this.PointsTo.MeetWith(other, ref hasChanged, setToHighestPointer)
                    : throw new DecompilationException("Bad array pointer");

            var otherPtr = other.As<Pointer>();
            if (this.PointsToAlpha && !otherPtr.PointsToAlpha)
            {
                this.PointsTo = otherPtr.PointsTo;
                hasChanged = true;
            }
            else
            {
                var thisBase = this.PointsTo;
                var otherBase = otherPtr.PointsTo;
                if (setToHighestPointer)
                {
                    if (thisBase.IsSubTypeOrEqual(otherBase))
                        return other.Clone();
                    return otherBase.IsSubTypeOrEqual(thisBase) ? this : new();
                }

                // should make sure they don't point to themselves

                if (thisBase.Equals(otherBase))
                    return this;
                if (this.PointerDepth == otherPtr.PointerDepth)
                {
                    var fType = this.FinalPointsTo;
                    if (fType.ResolvesTo<Void>())
                        return other.Clone();
                    var ofType = otherPtr.FinalPointsTo;
                    if (ofType.ResolvesTo<Void>())
                        return this;
                    if (fType.Equals(ofType))
                        return this;
                }

                if (!thisBase.IsCompatibleWith(otherBase))
                    throw new DecompilationException("Can't meet with this pointer");
                this.PointsTo = this.PointsTo.MeetWith(otherBase, ref hasChanged);
                return this;
            }

            return this;
        }

        public override bool IsCompatible(Base other, bool testAllElements) =>
            other.ResolvesTo<Void>() 
            || (other.ResolvesTo<SizeRange>() && other.Size == Pointer.StandardSize
            || other.ResolvesTo<Pointer>() && this.PointsTo.IsCompatibleWith(other.As<Pointer>().PointsTo));

        public override BitCount Size => (WordCount)1;

        public override string ToString() => "^" + this.PointsTo;
    }
}