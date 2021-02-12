namespace PascalSystem.Runtime
{
    using System;
    using System.Linq;

    public class Set
    {
        public ushort Size => (ushort)this.Data.Length;
        public ushort[] Data { get; }

        private Set(ushort[] data) => this.Data = data;

        /// <summary>
        ///     The SetPop function is used to pop a set from the value stack.
        ///     The top-of-stack is the number of words in the set (always &lt;=
        ///     255).  Subsequent values are the words of the set from least bit to
        ///     greatest bit.
        /// </summary>
        /// <returns></returns>
        public static Set Pop()
        {
            var size = Stack.Pop();
            var data = new ushort[size];
            for (var i = 0; i < size; i++)
                data[i] = Stack.Pop();
            return new(data);
        }

        /// <summary>
        ///     The AdjSet function is used to adjust the size of a set.
        ///     This is often needed for set comparisons.
        /// </summary>
        /// <param name="size">The desired size of the set.</param>
        public Set Adjust(ushort size)
        {
            var d = this.Data;
            System.Array.Resize(ref d, size);
            return new(d);
        }

        /// <summary>
        ///     The SetPush function is used to push a set onto the value stack.
        ///     When completed, the top-of-stack is the number of words in the set
        ///     (always &lt;= 255).  Subsequent values are the words of the set from
        ///     least bit to greatest bit.
        /// </summary>
        public void Push()
        {
            foreach (var d in this.Data)
                Stack.Push(d);
            Stack.Push(this.Size);
        }

        /// <summary>
        ///     The SetNeq function is used to compare sets for inequality.
        ///     (The sets may be adjusted to be the same length as each other.)
        /// </summary>
        /// <param name="other">The set to compare to.</param>
        /// <returns>true if the sets are not equal</returns>
        public bool IsNotEqual(Set other)
        {
            var size = Math.Max(this.Size, other.Size);
            var left = this;
            var right = other;
            if (left.Size < size)
                left = left.Adjust(size);
            else if (right.Size < size)
                right = right.Adjust(size);
            return !left.Data.Zip(right.Data, (a, b) => a == b).All(b => b);
        }

        /// <summary>
        ///     The set_is_improper_subset function is used to compare sets for
        ///     improper (&lt;=) subset or superset.
        /// </summary>
        /// <param name="subset">The alleged sub set.</param>
        /// <returns></returns>
        public bool IsImproperSubset(Set subset)
        {
            var size = subset.Size;
            while (size != 0 && subset.Data[size - 1] == 0)
                size--;
            return this.Size >= size && this.Data.Zip(subset.Data, (super, sub) => (super & sub) == sub).All(b => b);
        }

        /// <summary>
        ///     The set_is_proper_subset function is used to compare sets for
        ///     proper (&lt;) subset or superset.
        /// </summary>
        /// <param name="subset">The alleged sub set.</param>
        /// <returns></returns>
        public bool IsProperSubset(Set subset) => this.IsImproperSubset(subset) && this.IsNotEqual(subset);
    }
}