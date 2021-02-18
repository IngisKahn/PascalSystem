namespace PascalSystem.Decompilation.Types
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Model;

    public class Interval
    {
        private readonly struct RangeSlot
        {
            public RangeSlot(ByteCount offset, Base type)
            {
                this.Offset = offset;
                this.Type = type;
            }

            public ByteCount Offset { get; }
            public ByteCount Length => this.Type.Size;
            public Base Type { get; }

            public override string ToString() => $"{this.Type} @0x{this.Offset:X} b";
        }

        private class RangeSlotComparer : IComparer<RangeSlot>
        {
            public int Compare(RangeSlot x, RangeSlot y) => ((int)x.Offset).CompareTo((int)y.Offset);
        }

        private static readonly RangeSlotComparer slotComparer = new();
        //private readonly int maxByte;

        private readonly List<RangeSlot> slots = new();

        public Interval(ByteCount bytes) { }

        public int Count => this.slots.Count;

        internal Base MeetAt(ByteCount offset, Base type)
        {
            var slot = new RangeSlot(offset, type.Proxy());
            if (this.slots.Count == 0)
            {
                this.slots.Add(slot);
                return slot.Type;
            }
            var pos = this.slots.BinarySearch(slot, Interval.slotComparer);

            if (pos < 0)
                pos = ~pos;// - 1;

            RangeSlot? left = null, right = null;
            if (pos > 0) // check left
                left = this.slots[pos - 1];
            var b = false;
            if (pos < this.slots.Count) // check right
            {
                right = this.slots[pos];
                if (right.Value.Offset == offset)
                    return right.Value.Type.MeetWith(type, ref b);
            }
            // cases
            // 1. right doesn't exist or it is past our type
            // 2. it fits within our type, meetAt
            // 3. it doesn't fit - error
            if (right.HasValue && right.Value.Offset < offset + (ByteCount)type.Size)
            {

                var subOffset = right.Value.Offset - offset;
                if (subOffset + right.Value.Length > (ByteCount)type.Size)
                    throw new DecompilationException("Types overlap");
                this.slots.RemoveAt(pos);
                type.MeetAt(subOffset, right.Value.Type);
            }
            // cases
            // 1. there's nothing to the left - insert
            // 2. we don't overlap - insert
            // 3. offset is 0 - meet
            // 4. left has room for type - meet at
            // 5. overlap error
            if (!left.HasValue || left.Value.Offset + (ByteCount)left.Value.Type.Size <= offset)
            {
                this.slots.Insert(pos, slot);
                return type;
            }
            if (left.Value.Offset == offset)
                return left.Value.Type.MeetWith(type, ref b);
            if ((ByteCount)type.Size > left.Value.Length - (offset - left.Value.Offset))
                throw new InvalidOperationException("Types overlap");
            left.Value.Type.MeetWith(new Record().Proxy(), ref b);
            left.Value.Type.MeetAt(offset - left.Value.Offset, type.Proxy());
            return type;
        }
        public Base? GetTypeAtOffset(ByteCount offset)
        {
            if (this.slots.Count == 0)
                return null;
            var slot = new RangeSlot(offset, null);
            var pos = this.slots.BinarySearch(slot, Interval.slotComparer);

            return pos < 0 ? null : this.slots[pos].Type;
        }

        public static explicit operator Base[](Interval range) => range.slots.Select(s => s.Type).ToArray();
    }
}