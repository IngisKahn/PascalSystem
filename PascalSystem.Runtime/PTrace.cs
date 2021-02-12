namespace PascalSystem.Runtime
{
    using System.Text;

    public static class PTrace
    {
        private static readonly string[] instructions =
        {
            // ReSharper disable StringLiteralTypo
            "sldc 0",
            "sldc 1",
            "sldc 2",
            "sldc 3",
            "sldc 4",
            "sldc 5",
            "sldc 6",
            "sldc 7",
            "sldc 8",
            "sldc 9",
            "sldc 10",
            "sldc 11",
            "sldc 12",
            "sldc 13",
            "sldc 14",
            "sldc 15",
            "sldc 16",
            "sldc 17",
            "sldc 18",
            "sldc 19",
            "sldc 20",
            "sldc 21",
            "sldc 22",
            "sldc 23",
            "sldc 24",
            "sldc 25",
            "sldc 26",
            "sldc 27",
            "sldc 28",
            "sldc 29",
            "sldc 30",
            "sldc 31",
            "sldc 32",
            "sldc 33",
            "sldc 34",
            "sldc 35",
            "sldc 36",
            "sldc 37",
            "sldc 38",
            "sldc 39",
            "sldc 40",
            "sldc 41",
            "sldc 42",
            "sldc 43",
            "sldc 44",
            "sldc 45",
            "sldc 46",
            "sldc 47",
            "sldc 48",
            "sldc 49",
            "sldc 50",
            "sldc 51",
            "sldc 52",
            "sldc 53",
            "sldc 54",
            "sldc 55",
            "sldc 56",
            "sldc 57",
            "sldc 58",
            "sldc 59",
            "sldc 60",
            "sldc 61",
            "sldc 62",
            "sldc 63",
            "sldc 64",
            "sldc 65",
            "sldc 66",
            "sldc 67",
            "sldc 68",
            "sldc 69",
            "sldc 70",
            "sldc 71",
            "sldc 72",
            "sldc 73",
            "sldc 74",
            "sldc 75",
            "sldc 76",
            "sldc 77",
            "sldc 78",
            "sldc 79",
            "sldc 80",
            "sldc 81",
            "sldc 82",
            "sldc 83",
            "sldc 84",
            "sldc 85",
            "sldc 86",
            "sldc 87",
            "sldc 88",
            "sldc 89",
            "sldc 90",
            "sldc 91",
            "sldc 92",
            "sldc 93",
            "sldc 94",
            "sldc 95",
            "sldc 96",
            "sldc 97",
            "sldc 98",
            "sldc 99",
            "sldc 100",
            "sldc 101",
            "sldc 102",
            "sldc 103",
            "sldc 104",
            "sldc 105",
            "sldc 106",
            "sldc 107",
            "sldc 108",
            "sldc 109",
            "sldc 110",
            "sldc 111",
            "sldc 112",
            "sldc 113",
            "sldc 114",
            "sldc 115",
            "sldc 116",
            "sldc 117",
            "sldc 118",
            "sldc 119",
            "sldc 120",
            "sldc 121",
            "sldc 122",
            "sldc 123",
            "sldc 124",
            "sldc 125",
            "sldc 126",
            "sldc 127",
            "abi          (absolute value integer)",
            "abr          (absolute value real)",
            "adi          (add integer)",
            "adr          (add real)",
            "land         (logical and)",
            "dif          (set difference)",
            "dvi          (divide integer)",
            "dvr          (divide real)",
            "chk          (check)",
            "flo          (float tos-1)",
            "flt          (float tos)",
            "inn          (set memberschip)",
            "int          (set intersection)",
            "lor          (logical or)",
            "modi         (modulo integer)",
            "mpi          (multiply integer)",
            "mpr          (multiply real)",
            "ngi          (negate integer)",
            "ngr          (negate real)",
            "lnot         (logical not)",
            "srs          (build subrange set)",
            "sbi          (substract integer)",
            "sbr          (substract real)",
            "sgs          (build singleton set)",
            "sqi          (square integer)",
            "sqr          (square real)",
            "sto          (store indirect word)",
            "ixs          (index string array)",
            "uni          (set union)",
            "lde  U,B     (load extended word)",
            "csp  Q       (call standard procedure)",
            "ldcn         (load constant nil)",
            "adj  U       (adjust set)",
            "fjp  J       (false jump)",
            "inc  B       (increment field pointer)",
            "ind  B       (load indirect word)",
            "ixa  B       (index array)",
            "lao  B       (load global address)",
            "lsa  Y       (load string address)",
            "lae  U,B     (load extended address)",
            "mov  B       (move words)",
            "ldo  B       (load global word)",
            "sas  U P     (string assign)",
            "sro  B       (store global word)",
            "xjp  R       (case jump)",
            "rnp  D       (return non-base procedure)",
            "cip  C       (call intermediate procedure)",
            "equ  T       (equal)",
            "geq  T       (greater or equal)",
            "grt  T       (greater)",
            "lda  D,B     (load intermediate address)",
            "ldc  X       (load multiple word constant)",
            "leq  T       (less or equal)",
            "les  T       (less than)",
            "lod  D,B     (load intermediate word)",
            "neq  T       (not equal)",
            "str  D,B     (store intermediate word)",
            "ujp  J       (unconditional jump)",
            "ldp          (load packed field)",
            "stp          (store into packed field)",
            "ldm  U       (load multiple words)",
            "stm  U       (store multiple words)",
            "ldb          (load byte)",
            "stb          (store byte)",
            "ixp  U,U     (index packed array)",
            "rbp  D       (return base procedure)",
            "cbp  C       (call base procedure)",
            "equi         (equal integer)",
            "geqi         (greater or equal integer)",
            "grti         (greater iteger)",
            "lla  B       (load local address)",
            "ldci W       (load constant integer)",
            "leqi         (less or equal integer)",
            "lesi         (less than integer)",
            "ldl  B       (load local word)",
            "neqi         (not equal integer)",
            "stl  B       (store local word)",
            "cxp  A       (call external procedure)",
            "clp  C       (call local procedure)",
            "cgp  C       (call global procedure)",
            "lpa  Z       (load packed array)",
            "ste  U,B     (store extended word)",
            ".db  210",
            "efj  J       (equal false jump)",
            "nfj  J       (not equal false jump)",
            "bpt  B       (breakpoint)",
            "xit          (exit operating system)",
            "nop          (no operation)",
            "sldl 1       (short load local word)",
            "sldl 2       (short load local word)",
            "sldl 3       (short load local word)",
            "sldl 4       (short load local word)",
            "sldl 5       (short load local word)",
            "sldl 6       (short load local word)",
            "sldl 7       (short load local word)",
            "sldl 8       (short load local word)",
            "sldl 9       (short load local word)",
            "sldl 10      (short load local word)",
            "sldl 11      (short load local word)",
            "sldl 12      (short load local word)",
            "sldl 13      (short load local word)",
            "sldl 14      (short load local word)",
            "sldl 15      (short load local word)",
            "sldl 16      (short load local word)",
            "sldo 1       (short load global word)",
            "sldo 2       (short load global word)",
            "sldo 3       (short load global word)",
            "sldo 4       (short load global word)",
            "sldo 5       (short load global word)",
            "sldo 6       (short load global word)",
            "sldo 7       (short load global word)",
            "sldo 8       (short load global word)",
            "sldo 9       (short load global word)",
            "sldo 10      (short load global word)",
            "sldo 11      (short load global word)",
            "sldo 12      (short load global word)",
            "sldo 13      (short load global word)",
            "sldo 14      (short load global word)",
            "sldo 15      (short load global word)",
            "sldo 16      (short load global word)",
            "sind 0       (short load indirect word)",
            "sind 1       (short load indirect word)",
            "sind 2       (short load indirect word)",
            "sind 3       (short load indirect word)",
            "sind 4       (short load indirect word)",
            "sind 5       (short load indirect word)",
            "sind 6       (short load indirect word)",
            "sind 7       (short load indirect word)"
            // ReSharper enable StringLiteralTypo
        };

        private static string PString(ushort address)
        {
            var len = Memory.ReadByte(address, 0);
            StringBuilder sb = new(len + 2);
            sb.Append('\'');
            for (short i = 1; i <= len; i++)
                sb.Append(Memory.ReadByte(address, i));
            sb.Append('\'');
            return sb.ToString();
        }

        private static ushort ReadStack(ushort stackPointer, int topOfStackOffset) => (ushort)(
            Memory.ReadByte(stackPointer, (short)(2 * topOfStackOffset)) +
            (Memory.ReadByte(stackPointer, (short)(2 * topOfStackOffset + 1)) << 8));


        private static string ProcName(int seg, int proc, ushort sp)
        {
            if (seg != 0)
                return string.Empty;
            StringBuilder sb = new();
            switch (proc)
            {
                case 5:
                    sb.Append("Rewrite");
                    if (sp == 0)
                        break;
                    sb.Append('(');
                    sb.Append(PTrace.PString(PTrace.ReadStack(sp, 2)));
                    sb.Append(')');
                    break;
                case 6:
                    sb.Append("Close");
                    break;
                case 7:
                    sb.Append("Get");
                    break;
                case 8:
                    sb.Append("Put");
                    break;
                case 10:
                    sb.Append("Eof");
                    break;
                case 11:
                    sb.Append("Eoln");
                    break;
                case 12:
                    sb.Append("ReadInteger");
                    break;
                case 13:
                    sb.Append("WriteInteger");
                    break;
                case 16:
                    sb.Append("ReadChar");
                    break;
                case 17:
                    sb.Append("WriteChar");
                    break;
                case 18:
                    sb.Append("ReadString");
                    break;
                case 19:
                    sb.Append("WriteString");
                    if (sp == 0)
                        break;
                    sb.Append('(');
                    sb.Append(PTrace.PString(PTrace.ReadStack(sp, 1)));
                    sb.Append(')');
                    break;
                case 21:
                    sb.Append("ReadLn");
                    break;
                case 22:
                    sb.Append("WriteLn");
                    break;
                case 23:
                    sb.Append("Concat");
                    if (sp == 0)
                        break;
                    sb.Append('(');
                    sb.Append(PTrace.PString(PTrace.ReadStack(sp, 2)));
                    sb.Append(',');
                    sb.Append(PTrace.PString(PTrace.ReadStack(sp, 1)));
                    sb.Append(')');
                    break;
                case 24:
                    sb.Append("Insert");
                    if (sp == 0)
                        break;
                    sb.Append('(');
                    sb.Append(PTrace.PString(PTrace.ReadStack(sp, 3)));
                    sb.Append(',');
                    sb.Append(PTrace.PString(PTrace.ReadStack(sp, 2)));
                    sb.AppendFormat(", {0})", PTrace.ReadStack(sp, 0));
                    break;
                case 25:
                    sb.Append("Copy");
                    if (sp == 0)
                        break;
                    sb.Append('(');
                    sb.Append(PTrace.PString(PTrace.ReadStack(sp, 3)));
                    sb.AppendFormat(", {0}, {1})", PTrace.ReadStack(sp, 1), PTrace.ReadStack(sp, 0));
                    break;
                case 26:
                    sb.Append("Delete");
                    if (sp == 0)
                        break;
                    sb.Append('(');
                    sb.Append(PTrace.PString(PTrace.ReadStack(sp, 2)));
                    sb.AppendFormat(", {0}, {1})", PTrace.ReadStack(sp, 1), PTrace.ReadStack(sp, 0));
                    break;
                case 27:
                    sb.Append("Pos");
                    if (sp == 0)
                        break;
                    sb.Append('(');
                    sb.Append(PTrace.PString(PTrace.ReadStack(sp, 3)));
                    sb.Append(',');
                    sb.Append(PTrace.PString(PTrace.ReadStack(sp, 2)));
                    sb.Append(')');
                    break;
                case 28:
                    sb.Append("BlockRead/BlockWrite");
                    break;
                case 29:
                    sb.Append("GotoXY");
                    if (sp != 0)
                        sb.AppendFormat("( {0}, {1})", PTrace.ReadStack(sp, 1), PTrace.ReadStack(sp, 0));
                    break;
            }
            return sb.ToString();
        }

        /// <summary>
        ///     The DisasmP function is used to disassemble a p-code opcode from
        ///     memory, into the given buffer.
        /// </summary>
        /// <param name="buffer">The buffer to print the opcode into.</param>
        /// <param name="segNo">The segment number the code is within</param>
        /// <param name="ipcBase">The procedure base address (enter_ic).</param>
        /// <param name="ipc">
        ///     The instruction counter (address within segment) of the
        ///     instruction to disassemble.
        /// </param>
        /// <param name="jTab">
        ///     The jump table (procedure attributes) of the procedure being
        ///     executed, or disassembled.
        /// </param>
        /// <param name="sp">Stack pointer (?)</param>
        /// <returns></returns>
        public static ushort DisasmP(StringBuilder buffer, ushort segNo, ushort ipcBase, ushort ipc, ushort jTab,
            ushort sp)
        {
            var opCode = Memory.ReadByte(ipcBase, (short)ipc++);
            var s = PTrace.instructions[opCode];
            int val;

            foreach (var ch in s)
                switch (ch)
                {
                    case 'A':
                        // CXP Arguments 
                    {
                        int seg = Memory.ReadByte(ipcBase, (short)ipc++);
                        int proc = Memory.ReadByte(ipcBase, (short)ipc++);
                        buffer.AppendFormat("{0},{1} ", seg, proc);
                        buffer.Append(PTrace.ProcName(seg, proc, sp));
                    }
                        break;
                    case 'B':
                        val = Memory.ReadByte(ipcBase, (short)ipc++);
                        if ((val & 0x80) != 0)
                            val = ((val & 0x7f) << 8) + Memory.ReadByte(ipcBase, (short)ipc++);
                        buffer.Append(val);
                        break;
                    case 'C':
                        val = Memory.ReadByte(ipcBase, (short)ipc++);
                        buffer.AppendFormat("{0} ", val);
                        buffer.Append(PTrace.ProcName(segNo, val, sp));
                        break;
                    case 'D':
                    case 'U':
                        val = Memory.ReadByte(ipcBase, (short)ipc++);
                        buffer.Append(val);
                        break;
                    case 'P':
                        if (sp != 0)
                            buffer.Append(PTrace.PString(PTrace.ReadStack(sp, 0)));
                        break;
                    case 'S':
                        val = Memory.ReadByte(ipcBase, (short)ipc++);
                        if ((val & 0x80) != 0)
                            val = -(0x100 - val);
                        buffer.Append(val);
                        break;
                    case 'T':
                        val = Memory.ReadByte(ipcBase, (short)ipc++);
                        switch (val)
                        {
                            case 2:
                                buffer.Append("real");
                                break;
                            case 4:
                                buffer.Append("string");
                                break;
                            case 6:
                                buffer.Append("boolean");
                                break;
                            case 8:
                                buffer.Append("set");
                                break;
                            case 10:
                                val = Memory.ReadByte(ipcBase, (short)ipc++);
                                if ((val & 0x80) != 0)
                                    val = ((val & 0x7f) << 8) + Memory.ReadByte(ipcBase, (short)ipc++);
                                buffer.AppendFormat("byte array, {0} bytes", val);
                                break;
                            case 12:
                                val = Memory.ReadByte(ipcBase, (short)ipc++);
                                if ((val & 0x80) != 0)
                                    val = ((val & 0x7f) << 8) + Memory.ReadByte(ipcBase, (short)ipc++);
                                buffer.AppendFormat("{0} words", val);
                                break;
                            default:
                                buffer.Append(val);
                                break;
                        }
                        break;
                    case 'W':
                        val = Memory.ReadByte(ipcBase, (short)ipc++);
                        val |= Memory.ReadByte(ipcBase, (short)ipc++) << 8;
                        buffer.Append(val);
                        break;
                    case 'R': // case arguments 
                    {
                        ipc = (ushort)(ipc + 1 & ~1);
                        int min = Memory.ReadByte(ipcBase, (short)ipc++);
                        min |= Memory.ReadByte(ipcBase, (short)ipc++) << 8;
                        int max = Memory.ReadByte(ipcBase, (short)ipc++);
                        max |= Memory.ReadByte(ipcBase, (short)ipc++) << 8;
                        ipc++;
                        int defaultVal = Memory.ReadByte(ipcBase, (short)ipc++);
                        if ((defaultVal & 0x80) != 0) // less than zero?
                        {
                            defaultVal = -(0x100 - defaultVal);
                            defaultVal = -defaultVal;
                            defaultVal = Memory.ReadByte(jTab, -2) +
                                         (Memory.ReadByte(jTab, -1) << 8) + 2 -
                                         (Memory.ReadByte(jTab, (short)-defaultVal) +
                                          (Memory.ReadByte(jTab, (short)(-defaultVal + 1)) << 8) + defaultVal);
                        }
                        else
                            defaultVal = ipc + defaultVal;
                        buffer.AppendFormat("{0},{1},{2}  ", min, max, defaultVal);

                        while (min < max + 1)
                        {
                            val = Memory.ReadByte(ipcBase, (short)ipc++);
                            val |= Memory.ReadByte(ipcBase, (short)ipc++) << 8;
                            buffer.AppendFormat(",{0}", ipc - 2 - val);

                            min++;
                        }
                    }
                        break;
                    case 'Q':
                        val = Memory.ReadByte(ipcBase, (short)ipc++);
                        switch (val)
                        {
                            case 1:
                                buffer.Append("new");
                                break;
                            case 2:
                                buffer.Append("Moveleft");
                                break;
                            case 3:
                                buffer.Append("Moveright");
                                break;
                            case 4:
                                buffer.Append("exit");
                                break;
                            case 5:
                                buffer.Append("unitread");
                                break;
                            case 6:
                                buffer.Append("unitwrite");
                                break;
                            case 7:
                                buffer.Append("idsearch");
                                break;
                            case 8:
                                buffer.Append("treesearch");
                                break;
                            case 9:
                                buffer.Append("time");
                                break;
                            case 10:
                                buffer.Append("fillchar");
                                break;
                            case 11:
                                buffer.AppendFormat("scan");
                                break;
                            case 12:
                                buffer.Append("unitstat");
                                break;
                            case 21:
                                buffer.Append("load_segment");
                                break;
                            case 22:
                                buffer.Append("unload_segment");
                                break;
                            case 32:
                                buffer.Append("mark");
                                break;
                            case 33:
                                buffer.Append("release");
                                break;
                            case 34:
                                buffer.Append("ioresult");
                                break;
                            case 35:
                                buffer.Append("unitbusy");
                                break;
                            case 37:
                                buffer.Append("unitwait");
                                break;
                            case 38:
                                buffer.Append("unitclear");
                                break;
                            case 39:
                                buffer.Append("halt");
                                break;
                            case 40:
                                buffer.Append("memavail");
                                break;
                            default:
                                buffer.Append(val);
                                break;
                        }
                        break;
                    case 'J':
                        val = Memory.ReadByte(ipcBase, (short)ipc++);
                        if ((val & 0x80) != 0) /* less than zero? */
                        {
                            val = -(0x100 - val);
                            val = -val;

                            val = Memory.ReadByte(jTab, -2) +
                                  (Memory.ReadByte(jTab, -1) << 8) + 2 -
                                  (Memory.ReadByte(jTab, (short)-val) +
                                   (Memory.ReadByte(jTab, (short)(-val + 1)) << 8) + val);
                        }
                        else
                            val = ipc + val;
                        buffer.Append(val);
                        break;
                    case 'V':
                        break;
                    case 'X':
                        val = Memory.ReadByte(ipcBase, (short)ipc++);
                        buffer.Append(val);

                        ipc = (ushort)(ipc + 1 & ~1);
                        while (val-- != 0)
                        {
                            var w = Memory.ReadByte(ipcBase, (short)ipc) + (Memory.ReadByte(ipcBase,
                                (short)(ipc + 1)) << 8);
                            ipc += 2;
                            buffer.AppendFormat(",{0:X4}", w);
                        }
                        break;
                    case 'Y':
                        val = Memory.ReadByte(ipcBase, (short)ipc++);
                        buffer.AppendFormat("{0},'", val);

                        while (val-- != 0)
                            buffer.Append((char)Memory.ReadByte(ipcBase, (short)ipc++));
                        buffer.Append('\'');
                        break;

                    case 'Z':
                        val = Memory.ReadByte(ipcBase, (short)ipc++);
                        buffer.Append(val);

                        ipc = (ushort)(ipc + 1 & ~1);
                        while (val-- != 0)
                        {
                            buffer.AppendFormat(",{0:X2}", Memory.ReadByte(ipcBase, (short)ipc));
                            ipc++;
                        }
                        break;
                    default:
                        buffer.Append(ch);
                        break;
                }
            return ipc;
        }

        /// <summary>
        ///     The PrintStack function is used to print a representation of the
        ///     value stack into the buffer provided.
        /// </summary>
        /// <param name="sp">The top-of-stack pointer.</param>
        /// <returns></returns>
        public static string PrintStack(ushort sp)
        {
            StringBuilder sb = new();
            while (sp < 0x200)
            {
                sb.AppendFormat(" {0:X4}", Memory.Read(sp));
                sp += 2;
            }
            return sb.ToString();
        }

        /// <summary>
        ///     The PrintStaticChain function is used to print a representation of
        ///     the procedure call chain into the buffer provided.
        /// </summary>
        /// <param name="mp">The value of the MP register, the top of the call stack.</param>
        /// <returns></returns>
        public static string PrintStaticChain(ushort mp)
        {
            StringBuilder sb = new();
            for (ushort nextMp = 0xFFFF; nextMp != 0 && mp != nextMp; mp = nextMp)
                sb.AppendFormat(" {0:X4}", mp);
            return sb.ToString();
        }
    }
}