namespace PascalSystem.Runtime
{
    internal static class Array
    {
        /// <summary>
        /// The StrCmp function is used to compare two Pascal strings.
        /// </summary>
        /// <param name="s1">The left hand side of the comparison (memory word address)</param>
        /// <param name="s2">The right hand side of the comparison (memory word address).</param>
        /// <returns>minus one (-1) if s1 &lt; s2;
        /// zero (0) if s1 == s2; or,
        /// plus one (1) if s1 > s2.</returns>
        public static int StrCmp(ushort s1, ushort s2)
        {
            var len1 = Memory.ReadByte(s1, 0);
            var len2 = Memory.ReadByte(s2, 0);
            var len = len1 < len2 ? len1 : len2; /* Length to compare */

            for (byte i = 1; i <= len; i++)
            {
                var ch1 = Memory.ReadByte(s1, i); /* Get a char from both strings */
                var ch2 = Memory.ReadByte(s2, i);
                if (ch1 < ch2)
                    return -1; /* S1 < S2 */
                if (ch1 > ch2)
                    return 1; /* S1 > S2 */
            }
            /* All chars in range of common length are equal. */
            if (len1 < len2)
                /* S1 ist shorter?  If so S1 < S2 */
                return -1;
            return len1 > len2 ? 1 : 0;
            /* both strings have the same length, so they are equal. */
        }

        public static int ByteCmp(ushort ba1, ushort ba2, ushort len)
        {
            for (ushort i = 0; i < len; i++)
            {
                var ch1 = Memory.ReadByte(ba1, (short)i); /* Get a char from both strings */
                var ch2 = Memory.ReadByte(ba2, (short)i);
                if (ch1 < ch2)
                    return -1; /* BA1 < BA2 */
                if (ch1 > ch2)
                    return 1; /* BA1 > BA2 */
            }
            return 0;
        }

        public static int WordCmp(ushort wa1, ushort wa2, ushort len)
        {
            for (ushort i = 0; i < len; i++)
                if (Memory.Read(wa1.Index(i)) != Memory.Read(wa2.Index(i)))
                    return 1;
            return 0;
        }
    }
}