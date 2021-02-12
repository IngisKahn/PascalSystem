namespace PascalSystem.Runtime
{
    using System;
    using System.Linq;

    internal static class LongInteger
    {
        private static int Compare(ref Bc first, ref Bc second, bool useSign)
        {
            if (useSign && first.Sign != second.Sign)
                return first.Sign == BcSign.Plus ? 1 : -1;

            var len1 = first.Value.Length;
            var len2 = second.Value.Length;

            if (len1 != len2)
                return len1 > len2 ? !useSign || first.Sign == BcSign.Plus ? 1 : -1 
                                   : !useSign || first.Sign == BcSign.Plus ? -1 : 1;

            if (len1 <= 0)
                return 0;
            return first.Value[len1 - 1] > second.Value[len2 - 1] ? !useSign || first.Sign == BcSign.Plus ? 1 : -1 
                                                                  : !useSign || first.Sign == BcSign.Plus ? -1 : 1;
        }

        private static int Compare(ref Bc first, ref Bc second) => LongInteger.Compare(ref first, ref second, true);

        private static bool IsZero(ref Bc num) => num.Value.All(b => b == 0);

        private static void RemoveLeadingZeros(ref Bc num)
        {
            if (num.Value[0] != 0)
                return;

            var bytes = num.Value.Length;
            var src = 0;
            while (bytes > 1 && num.Value[src] == 0)
            {
                bytes--;
                src++;
            }

            var temp = new byte[bytes];
            System.Array.Copy(num.Value, src, temp, 0, bytes);
            num.Value = temp;
        }

        private static void AddSignless(ref Bc first, ref Bc second, out Bc result)
        {
            var len1 = first.Value.Length;
            var len2 = second.Value.Length;
            result = new Bc(Math.Max(len1, len2) + 1);
            var i1 = len1 - 1;
            var i2 = len2 - 1;
            var i3 = result.Value.Length - 1;
            var carry = 0;

            while (len1 > 0 && len2 > 0)
            {
                var sum = first.Value[i1--] + second.Value[i2--] + carry;
                if (sum >= 10)
                {
                    carry = 1;
                    sum -= 10;
                }
                else
                    carry = 0;
                result.Value[i3--] = (byte)sum;
                len1--;
                len2--;
            }

            byte[] rem;
            if (len1 == 0)
            {
                len1 = len2;
                i1 = i2;
                rem = second.Value;
            }
            else
                rem = first.Value;

            while (len1-- > 0)
            {
                var sum = rem[i1--] + carry;
                if (sum >= 10)
                {
                    carry = 1;
                    sum -= 10;
                }
                else
                    carry = 0;
                result.Value[i3--] = (byte)sum;
            }

            if (carry != 0)
                result.Value[i3] += 1;

            LongInteger.RemoveLeadingZeros(ref result);
        }

        private static void SubtractSignless(ref Bc first, ref Bc second, out Bc result)
        {
            var len1 = first.Value.Length;
            var len2 = second.Value.Length;
            var diffLen = Math.Max(len1, len2);
            var minLen = Math.Min(len1, len2);
            result = new Bc(diffLen);

            var i1 = len1 - 1;
            var i2 = len2 - 1;
            var i3 = diffLen - 1;

            var borrow = 0;

            for (var count = 0; count < minLen; count++)
            {
                var val = first.Value[i1--] - second.Value[i2--] - borrow;
                if (val < 0)
                {
                    val += 10;
                    borrow = 1;
                }
                else
                    borrow = 0;
                result.Value[i3--] = (byte)val;
            }

            if (diffLen != minLen)
                for (var count = diffLen - minLen; count > 0; count--)
                {
                    var val = first.Value[i1--] - borrow;
                    if (val < 0)
                    {
                        val += 10;
                        borrow = 1;
                    }
                    else
                        borrow = 0;
                    result.Value[i3--] = (byte)val;
                }

            LongInteger.RemoveLeadingZeros(ref result);
        }

        private static void Add(ref Bc first, ref Bc second, out Bc result)
        {
            if (first.Sign == second.Sign)
            {
                LongInteger.AddSignless(ref first, ref second, out result);
                result.Sign = first.Sign;
                return;
            }

            switch (LongInteger.Compare(ref first, ref second))
            {
                case -1:
                    LongInteger.SubtractSignless(ref second, ref first, out result);
                    result.Sign = second.Sign;
                    break;
                case 1:
                    LongInteger.SubtractSignless(ref first, ref second, out result);
                    result.Sign = first.Sign;
                    break;
                default:
                    result = new(1);
                    break;
            }
        }

        private static void Subtract(ref Bc first, ref Bc second, out Bc result)
        {
            if (first.Sign != second.Sign)
            {
                LongInteger.AddSignless(ref first, ref second, out result);
                result.Sign = first.Sign;
                return;
            }

            switch (LongInteger.Compare(ref first, ref second))
            {
                case -1:
                    LongInteger.SubtractSignless(ref second, ref first, out result);
                    result.Sign = second.Sign == BcSign.Plus ? BcSign.Minus : BcSign.Plus;
                    break;
                case 1:
                    LongInteger.SubtractSignless(ref first, ref second, out result);
                    result.Sign = first.Sign;
                    break;
                default:
                    result = new(1);
                    break;
            }
        }

        private static void Multiply(ref Bc first, ref Bc second, out Bc result)
        {
            var len1 = first.Value.Length;
            var len2 = second.Value.Length;
            var totalDigits = len1 + len2;
            result = new Bc(totalDigits);
            var end1 = len1 - 1;
            var end2 = len2 - 1;
            var i3 = totalDigits - 1;
            var sum = 0;

            result.Sign = first.Sign == second.Sign ? BcSign.Plus : BcSign.Minus;

            for (var i = 0; i < totalDigits - 1; i++)
            {
                var i1 = end1 - Math.Max(0, i - len2 + 1);
                var i2 = end2 - Math.Min(i, len2 - 1);
                while (i1 >= 0 && i2 <= end2)
                    sum += first.Value[i1--] * second.Value[i2++];
                result.Value[i3--] = (byte)(sum % 10);
                sum /= 10;
            }
            result.Value[i3] = (byte)sum;

            LongInteger.RemoveLeadingZeros(ref result);
            if (LongInteger.IsZero(ref result))
                result.Sign = BcSign.Plus;
        }

        private static void MultiplyOne(byte[] num, int size, int digit, byte[] result, int offset1 = 0,
            int offset2 = 0)
        {
            switch (digit)
            {
                case 0:
                    for (var i = offset2; i < size; i++)
                        result[i] = 0;
                    return;
                case 1:
                    System.Array.Copy(num, offset1, result, offset2, size);
                    return;
            }

            var i1 = size - 1 + offset1;
            var i2 = size - 1 + offset2;
            var carry = 0;

            while (size-- > 0)
            {
                var value = num[i1--] * digit + carry;
                result[i2--] = (byte)(value % 10);
                carry = value / 10;
            }

            if (carry != 0)
                result[i2] = (byte)carry;
        }

        private static bool Divide(ref Bc first, ref Bc second, out Bc result)
        {
            if (LongInteger.IsZero(ref second))
            {
                result = new(0);
                return false;
            }

            if (second.Value.Length == 1 && second.Value[0] == 1)
            {
                result = new(first.Value.Length)
                {
                    Sign = first.Sign == second.Sign ? BcSign.Plus : BcSign.Minus
                };

                System.Array.Copy(first.Value, result.Value, first.Value.Length);

                return true;
            }

            var i2 = 0; // second.Value.Length - 1;

            var len1 = first.Value.Length;

            var num1 = new byte[len1 + 2];
            System.Array.Copy(first.Value, 0, num1, 1, len1);

            var len2 = second.Value.Length;
            var num2 = new byte[len2 + 1];
            System.Array.Copy(second.Value, num2, len2);

            while (num2[i2] == 0)
            {
                i2++;
                len2--;
            }

            int qDigits;
            bool zero;
            if (len2 > len1)
            {
                qDigits = 1;
                zero = true;
            }
            else
            {
                qDigits = len2 > len1 ? 1 : len1 - len2 + 1;
                zero = false;
            }

            result = new(qDigits);

            var mVal = new byte[len2 + 1];

            if (!zero)
            {
                var norm = 10 / (num2[i2] + 1);
                if (norm != 1)
                {
                    LongInteger.MultiplyOne(num1, len1 + 1, norm, num1);
                    LongInteger.MultiplyOne(num2, len2, norm, num2, i2, i2);
                }

                var qDigit = 0;
                var i3 = len2 > len1 ? len2 - len1 : 0;

                while (qDigit <= len1 - len2)
                {
                    var qGuess = num2[i2] == num1[qDigit] ? 9 : (num1[qDigit] * 10 + num1[qDigit + 1]) / num2[i2];

                    if (num2[i2 + 1] * qGuess > (num1[qDigit] * 10 + num1[qDigit + 1] - num2[i2] * qGuess) * 10 +
                        num1[qDigit + 2])
                    {
                        qGuess--;
                        if (num2[i2 + 1] * qGuess > (num1[qDigit] * 10 + num1[qDigit + 1] - num2[i2] * qGuess) * 10 +
                            num1[qDigit + 2])
                            qGuess--;
                    }

                    var borrow = 0;
                    if (qGuess != 0)
                    {
                        LongInteger.MultiplyOne(num2, len2, qGuess, mVal, i2, 1);
                        var j1 = qDigit + len2;
                        var j2 = len2;
                        for (var count = 0; count < len2 + 1; count++)
                        {
                            var val = num1[j1] - mVal[j2] - borrow;
                            if (val < 0)
                            {
                                val += 10;
                                borrow = 1;
                            }
                            else
                                borrow = 0;
                            num1[j1--] = (byte)val;
                        }
                    }

                    if (borrow == 1)
                    {
                        qGuess--;
                        var j1 = qDigit + len2;
                        var j2 = i2 + len2 - 1;
                        var carry = 0;

                        for (var count = 0; count < len2; count++)
                        {
                            var val = num1[j1] + num2[j2--] + carry;
                            if (val > 9)
                            {
                                val -= 10;
                                carry = 1;
                            }
                            else
                                carry = 0;
                            num1[j1--] = (byte)val;
                        }
                        if (carry == 1)
                            num1[j1] = (byte)((num1[j1] + 1) % 10);
                    }

                    result.Value[i3++] = (byte)qGuess;
                    qDigit++;
                }
            }

            result.Sign = first.Sign == second.Sign ? BcSign.Plus : BcSign.Minus;
            if (LongInteger.IsZero(ref result))
                result.Sign = BcSign.Plus;

            LongInteger.RemoveLeadingZeros(ref result);

            return true;
        }

        private static void Pop(out Bc result)
        {
            var len = (byte)Stack.Pop() - 1;
            result = new(4 * len)
            {
                Sign = Stack.Pop() == 0 ? BcSign.Plus : BcSign.Minus
            };

            var i = 0;
            while (len-- > 0)
            {
                var w = Stack.Pop();
                result.Value[i] = (byte)(w & 0xF);
                w >>= 4;
                result.Value[i + 1] = (byte)(w & 0xF);
                w >>= 4;
                result.Value[i + 2] = (byte)(w & 0xF);
                w >>= 4;
                result.Value[i + 3] = (byte)(w & 0xF);
                i += 4;
            }

            LongInteger.RemoveLeadingZeros(ref result);
        }

        private static void Push(ref Bc num)
        {
            var len = 0;
            var i = num.Value.Length - 1;
            while (i >= 0)
            {
                ushort w = 0;
                for (var x = 0; x < 4; x++)
                {
                    w <<= 4;
                    if (i >= 0)
                        w += num.Value[i--];
                }
                Stack.Push(w);
                len++;
            }

            Stack.Push((ushort)(num.Sign == BcSign.Minus ? 0xFF : 0));
            Stack.Push((ushort)(len + 1));
        }

        public static void Process(ushort entryPoint)
        {
            var op = (DecimalOps)Stack.Pop();

            switch (op)
            {
                case DecimalOps.Adjust:
                {
                    var newLen = Stack.Pop();
                    var len = Stack.Pop() & 0xFF;
                    var sign = Stack.Pop();
                    if (len < newLen)
                        while (len++ < newLen)
                            Stack.Push(0);
                    else
                        while (len-- > newLen)
                            Stack.Pop();
                    Stack.Push(sign);
                }
                    break;
                case DecimalOps.Add:
                {
                    LongInteger.Pop(out var arg2);
                    LongInteger.Pop(out var arg1);
                    LongInteger.Add(ref arg1, ref arg2, out var result);
                    LongInteger.Push(ref result);
                }
                    break;
                case DecimalOps.Subtract:
                {
                    LongInteger.Pop(out var arg2);
                    LongInteger.Pop(out var arg1);
                    LongInteger.Subtract(ref arg1, ref arg2, out var result);
                    LongInteger.Push(ref result);
                }
                    break;
                case DecimalOps.Negate:
                {
                    var len = Stack.Pop();
                    var sign = Stack.Pop() == 0 ? 1 : 0;
                    Stack.Push((ushort)(sign != 0 ? 0xFF : 0));
                    Stack.Push(len);
                }
                    break;
                case DecimalOps.Multiply:
                {
                    LongInteger.Pop(out var arg2);
                    LongInteger.Pop(out var arg1);
                    LongInteger.Multiply(ref arg1, ref arg2, out var result);
                    LongInteger.Push(ref result);
                }
                    break;
                case DecimalOps.Divide:
                {
                    LongInteger.Pop(out var arg2);
                    LongInteger.Pop(out var arg1);
                    if (!LongInteger.Divide(ref arg1, ref arg2, out var result))
                        throw new ExecutionException(ExecutionErrorCode.DivideByZero);
                    LongInteger.Push(ref result);
                }
                    break;
                case DecimalOps.Str:
                {
                    Stack.Pop();
                    var strAddress = Stack.Pop();
                    var intLen = Stack.Pop();
                    var sign = Stack.Pop();

                    short idx = 0;

                    if (sign != 0)
                        Memory.WriteByte(strAddress, ++idx, (byte)'-');

                    var suppress = true;

                    while (intLen-- > 0)
                    {
                        var w = Stack.Pop();
                        for (var i = 0; i < 4; i++)
                        {
                            var digit = (byte)(w >> 4 * i & 0xF);
                            if (suppress && digit <= 0)
                                continue;
                            Memory.WriteByte(strAddress, ++idx, (byte)(digit + (byte)'0'));
                            suppress = false;
                        }
                    }
                    if (suppress)
                        Memory.WriteByte(strAddress, ++idx, (byte)'0');

                    Memory.WriteByte(strAddress, 0, (byte)idx);
                }
                    break;
                case DecimalOps.Compare:
                {
                    var cmp = Stack.Pop();

                    LongInteger.Pop(out var arg2);
                    LongInteger.Pop(out var arg1);
                    var result = LongInteger.Compare(ref arg1, ref arg2);
                    switch (cmp)
                    {
                        case 8: // <
                            Stack.Push(PSystem.Boolean(result < 0));
                            break;
                        case 9: // <=
                            Stack.Push(PSystem.Boolean(result <= 0));
                            break;
                        case 10: // >=
                            Stack.Push(PSystem.Boolean(result >= 0));
                            break;
                        case 11: // >
                            Stack.Push(PSystem.Boolean(result > 0));
                            break;
                        case 12: // !=
                            Stack.Push(PSystem.Boolean(result != 0));
                            break;
                        case 13: // ==
                            Stack.Push(PSystem.Boolean(result == 0));
                            break;
                        default:
                            throw new ExecutionException(ExecutionErrorCode.UnimplementedInstruction);
                    }
                }
                    break;
                case DecimalOps.ConvertTos:
                {
                    var val = Stack.PopInteger();
                    var sign = val < 0;
                    if (!sign)
                        val = (short)-val;

                    for (var i = 0; i < 2; i++)
                    {
                        ushort w = 0;
                        for (var j = 0; j < 4; j++)
                        {
                            w = (ushort)((w << 4) - val % 10);
                            val /= 10;
                        }
                        Stack.Push(w);
                    }
                    Stack.Push((ushort)(sign ? 0xFF : 0));
                    Stack.Push(3);
                }
                    break;
                case DecimalOps.Truncate:
                {
                    LongInteger.Pop(out var arg1);

                    var result = arg1.Value.Aggregate<byte, short>(0, (current, t) => (short)(current * 10 - t));

                    if (arg1.Sign == BcSign.Minus)
                        Stack.Push(result);
                    else
                        Stack.Push((short)-result);
                }
                    break;
                //case DecimalOps.ConvertTosM1:
                default:
                    throw new ExecutionException(ExecutionErrorCode.UnimplementedInstruction);
            }
        }

        private enum BcSign
        {
            Plus,
            Minus
        }

        private struct Bc
        {
            public BcSign Sign;
            public byte[] Value;

            public Bc(int length)
            {
                this.Sign = BcSign.Plus;
                this.Value = new byte[length];
            }
        }

        private enum DecimalOps : ushort
        {
            Adjust = 0,
            Add = 2,
            Subtract = 4,
            Negate = 6,
            Multiply = 8,
            Divide = 10,
            Str = 12,

            //ConvertTosM1 = 14,
            Compare = 16,
            ConvertTos = 18,
            Truncate = 20
        }
    }
}