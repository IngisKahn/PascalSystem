namespace PascalSystem.Runtime
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;

    internal static class Search
    {
        private static readonly Dictionary<string, Id> idTable = new()
        {
            ["AND"] = new(Symbol.Multiply, Operator.And),
            ["ARRAY"] = new(Symbol.Array, Operator.None),
            ["BEGIN"] = new(Symbol.Begin, Operator.None),
            ["CASE"] = new(Symbol.Case, Operator.None),
            ["CONST"] = new(Symbol.Const, Operator.None),
            ["DIV"] = new(Symbol.Multiply, Operator.IntegerDivide),
            ["DO"] = new(Symbol.Do, Operator.None),
            ["DOWNTO"] = new(Symbol.DownTo, Operator.None),
            ["ELSE"] = new(Symbol.Else, Operator.None),
            ["END"] = new(Symbol.End, Operator.None),
            ["EXTERNAL"] = new(Symbol.External, Operator.None),
            ["FILE"] = new(Symbol.File, Operator.None),
            ["FOR"] = new(Symbol.For, Operator.None),
            ["FORWARD"] = new(Symbol.Forward, Operator.None),
            ["FUNCTION"] = new(Symbol.Function, Operator.None),
            ["GOTO"] = new(Symbol.Goto, Operator.None),
            ["IF"] = new(Symbol.If, Operator.None),
            ["IMPLEMENT"] = new(Symbol.Implement, Operator.None),
            ["IN"] = new(Symbol.Rel, Operator.In),
            ["INTERFACE"] = new(Symbol.Interface, Operator.None),
            ["LABEL"] = new(Symbol.Label, Operator.None),
            ["MOD"] = new(Symbol.Multiply, Operator.IntegerModulo),
            ["NOT"] = new(Symbol.Not, Operator.None),
            ["OF"] = new(Symbol.Of, Operator.None),
            ["OR"] = new(Symbol.Add, Operator.Or),
            ["OTHERWISE"] = new(Symbol.Otherwise, Operator.None),
            ["PACKED"] = new(Symbol.Packed, Operator.None),
            ["PROCEDURE"] = new(Symbol.Procedure, Operator.None),
            ["PROGRAM"] = new(Symbol.Program, Operator.None),
            ["RECORD"] = new(Symbol.Record, Operator.None),
            ["REPEAT"] = new(Symbol.Repeat, Operator.None),
            ["SEGMENT"] = new(Symbol.Program, Operator.None),
            ["SEPARATE"] = new(Symbol.Separate, Operator.None),
            ["SET"] = new(Symbol.Set, Operator.None),
            ["THEN"] = new(Symbol.Then, Operator.None),
            ["TO"] = new(Symbol.To, Operator.None),
            ["TYPE"] = new(Symbol.Type, Operator.None),
            ["UNIT"] = new(Symbol.Unit, Operator.None),
            ["UNTIL"] = new(Symbol.Until, Operator.None),
            ["USES"] = new(Symbol.Uses, Operator.None),
            ["VAR"] = new(Symbol.Var, Operator.None),
            ["WHILE"] = new(Symbol.While, Operator.None),
            ["WITH"] = new(Symbol.With, Operator.None)
        };


        public static void CspIdSearch(ushort bufPtr, ushort arg2Ptr)
        {
            var bufOffset = Memory.Read(arg2Ptr);
            StringBuilder tokenBuf = new();

            for (; ; bufOffset++)
            {
                var ch = Memory.ReadByte(bufPtr, (short)bufOffset);
                if (!char.IsLetterOrDigit((char)ch))
                    break;
                tokenBuf.Append(ch);
            }

            // Offset Correct, this team book failed the test.
            bufOffset--;

            Memory.Write(arg2Ptr, bufOffset);

            Debug.Write(tokenBuf);

            if (Search.idTable.TryGetValue(tokenBuf.ToString(), out var id))
            {
                Debug.WriteLine(": found, Sym={0} Op={1}", id.Symbol, id.Operator);

                Memory.Write(arg2Ptr.Index(1), (ushort)id.Symbol);
                Memory.Write(arg2Ptr.Index(2), (ushort)id.Operator);
                return;
            }

            Memory.Write(arg2Ptr.Index(1), (ushort)Symbol.Identifier);
            Memory.Write(arg2Ptr.Index(2), (ushort)Operator.None);
            short index = 0;
            foreach (var c in tokenBuf.ToString())
                Memory.WriteByte(arg2Ptr.Index(3), index++, (byte)c);

            Debug.WriteLine(": not found");
        }


        public static ushort CspTreeSearch(ushort tokenBuf, ushort resultPtr, ushort nodePtr)
        {
#if DEBUG
            for (short i = 0; ; i++)
            {
                var c = (char)Memory.ReadByte(tokenBuf, i);
                if (!char.IsLetterOrDigit(c))
                    break;
                Debug.Write(c);
            }
            Debug.Write(": ");
#endif

            for (; ; )
            {
                var found = false;
                for (ushort i = 0; ; i++)
                {
                    var ch1 = Memory.ReadByte(tokenBuf, (short)i); /* Get a char from both strings */
                    var ch2 = Memory.ReadByte(nodePtr, (short)i);
                    if (ch1 < ch2)
                    {
                        var link = Memory.Read(nodePtr.Index(5));
                        if (link == 0)
                        {
                            Debug.WriteLine("not found, should be on right node");

                            Memory.Write(resultPtr, nodePtr);
                            return 0xffff;
                        }

                        nodePtr = link; /* follow RightLink                 */
                        found = true;
                        break;
                    }
                    if (ch1 > ch2)
                    {
                        var link = Memory.Read(nodePtr.Index(4));
                        if (link == 0)
                        {
                            Debug.WriteLine("not found, should be on left node");

                            Memory.Write(resultPtr, nodePtr);
                            return 0xffff;
                        }

                        nodePtr = link; /* follow RightLink                 */
                        found = true;
                        break;
                    }
                    if (!char.IsLetterOrDigit((char)ch1))
                        break;
                }
                if (!found)
                    continue;
                Debug.WriteLine("found");

                Memory.Write(resultPtr, nodePtr);
                return 0;
            }
        }

        private enum Symbol
        {
            Identifier,

            //Comma,
            //Colon,
            //Semicolon,
            //LeftParenthesis,
            //RightParenthesis,
            Do,
            To,
            DownTo,
            End,
            Until,
            Of,
            Then,
            Else,

            //Becomes,
            //LeftBracket,
            //RightBracket,
            //Arrow,
            //Period,
            Begin,
            If,
            Case,
            Repeat,
            While,
            For,
            With,
            Goto,
            Label,
            Const,
            Type,
            Var,
            Procedure,
            Function,
            Program,
            Forward,

            //IntConst,
            //RealConst,
            //StringConst,
            Not,
            Multiply,
            Add,
            Rel,
            Set,
            Packed,
            Array,
            Record,
            File,

            //Other,
            //LongConst,
            Uses,
            Unit,
            Interface,
            Implement,
            External,
            Separate,
            Otherwise
        }

        private enum Operator
        {
            //Multiply,
            //RealDivide,
            And,
            IntegerDivide,
            IntegerModulo,

            //Plus,
            //Minus,
            Or,

            //LessThan,
            //LessOrEqual,
            //GreaterOrEqual,
            //GreaterThan,
            //NotEqual,
            //Equal,
            In,
            None
        }

        private readonly struct Id
        {
            public readonly Symbol Symbol;
            public readonly Operator Operator;

            public Id(Symbol symbol, Operator @operator)
            {
                this.Symbol = symbol;
                this.Operator = @operator;
            }
        }
    }
}