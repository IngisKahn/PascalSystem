namespace PascalSystem.Runtime
{
    using System;
    using System.CodeDom.Compiler;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;

    internal static class VirtualMachine
    {
        private const float machineEpsilonFloat = 1.1920929e-7f;
        //private const double machineEpsilonDouble = 2.2204460492503131e-16;

        /*
                private static TextWriter traceWriter;
        */
        private static byte traceSeg;
        private static byte traceProc;

        private enum MethodStack
        {
            ProgramStackPointer = -1,
            StaticLinks,
            Dyn,
            JumpTable,
            SegmentId,
            InterpreterProgramCounter,
            StackPointer,

            /// <summary>
            ///     Var-Offset counts from 1..
            /// </summary>
            Variables = MethodStack.StackPointer,
            FrameSize
        }

        private const ushort heapBottom = 0x200;
        private const ushort kpTop = 0xFE80;
        private const ushort systemCommuincationSize = 170;
        private const ushort spTop = 0x200;

        // Official P-Machine registers
        public static ushort NewPointer, ProgramStackPointer;

        public static ushort SystemCommunicationPointer { get; private set; }
        public static ushort StackPointer { get; set; }
        public static ushort MarkStackPointer, Base;
        public static ushort InterpreterProgramCounter { get; set; }
        public static ushort InterpreterProgramCounterBase, Segment, JumpTable;


#if !WORD_MEMORY
        private static int appleCompatibility = 0;
#endif

        private static ushort currentIpc, baseMp;
        private static uint level;
        private static uint traceLevel;

        private const int segmentDictionarySize = 32;

        public struct SegmentInfo
        {
            public int UseCount;

            public ushort OldKp;

            //public ushort Id;
            public ushort SegmentPointer;

            public ushort SegBase;
            public string Name;
        }

        public static readonly SegmentInfo[] SegmentDictionary = new SegmentInfo[VirtualMachine.segmentDictionarySize];
        
        private static void Warning(string format, params object[] parameters) => Console.Error.WriteLine("warning: " + format, parameters);

        private static void DumpCore()
        {
            var fileName = Assembly.GetEntryAssembly()?.FullName + ".core";
            try
            {
                using StreamWriter writer = new(fileName);
                Memory.Dump(writer, 0, 0xFFFF);
            }
            catch (Exception ex)
            {
                VirtualMachine.Warning("DumpCore: unable to create core dump {0}: {1}", fileName, ex);
            }
        }
        
        public static void Panic(string format, params object[] parameters)
        {
            //TermClose();
            Console.Error.WriteLine("panic: " + format, parameters);
            VirtualMachine.DumpCore();
            Environment.Exit(1);
        }

        private static void MoveLeft(ushort dest, int destOffset, ushort src, int srcOffset, int len)
        {
            while (len-- > 0)
                Memory.WriteByte(dest, (short)destOffset++, Memory.ReadByte(src, (short)srcOffset++));
        }

        private static void MoveRight(ushort dest, short destOffset, ushort src, short srcOffset, short len)
        {
            srcOffset += len;
            destOffset += len;
            while (len-- > 0)
                Memory.WriteByte(dest, --destOffset, Memory.ReadByte(src, --srcOffset));
        }

        public static sbyte FetchSignedByte() => (sbyte)Memory.ReadByte(VirtualMachine.InterpreterProgramCounterBase,
            (short)VirtualMachine.InterpreterProgramCounter++);

        public static ushort FetchWord()
        {
            var w = (ushort)Memory.ReadByte(VirtualMachine.InterpreterProgramCounterBase,
                (short)VirtualMachine.InterpreterProgramCounter++);
            w += (ushort)(Memory.ReadByte(VirtualMachine.InterpreterProgramCounterBase,
                               (short)VirtualMachine.InterpreterProgramCounter++) << 8);
            return w;
        }

        public static byte FetchByte() => Memory.ReadByte(VirtualMachine.InterpreterProgramCounterBase,
            (short)VirtualMachine.InterpreterProgramCounter++);

        /// <summary>
        ///     Returns the number of procedures of a segment.
        /// </summary>
        /// <param name="segment"></param>
        /// <returns></returns>
        private static byte GetSegmentProcedureCount(ushort segment) => (byte)(Memory.Read(segment) >> 8);

        /// <summary>
        ///     Return the segment number of a segment.
        /// </summary>
        /// <param name="segment"></param>
        /// <returns></returns>
        private static byte GetSegmentNumber(ushort segment) => (byte)(Memory.Read(segment) & 0xFF);

        /// <summary>
        ///     Returns a pointer to the activation record of a specified procedure
        ///     in a specified segment.
        /// </summary>
        /// <param name="seg"></param>
        /// <param name="procId"></param>
        /// <returns></returns>
        private static ushort GetProcedureActivationRecord(ushort seg, int procId)
        {
            Memory.PointerCheck(seg);
            if (procId < 1 || procId > VirtualMachine.GetSegmentProcedureCount(seg))
                VirtualMachine.Panic("Proc: Illegal Procedure Number " + procId);
            return Memory.SelfRelPtr(seg.Index((short)-procId));
        }

        /// <summary>
        ///     Returns the procedure number of a procedure.
        /// </summary>
        /// <param name="jTab"></param>
        /// <returns></returns>
        public static ushort GetProcedureNumber(ushort jTab)
        {
            Memory.PointerCheck(jTab);
            return (byte)Memory.Read(jTab);
        }

        /// <summary>
        ///     Returns the lex level of a procedure.
        /// </summary>
        /// <param name="jTab"></param>
        /// <returns></returns>
        private static sbyte GetLexLevel(ushort jTab)
        {
            Memory.PointerCheck(jTab);
            return (sbyte)(Memory.Read(jTab) >> 8);
        }

        /// <summary>
        ///     Returns the byte offset to the exit code of a procedure.
        /// </summary>
        /// <param name="jTab"></param>
        /// <returns></returns>
        private static ushort GetExitIpc(ushort jTab)
        {
            Memory.PointerCheck(jTab);
            return (ushort)(Memory.Read(jTab.Index(-1)) - Memory.Read(jTab.Index(-2)) - 2);
        }

        /// <summary>
        ///     Returns the size of the parameters, which are passed to a
        ///     procedure.
        /// </summary>
        /// <param name="jTab"></param>
        /// <returns></returns>
        private static ushort GetParameterSize(ushort jTab)
        {
            Memory.PointerCheck(jTab);
            return Memory.Read(jTab.Index(-3));
        }

        /// <summary>
        ///     Returns the size of the storage a procedure needs for its local
        ///     variables.
        /// </summary>
        /// <param name="jTab"></param>
        /// <returns></returns>
        private static ushort GetLocalVariableSize(ushort jTab)
        {
            Memory.PointerCheck(jTab);
            return Memory.Read(jTab.Index(-4));
        }

        /// <summary>
        ///     Returns a pointer to a local variable.
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        private static ushort LocalAddress(ushort offset) => VirtualMachine.MarkStackPointer.Index(
            (ushort)(MethodStack.Variables + offset));

        /// <summary>
        ///     Returns a pointer to a global variable.
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        private static ushort GlobalAddress(short offset) => VirtualMachine.Base.Index(
            (short)(MethodStack.Variables + offset));

        /// <summary>
        ///     Traverse the static link chain.
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        private static ushort Intermediate(int count)
        {
            ushort p;
            for (p = VirtualMachine.MarkStackPointer; count != 0; count--)
                p = Memory.Read(p.Index((short)MethodStack.StaticLinks));
            return p;
        }

        /// <summary>
        ///     Returns a pointer to a variable of an enclosing procedure.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        private static ushort IntermediateAddress(short offset, byte count) => VirtualMachine.Intermediate(count)
            .Index((short)(MethodStack.Variables + offset));

        /// <summary>
        ///     Returns a pointer to a variable in a data segment (a global
        ///     variable in a UNIT)
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="segNo"></param>
        /// <returns></returns>
        private static ushort ExternalAddress(short offset, byte segNo)
        {
            Debug.Assert(segNo < VirtualMachine.segmentDictionarySize);
            return VirtualMachine.SegmentDictionary[segNo].SegmentPointer.Index(offset);
        }

        /// <summary>
        ///     calculates the target address of a jump operation. Positive
        ///     displacements perform relative jumps, negative displacements are
        ///     used as indices into the jump table.
        /// </summary>
        /// <param name="disp"></param>
        private static ushort GetJumpTarget(int disp)
        {
            if (disp >= 0)
                return (ushort)(VirtualMachine.InterpreterProgramCounter + disp);
            disp = -disp;
#if WORD_MEMORY
            return (ushort)(Memory.Read(VirtualMachine.JumpTable.Index(-1)) + 2 -
                             (Memory.Read((ushort)(VirtualMachine.JumpTable - disp / 2)) + disp));
#else
            return (ushort)(Memory.Rd(JTab.Index(-1)) + 2 - (Memory.Rd((ushort)(JTab - disp)) + disp));
#endif
        }

        /// <summary>
        ///     Calculates the static link pointer for a procedure.
        /// </summary>
        /// <param name="newSeg"></param>
        /// <param name="procNo"></param>
        /// <returns></returns>
        private static ushort GetStaticLink(ushort newSeg, byte procNo)
        {
            var newJTab = VirtualMachine.GetProcedureActivationRecord(newSeg, procNo);

            return VirtualMachine.GetProcedureNumber(newJTab) == 0
                ? (ushort)0
                : VirtualMachine.Intermediate(VirtualMachine.GetLexLevel(VirtualMachine.JumpTable) -
                                              VirtualMachine.GetLexLevel(newJTab) + 1);
        }

        /// <summary>
        ///     Load a segment.  If a data segment is to be loaded, just allocate
        ///     storage on the stack.
        /// </summary>
        /// <param name="segNo"></param>
        private static void CspLoadSegment(int segNo)
        {
            Debug.Assert(segNo < VirtualMachine.segmentDictionarySize);
            var si = VirtualMachine.SegmentDictionary[segNo];
            if (si.UseCount++ == 0)
            {
                var segUnit = Memory.Read(PSystem.SegmentUnitPointer((ushort)segNo));
                var segBlock = Memory.Read(PSystem.SegmentBlockPointer((ushort)segNo));
                var segSize = Memory.Read(PSystem.SegmentSizePointer((ushort)segNo));

                Debug.Assert((segSize & 1) == 0);
                if (segSize == 0)
                    throw new ExecutionException(ExecutionErrorCode.NoSegment);

                si.OldKp = VirtualMachine.ProgramStackPointer;
#if WORD_MEMORY
                VirtualMachine.ProgramStackPointer -= (ushort)(segSize / 2);
#else
                Kp -= segSize;
#endif
                si.SegBase = VirtualMachine.ProgramStackPointer;
                if (segBlock != 0)
                {
                    si.SegmentPointer = si.OldKp.Index(-1);
                    DiskIO.Read(segUnit, VirtualMachine.ProgramStackPointer, 0, segSize, segBlock);
                    if (Memory.Read(PSystem.IOResultPointer) != 0)
                        throw new ExecutionException(ExecutionErrorCode.System);
                }
                else
                    si.SegmentPointer = VirtualMachine.ProgramStackPointer.Index(-1);
            }
            VirtualMachine.SegmentDictionary[segNo] = si;
        }

        private static void CspUnloadSegment(byte segNo)
        {
            var si = VirtualMachine.SegmentDictionary[segNo];
            Debug.Assert(si.UseCount > 0);
            si.UseCount--;
            if (si.UseCount == 0)
            {
                VirtualMachine.ProgramStackPointer = si.OldKp;
                si.OldKp = 0;
                si.SegmentPointer = 0;
            }
            VirtualMachine.SegmentDictionary[segNo] = si;
        }

        /// <summary>
        ///     Clear the global directory pointer.
        /// </summary>
        private static void ClearGlobalDirectoryPointer()
        {
            var gdirp = Memory.Read(PSystem.GlobalDirectoryPointer);
            if (gdirp == 0)
                return;
            VirtualMachine.NewPointer = gdirp;
            Memory.Write(PSystem.GlobalDirectoryPointer, 0);
        }

        /// <summary>
        ///     check for a gap between heap and stack.
        /// </summary>
        private static void StackCheck()
        {
            if (VirtualMachine.NewPointer < VirtualMachine.ProgramStackPointer)
                return;
            Memory.Write(PSystem.GlobalDirectoryPointer, 0);
            VirtualMachine.ProgramStackPointer = 0x8000;
            VirtualMachine.NewPointer = 0x6200;
            throw new ExecutionException(ExecutionErrorCode.StackOverflow);
        }

        /// <summary>
        ///     Call a procedure.  It builds a stack frame for the new procedure
        ///     and sets up all registers of the p-machine.
        /// </summary>
        /// <param name="newSeg"></param>
        /// <param name="procId"></param>
        /// <param name="staticLink"></param>
        /// <returns>
        ///     1 if its a native procedure,
        ///     0 if it is a p-code procedure
        /// </returns>
        private static int Call(ushort newSeg, int procId, ushort staticLink)
        {
            // Find the procedure code section for the called procedure.
            // From the table of attributes (JTAB) in the called procedure's
            // code section, find the data size and parameter size of the
            // called procedure.
            var newJTab = VirtualMachine.GetProcedureActivationRecord(newSeg, procId);
            var dataSize = VirtualMachine.GetLocalVariableSize(newJTab);
            var paramSize = VirtualMachine.GetParameterSize(newJTab);
            // Extend the program stack by a number of bytes equal to the
            // data size plus the parameter size.
            var newMp = VirtualMachine.ProgramStackPointer.Index((short)(-(dataSize + paramSize) / 2));

            if (VirtualMachine.GetProcedureNumber(newJTab) == 0)
            {
                Native.Process(newJTab);
                return 1;
            }

            Debug.Assert((paramSize & 1) == 0);

            // Copy a number of bytes equal to the parameter size, from the
            // evaluation stack's  tos  (pointed to by SP) to the beginning
            // of the space just allocated.  This passes parameters to the
            // new procedure from its caller.
            VirtualMachine.MoveLeft(newMp, 0, VirtualMachine.StackPointer, 0, paramSize);
            // release parameters
            VirtualMachine.StackPointer = VirtualMachine.StackPointer.Index((ushort)(paramSize / 2));

            newMp = newMp.Index(-(short)MethodStack.FrameSize);
            if (VirtualMachine.GetLexLevel(newJTab) <= 0)
            {
                Stack.Push(VirtualMachine.Base);
                VirtualMachine.Base = newMp;
                Memory.Write(PSystem.StackBase, VirtualMachine.Base);
            }

            Memory.Write(newMp.Index((short)MethodStack.ProgramStackPointer), VirtualMachine.ProgramStackPointer);
            Memory.Write(newMp.Index((short)MethodStack.StaticLinks), staticLink);
            Memory.Write(newMp.Index((short)MethodStack.Dyn), VirtualMachine.MarkStackPointer);
            Memory.Write(newMp.Index((short)MethodStack.JumpTable), VirtualMachine.JumpTable);
            Memory.Write(newMp.Index((short)MethodStack.SegmentId), VirtualMachine.Segment);
            Memory.Write(newMp.Index((short)MethodStack.InterpreterProgramCounter),
                VirtualMachine.InterpreterProgramCounter);
            Memory.Write(newMp.Index((short)MethodStack.StackPointer), VirtualMachine.StackPointer);

            VirtualMachine.ProgramStackPointer = newMp.Index(-1); // hack?
            VirtualMachine.MarkStackPointer = newMp;
            VirtualMachine.Segment = newSeg;
            VirtualMachine.JumpTable = newJTab;
            Memory.Write(PSystem.LastMp, VirtualMachine.MarkStackPointer);

            Memory.Write(PSystem.Seg, VirtualMachine.Segment);

            Memory.Write(PSystem.JTab, VirtualMachine.JumpTable);

            VirtualMachine.InterpreterProgramCounterBase =
                VirtualMachine.GetProcedureInstructionPointerBase(VirtualMachine.JumpTable);
            VirtualMachine.InterpreterProgramCounter = 0;
            VirtualMachine.level++;
            VirtualMachine.StackCheck();
            return 0;
        }

        private static void Return(int n)
        {
            var oldMp = VirtualMachine.MarkStackPointer;
            var oldSegNo = VirtualMachine.GetSegmentNumber(VirtualMachine.Segment);

            while (n > 0)
                Stack.Push(Memory.Read(VirtualMachine.LocalAddress((ushort)n--)));

            VirtualMachine.ProgramStackPointer = Memory.Read(oldMp.Index((short)MethodStack.ProgramStackPointer));
            VirtualMachine.MarkStackPointer = Memory.Read(oldMp.Index((short)MethodStack.Dyn));
            VirtualMachine.JumpTable = Memory.Read(oldMp.Index((short)MethodStack.JumpTable));
            VirtualMachine.InterpreterProgramCounterBase =
                VirtualMachine.GetProcedureInstructionPointerBase(VirtualMachine.JumpTable);
            VirtualMachine.Segment = Memory.Read(oldMp.Index((short)MethodStack.SegmentId));
            VirtualMachine.InterpreterProgramCounter =
                Memory.Read(oldMp.Index((short)MethodStack.InterpreterProgramCounter));
            Memory.Write(PSystem.LastMp, VirtualMachine.MarkStackPointer);
            Memory.Write(PSystem.Seg, VirtualMachine.Segment);
            Memory.Write(PSystem.JTab, VirtualMachine.JumpTable);

            if (oldSegNo != VirtualMachine.GetSegmentNumber(VirtualMachine.Segment) &&
                oldSegNo != 0) // Segment 0 is not managed.
                VirtualMachine.CspUnloadSegment(oldSegNo);
            VirtualMachine.level--;
            VirtualMachine.StackCheck();
        }

        // P - debugger stuff

        /// <summary>
        ///     Dump memory in decimal and in hex. Used to dump the evaluation stack.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        private static void ShowMem(ushort start, ushort end)
        {
            for (; start < end; start = start.Index(1))
                Console.Error.Write(" {0}({1:X})", Memory.Read(start), Memory.Read(start));
            Console.Error.WriteLine();
        }

        /// <summary>
        ///     Disassemble a procedure
        /// </summary>
        /// <param name="segNo"></param>
        /// <param name="jtab"></param>
        /// <returns></returns>
        private static string List(int segNo, ushort jtab)
        {
            var ipcBase = VirtualMachine.GetProcedureInstructionPointerBase(jtab);
            ushort ipc = 0;
            StringBuilder sb = new();
            sb.AppendFormat("Params: {0}, Vars: {1}\n",
                VirtualMachine.GetParameterSize(jtab) / 2, VirtualMachine.GetLocalVariableSize(jtab) / 2);
            while (ipcBase.Index((short)(ipc / 2)) < jtab)
            {
                var opCode = (OpcodeValue)Memory.ReadByte(ipcBase, (short)ipc);
                sb.AppendFormat("{0}:       ", ipc);
                ipc = PTrace.DisasmP(sb, (ushort)segNo, ipcBase, ipc, jtab, 0);
                sb.AppendLine();
                if (opCode == OpcodeValue.RNP || opCode == OpcodeValue.RBP || opCode == OpcodeValue.XIT)
                    break;
            }
            return sb.ToString();
        }

        private static void Debugger()
        {
            if (VirtualMachine.level > VirtualMachine.traceLevel)
                return;
            VirtualMachine.traceLevel = 0x7fff;

            StringBuilder sb = new();
            PTrace.DisasmP(sb, VirtualMachine.GetSegmentNumber(VirtualMachine.Segment),
                VirtualMachine.InterpreterProgramCounterBase, VirtualMachine.InterpreterProgramCounter,
                VirtualMachine.JumpTable, VirtualMachine.StackPointer);
            var prompt =
                $"s{VirtualMachine.GetSegmentNumber(VirtualMachine.Segment)}, p{VirtualMachine.GetProcedureNumber(VirtualMachine.JumpTable)}, {VirtualMachine.currentIpc:D4}:      {sb}      > ";
            sb.Clear();
            for (; ; )
            {
                Console.Error.Write(prompt);
                var line = Console.ReadLine();
                if (line ==
                    "")
                    break;

                //IDisposable closeMethod = null;
                //TextWriter tout = null;
                //line = Buffer;
                //while (*line)
                //{
                //    if ((*line == '|') || (*line == '>'))
                //        break;
                //    else
                //        line++;
                //}

                //if (*line == '|')
                //{
                //    *line = '\0';
                //    line++;
                //    while (*line)
                //    {
                //        if (isspace(*line))
                //            line++;
                //        else
                //            break;
                //    }
                //    out = popen(line, "w");
                //    close_method = pclose;
                //}
                //else if (*line == '>')
                //{
                //    *line = '\0';
                //    line++;
                //    if (*line == '>')
                //    {
                //        line++;
                //        mode = "a";
                //    }
                //    else
                //        mode = "w";
                //    while (*line)
                //    {
                //        if (isspace(*line))
                //            line++;
                //        else
                //            break;
                //    }
                //    out = fopen(line, mode);
                //    close_method = fclose;
                //}
                //if (!out)
                //{
                //    close_method = NULL;
                //    out = stderr;
                //}

                string[] parameters;

                switch (line[0])
                {
                    case 'p':
                        // print stack
                        Console.Error.Write("Stack:");
                        VirtualMachine.ShowMem(VirtualMachine.StackPointer, VirtualMachine.spTop);
                        break;

                    case 'd':
                        parameters = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        switch (parameters.Length)
                        {
#if false
            case 1:
                from = NextDumpAddr;
                goto case 2;
#endif
                            case 2:
                                ushort fromAddr;
                                ushort.TryParse(parameters[1], NumberStyles.AllowHexSpecifier,
                                    CultureInfo.InvariantCulture, out fromAddr);

                                Memory.Dump(Console.Error, fromAddr, (ushort)(fromAddr + 0x80));
                                break;
                            case 3:
                                ushort toAddr;
                                ushort.TryParse(parameters[1], NumberStyles.AllowHexSpecifier,
                                    CultureInfo.InvariantCulture, out fromAddr);
                                ushort.TryParse(parameters[2], NumberStyles.AllowHexSpecifier,
                                    CultureInfo.InvariantCulture, out toAddr);

                                Memory.Dump(Console.Error, fromAddr, toAddr);
                                break;

                            default:
                                Console.Error.WriteLine("d <from> [<to>]");
                                break;
                        }
                        break;

                    case 'l':
                        {
                            var segNo = 0;
                            var procNo = 0;
                            var invalid = false;
                            parameters = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                            switch (parameters.Length)
                            {
                                case 2:
                                    int.TryParse(parameters[1], out segNo);
                                    procNo = segNo;
                                    segNo = VirtualMachine.GetSegmentNumber(VirtualMachine.Segment);
                                    break;

                                case 3:
                                    int.TryParse(parameters[1], out segNo);
                                    int.TryParse(parameters[2], out procNo);
                                    break;

                                default:
                                    Console.Error.WriteLine("l [<SegNo>] <ProcNo>");
                                    invalid = true;
                                    break;
                            }
                            if (!invalid && segNo < VirtualMachine.segmentDictionarySize)
                            {
                                VirtualMachine.CspLoadSegment(segNo);
                                Console.Error.Write(VirtualMachine.List(segNo,
                                    VirtualMachine.GetProcedureActivationRecord(
                                        VirtualMachine.SegmentDictionary[segNo].SegmentPointer, procNo)));
                                VirtualMachine.CspUnloadSegment((byte)segNo);
                            }
                        }
                        break;

                    case 't':
                        {
                            var s = VirtualMachine.Segment;
                            var j = VirtualMachine.JumpTable;
                            var m = VirtualMachine.MarkStackPointer;
                            var i = VirtualMachine.InterpreterProgramCounter;

                            for (; ; )
                            {
                                Console.WriteLine("\ns{0}, p{1}, {2:D4}:",
                                    VirtualMachine.GetSegmentNumber(s), VirtualMachine.GetProcedureNumber(j), i);
                                var w = m.Index((short)MethodStack.Variables);
                                Memory.Dump(Console.Error, w,
                                    (ushort)(w + VirtualMachine.GetParameterSize(j) +
                                              VirtualMachine.GetLocalVariableSize(j)));

                                if (VirtualMachine.GetLexLevel(j) <= 0)
                                    break;
                                j = Memory.Read(m.Index((short)MethodStack.JumpTable));
                                s = Memory.Read(m.Index((short)MethodStack.SegmentId));
                                i = Memory.Read(m.Index((short)MethodStack.InterpreterProgramCounter));
                                m = Memory.Read(m.Index((short)MethodStack.Dyn));
                            }
                        }
                        Memory.Dump(Console.Error, VirtualMachine.ProgramStackPointer, 0xb000);
                        break;

                    case 'v':
                        Memory.Dump
                        (
                            Console.Error,
                            VirtualMachine.MarkStackPointer.Index((short)MethodStack.Variables),
                            (ushort)(VirtualMachine.MarkStackPointer.Index((short)MethodStack.Variables) +
                                      VirtualMachine.GetLocalVariableSize(VirtualMachine.JumpTable)
                                      + VirtualMachine.GetParameterSize(VirtualMachine.JumpTable))
                        );
                        break;

                    case 'g':
                        VirtualMachine.traceLevel = 0;
                        return;

                    case 'n':
                        VirtualMachine.traceLevel = VirtualMachine.level;
                        return;

                    case 'f':
                        VirtualMachine.traceLevel = VirtualMachine.level - 1;
                        return;

                    case 'r':
                        Console.Error.WriteLine(
                            "Sp={0:X4}, Kp={1:X4}, Mp={2:X4}, Base={3:X4}, Seg={4:X4}, JTab={5:X4}, Np={6:X4}",
                            VirtualMachine.StackPointer, VirtualMachine.ProgramStackPointer,
                            VirtualMachine.MarkStackPointer, VirtualMachine.Base, VirtualMachine.Segment,
                            VirtualMachine.JumpTable, VirtualMachine.NewPointer);
                        break;

                    case 'q':
                        Environment.Exit(0);
                        break;
                }
                //if (close_method && out)
                //{
                //    close_method(out);
                //    close_method = NULL;
                //    out = NULL;
                //}
            }
        }

        /// <summary>
        ///     To compare traces with byte and word architecture, this routine
        ///     tries to 'normalize' the value of pointers. Of course, the
        ///     assumtions are not always true, but the diffs get a lot shorter
        ///     using this translation.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static ushort Translate(ushort value)
        {
#if TRACE_TRANSLATE
#if WORD_MEMORY
    if (value > kpTop)
        ;
    else if (value > 0x8000)
        Value = (Value - kpTop) * 2 + kpTop;
    else if (value > 0x7f00)
        ;
    else if (value > heapBottom)
        value = (value - heapBottom) * 2 + heapBottom;
#endif
#endif
            return value;
        }

        public static void Tracer(TextWriter writer)
        {
            StringBuilder stackBuffer = new();

            for (var w = VirtualMachine.StackPointer; w < VirtualMachine.spTop; w = w.Index(1))
            {
                var value = Memory.Read(w);
                stackBuffer.AppendFormat("{0:X4} ", VirtualMachine.Translate(value));
            }
            StringBuilder buffer = new();
            PTrace.DisasmP(buffer, (ushort)(Memory.Read(VirtualMachine.Segment) & 0xFF),
                VirtualMachine.InterpreterProgramCounterBase, VirtualMachine.InterpreterProgramCounter,
                VirtualMachine.JumpTable, VirtualMachine.StackPointer);
            writer.WriteLine("s{0} p{1} o{2}        {3}      Stack: {4}",
                Memory.Read(VirtualMachine.Segment) & 0xff, Memory.Read(VirtualMachine.JumpTable) & 0xff,
                VirtualMachine.InterpreterProgramCounter, buffer, stackBuffer);
        }

        private static void SetTrace(string list)
        {
            var parameters = list.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s =>
                {
                    byte.TryParse(s.Trim(), out var v);
                    return v;
                }).ToArray();
            switch (parameters.Length)
            {
                case 1:
                    VirtualMachine.traceProc = parameters[0];
                    break;
                case 2:
                    VirtualMachine.traceSeg = parameters[0];
                    VirtualMachine.traceProc = parameters[1];
                    break;
                default:
                    Console.Error.WriteLine("invalid trace flags");
                    Environment.Exit(1);
                    break;
            }
        }

        private static bool AppleHack1()
        {
            var save0 = VirtualMachine.InterpreterProgramCounter;

            if (VirtualMachine.FetchByte() == 145) // NGI 
            {
                var opCode = VirtualMachine.FetchByte();
                switch (opCode)
                {
                    case 171: // SRO
                        {
                            var var = VirtualMachine.FetchSignedByte(); // Parameter SRO 
                            opCode = VirtualMachine.FetchByte();
                            if (
                                (
                                    // LDO n 
                                    opCode == 169 && VirtualMachine.FetchSignedByte() == var
                                    ||
                                    // SLDO n 
                                    var >= 1 && var <= 16 && opCode == 231 + var
                                )
                                &&
                                VirtualMachine.FetchByte() == 0 // SLDC 0 
                                &&
                                VirtualMachine.FetchByte() == 190 // LDB 
                            )
                                return true;
                        }
                        break;
                    case 204: // STL
                        {
                            var var = VirtualMachine.FetchSignedByte(); // Parameter STL 
                            opCode = VirtualMachine.FetchByte();
                            if (
                                (
                                    // LDL  n 
                                    opCode == 202 && VirtualMachine.FetchSignedByte() == var
                                    ||
                                    // SLDL n 
                                    var >= 1 && var <= 16 && opCode == 215 + var
                                )
                                &&
                                VirtualMachine.FetchByte() == 0 // SLDC 0 
                                &&
                                VirtualMachine.FetchByte() == 190 // LDB 
                            )
                                return true;
                        }
                        break;
                }
            }
            VirtualMachine.InterpreterProgramCounter = save0;
            return false;
        }

        private static bool AppleHack2()
        {
            var save = VirtualMachine.InterpreterProgramCounter;
            sbyte var;

            if
            (
                VirtualMachine.FetchByte() == 145 && VirtualMachine.FetchByte() == 171 &&
                (var = VirtualMachine.FetchSignedByte()) != 0 && VirtualMachine.FetchByte() == 169 &&
                VirtualMachine.FetchSignedByte() == var && VirtualMachine.FetchByte() == 6 &&
                VirtualMachine.FetchByte() == 192 && VirtualMachine.FetchByte() == 16 &&
                VirtualMachine.FetchByte() == 1 && VirtualMachine.FetchByte() == 186 // LDP 
            )
                return true;
            VirtualMachine.InterpreterProgramCounter = save;
            return false;
        }

        private static void Processor()
        {
            for (; ; )
                try
                {
                    VirtualMachine.Debugger();
                    VirtualMachine.currentIpc = VirtualMachine.InterpreterProgramCounter;
                    var opcode = (OpcodeValue)VirtualMachine.FetchByte();
                    if (!VirtualMachine.ProcessOpcode(opcode))
                        break;
                }
                catch (ExecutionException ee)
                {
                    VirtualMachine.ExecutionExceptionHandler(ee);
                }
        }

        private static bool ProcessOpcode(OpcodeValue opcode)
        {
            switch (opcode)
            {
                // One word Load and Stores constant
                case OpcodeValue.SLDC_0:
                case OpcodeValue.SLDC_1:
                case OpcodeValue.SLDC_2:
                case OpcodeValue.SLDC_3:
                case OpcodeValue.SLDC_4:
                case OpcodeValue.SLDC_5:
                case OpcodeValue.SLDC_6:
                case OpcodeValue.SLDC_7:
                case OpcodeValue.SLDC_8:
                case OpcodeValue.SLDC_9:
                case OpcodeValue.SLDC_10:
                case OpcodeValue.SLDC_11:
                case OpcodeValue.SLDC_12:
                case OpcodeValue.SLDC_13:
                case OpcodeValue.SLDC_14:
                case OpcodeValue.SLDC_15:
                case OpcodeValue.SLDC_16:
                case OpcodeValue.SLDC_17:
                case OpcodeValue.SLDC_18:
                case OpcodeValue.SLDC_19:
                case OpcodeValue.SLDC_20:
                case OpcodeValue.SLDC_21:
                case OpcodeValue.SLDC_22:
                case OpcodeValue.SLDC_23:
                case OpcodeValue.SLDC_24:
                case OpcodeValue.SLDC_25:
                case OpcodeValue.SLDC_26:
                case OpcodeValue.SLDC_27:
                case OpcodeValue.SLDC_28:
                case OpcodeValue.SLDC_29:
                case OpcodeValue.SLDC_30:
                case OpcodeValue.SLDC_31:
                case OpcodeValue.SLDC_32:
                case OpcodeValue.SLDC_33:
                case OpcodeValue.SLDC_34:
                case OpcodeValue.SLDC_35:
                case OpcodeValue.SLDC_36:
                case OpcodeValue.SLDC_37:
                case OpcodeValue.SLDC_38:
                case OpcodeValue.SLDC_39:
                case OpcodeValue.SLDC_40:
                case OpcodeValue.SLDC_41:
                case OpcodeValue.SLDC_42:
                case OpcodeValue.SLDC_43:
                case OpcodeValue.SLDC_44:
                case OpcodeValue.SLDC_45:
                case OpcodeValue.SLDC_46:
                case OpcodeValue.SLDC_47:
                case OpcodeValue.SLDC_48:
                case OpcodeValue.SLDC_49:
                case OpcodeValue.SLDC_50:
                case OpcodeValue.SLDC_51:
                case OpcodeValue.SLDC_52:
                case OpcodeValue.SLDC_53:
                case OpcodeValue.SLDC_54:
                case OpcodeValue.SLDC_55:
                case OpcodeValue.SLDC_56:
                case OpcodeValue.SLDC_57:
                case OpcodeValue.SLDC_58:
                case OpcodeValue.SLDC_59:
                case OpcodeValue.SLDC_60:
                case OpcodeValue.SLDC_61:
                case OpcodeValue.SLDC_63:
                case OpcodeValue.SLDC_64:
                case OpcodeValue.SLDC_65:
                case OpcodeValue.SLDC_66:
                case OpcodeValue.SLDC_67:
                case OpcodeValue.SLDC_68:
                case OpcodeValue.SLDC_69:
                case OpcodeValue.SLDC_70:
                case OpcodeValue.SLDC_71:
                case OpcodeValue.SLDC_72:
                case OpcodeValue.SLDC_73:
                case OpcodeValue.SLDC_74:
                case OpcodeValue.SLDC_75:
                case OpcodeValue.SLDC_76:
                case OpcodeValue.SLDC_77:
                case OpcodeValue.SLDC_78:
                case OpcodeValue.SLDC_79:
                case OpcodeValue.SLDC_80:
                case OpcodeValue.SLDC_81:
                case OpcodeValue.SLDC_82:
                case OpcodeValue.SLDC_83:
                case OpcodeValue.SLDC_84:
                case OpcodeValue.SLDC_85:
                case OpcodeValue.SLDC_86:
                case OpcodeValue.SLDC_87:
                case OpcodeValue.SLDC_88:
                case OpcodeValue.SLDC_89:
                case OpcodeValue.SLDC_90:
                case OpcodeValue.SLDC_91:
                case OpcodeValue.SLDC_92:
                case OpcodeValue.SLDC_93:
                case OpcodeValue.SLDC_94:
                case OpcodeValue.SLDC_95:
                case OpcodeValue.SLDC_96:
                case OpcodeValue.SLDC_97:
                case OpcodeValue.SLDC_98:
                case OpcodeValue.SLDC_99:
                case OpcodeValue.SLDC_100:
                case OpcodeValue.SLDC_101:
                case OpcodeValue.SLDC_102:
                case OpcodeValue.SLDC_103:
                case OpcodeValue.SLDC_104:
                case OpcodeValue.SLDC_105:
                case OpcodeValue.SLDC_106:
                case OpcodeValue.SLDC_107:
                case OpcodeValue.SLDC_108:
                case OpcodeValue.SLDC_109:
                case OpcodeValue.SLDC_110:
                case OpcodeValue.SLDC_111:
                case OpcodeValue.SLDC_112:
                case OpcodeValue.SLDC_113:
                case OpcodeValue.SLDC_114:
                case OpcodeValue.SLDC_115:
                case OpcodeValue.SLDC_116:
                case OpcodeValue.SLDC_117:
                case OpcodeValue.SLDC_118:
                case OpcodeValue.SLDC_119:
                case OpcodeValue.SLDC_120:
                case OpcodeValue.SLDC_121:
                case OpcodeValue.SLDC_122:
                case OpcodeValue.SLDC_123:
                case OpcodeValue.SLDC_124:
                case OpcodeValue.SLDC_125:
                case OpcodeValue.SLDC_126:
                case OpcodeValue.SLDC_127:
                    // push the constant
                    Stack.Push((ushort)(opcode - OpcodeValue.SLDC_0));
                    break;
                case OpcodeValue.LDCN: // Load Constant Nil
                    // push the vallue of NIL
                    Stack.Push(0);
                    break;
                case OpcodeValue.LDCI: // Load Constant Integer
                                       // This is always a little-endian fetch, even on big-endian
                                       // hosts, because there is no guarantee of word alignment (and
                                       // that's how the native compiler is written).
                    {
                        var p1 = VirtualMachine.FetchWord();
                        switch (p1)
                        {
                            case 16607:
                                if (VirtualMachine.AppleHack1())
                                    Stack.Push(4);
                                else
                                    Stack.Push(p1);
                                break;
                            case 16606:
                                if (VirtualMachine.AppleHack2())
                                    Stack.Push(0);
                                else
                                    Stack.Push(p1);
                                break;
                            default:
                                Stack.Push(p1);
                                break;
                        }
                    }
                    break;

                // One-word load and stores local
                // SLDL Short LoaD Local 1..16    
                case OpcodeValue.SLDL_1:
                case OpcodeValue.SLDL_2:
                case OpcodeValue.SLDL_3:
                case OpcodeValue.SLDL_4:
                case OpcodeValue.SLDL_5:
                case OpcodeValue.SLDL_6:
                case OpcodeValue.SLDL_7:
                case OpcodeValue.SLDL_8:
                case OpcodeValue.SLDL_9:
                case OpcodeValue.SLDL_10:
                case OpcodeValue.SLDL_11:
                case OpcodeValue.SLDL_12:
                case OpcodeValue.SLDL_13:
                case OpcodeValue.SLDL_14:
                case OpcodeValue.SLDL_15:
                case OpcodeValue.SLDL_16:
                    // Fetch the word with offset x in the marked stack variables 
                    Stack.Push(Memory.Read(VirtualMachine.LocalAddress((ushort)(opcode - OpcodeValue.SLDL_1 + 1))));
                    break;
                case OpcodeValue.LDL: //Load  Local
                    Stack.Push(Memory.Read(VirtualMachine.LocalAddress((ushort)VirtualMachine.FetchSignedByte())));
                    break;
                case OpcodeValue.LLA: // Load Local Address
                    Stack.Push(VirtualMachine.LocalAddress((ushort)VirtualMachine.FetchSignedByte()));
                    break;
                case OpcodeValue.STL: // Store Local
                    Memory.Write(VirtualMachine.LocalAddress((ushort)VirtualMachine.FetchSignedByte()), Stack.Pop());
                    break;
                // One-word load and stores global
                // Short Load Global Word
                case OpcodeValue.SLDO_1:
                case OpcodeValue.SLDO_2:
                case OpcodeValue.SLDO_3:
                case OpcodeValue.SLDO_4:
                case OpcodeValue.SLDO_5:
                case OpcodeValue.SLDO_6:
                case OpcodeValue.SLDO_7:
                case OpcodeValue.SLDO_8:
                case OpcodeValue.SLDO_9:
                case OpcodeValue.SLDO_10:
                case OpcodeValue.SLDO_11:
                case OpcodeValue.SLDO_12:
                case OpcodeValue.SLDO_13:
                case OpcodeValue.SLDO_14:
                case OpcodeValue.SLDO_15:
                case OpcodeValue.SLDO_16:
                    Stack.Push(Memory.Read(VirtualMachine.GlobalAddress((short)(opcode - OpcodeValue.SLDO_1 + 1))));
                    break;
                case OpcodeValue.LDO: // Load Global
                    Stack.Push(Memory.Read(VirtualMachine.GlobalAddress(VirtualMachine.FetchSignedByte())));
                    break;
                case OpcodeValue.LAO: // Load Address Global
                    Stack.Push(VirtualMachine.GlobalAddress(VirtualMachine.FetchSignedByte()));
                    break;
                case OpcodeValue.SRO: // Store Global
                    Memory.Write(VirtualMachine.GlobalAddress(VirtualMachine.FetchSignedByte()), Stack.Pop());
                    break;
                // One-word load and stores intermediate
                case OpcodeValue.LOD: // Load
                    {
                        var p1 = VirtualMachine.FetchByte();
                        Stack.Push(Memory.Read(VirtualMachine.IntermediateAddress(VirtualMachine.FetchSignedByte(), p1)));
                        break;
                    }
                case OpcodeValue.LDA: // Load Address
                    {
                        var p1 = VirtualMachine.FetchByte();
                        Stack.Push(VirtualMachine.IntermediateAddress(VirtualMachine.FetchSignedByte(), p1));
                    }
                    break;
                case OpcodeValue.STR: // Store
                    {
                        var p1 = VirtualMachine.FetchByte();
                        Memory.Write(VirtualMachine.IntermediateAddress(VirtualMachine.FetchSignedByte(), p1), Stack.Pop());
                    }
                    break;
                // One-word load and stores indirect
                case OpcodeValue.SIND_0: // Short Indirect
                case OpcodeValue.SIND_1:
                case OpcodeValue.SIND_2:
                case OpcodeValue.SIND_3:
                case OpcodeValue.SIND_4:
                case OpcodeValue.SIND_5:
                case OpcodeValue.SIND_6:
                case OpcodeValue.SIND_7:
                    Stack.Push(Memory.Read(Stack.Pop().Index(opcode - OpcodeValue.SIND_0)));
                    break;
                case OpcodeValue.IND: // Indirect
                    Stack.Push(Memory.Read(Stack.Pop().Index(VirtualMachine.FetchSignedByte())));
                    break;
                case OpcodeValue.STO: // Store Indirect
                    {
                        var p1 = Stack.Pop();
                        Memory.Write(Stack.Pop(), p1);
                    }
                    break;
                // One-word load and stores external
                case OpcodeValue.LDE: // Load External
                    {
                        var p1 = VirtualMachine.FetchByte();
                        Stack.Push(Memory.Read(VirtualMachine.ExternalAddress(VirtualMachine.FetchSignedByte(), p1)));
                    }
                    break;
                case OpcodeValue.LAE: // Load Address External  
                    {
                        var p1 = VirtualMachine.FetchByte();
                        Stack.Push(VirtualMachine.ExternalAddress(VirtualMachine.FetchSignedByte(), p1));
                    }
                    break;
                case OpcodeValue.STE: // Store External
                    {
                        var p1 = VirtualMachine.FetchByte();
                        Memory.Write(VirtualMachine.ExternalAddress(VirtualMachine.FetchSignedByte(), p1), Stack.Pop());
                    }
                    break;
                // multiple-word loads and stores
                case OpcodeValue.LDC: // Load Multiple Word Constant
                    {
                        var p1 = VirtualMachine.FetchByte();
                        VirtualMachine.InterpreterProgramCounter =
                            (ushort)(VirtualMachine.InterpreterProgramCounter + 1 & ~1); // allowed only on word boundary
#if WORD_MEMORY
                        //var w = IpcBase + Ipc / 2;
#else //var w = IpcBase + Ipc;
#endif
                        // FIXME: should this be a "native" word fetch?
                        while (p1-- != 0)
                            Stack.Push(VirtualMachine.FetchWord());
                    }
                    break;
                case OpcodeValue.LDM: // Load Multiple Words
                    {
                        var p1 = VirtualMachine.FetchByte();
                        var w = Stack.Pop();
                        while (p1-- != 0)
                            Stack.Push(Memory.Read(w.Index(p1)));
                    }
                    break;
                case OpcodeValue.STM: // Store Multiple Words
                    {
                        var p1 = VirtualMachine.FetchByte();
                        var w = Memory.Read(VirtualMachine.StackPointer.Index(p1));
                        while (p1-- != 0)
                        {
                            Memory.Write(w, Stack.Pop());
                            w = w.Index(1);
                        }
                        Stack.Pop();
                    }
                    break;
                // byte array handling  
                case OpcodeValue.LDB: // Load ByteArray
                    {
                        var w = (short)Stack.Pop();
                        Stack.Push(Memory.ReadByte(Stack.Pop(), w));
                    }
                    break;
                case OpcodeValue.STB: // Store ByteArray
                    {
                        var p1 = (byte)Stack.Pop();
                        var w = (short)Stack.Pop();
                        Memory.WriteByte(Stack.Pop(), w, p1);
                    }
                    break;
                // string handling   
                case OpcodeValue.LSA: // Load String Address
                    Debug.Assert((VirtualMachine.InterpreterProgramCounter & 1) == 0);
                    Stack.Push(VirtualMachine.InterpreterProgramCounterBase.Index(
                        (short)(VirtualMachine.InterpreterProgramCounter / 2)));
                    VirtualMachine.InterpreterProgramCounter += (ushort)(VirtualMachine.FetchByte() + 1);
                    break;
                case OpcodeValue.SAS: // String Assign
                    {
                        var p1 = VirtualMachine.FetchByte();
                        var w = Stack.Pop();
                        if ((w & 0xFF00) != 0)
                        {
                            // copy string
                            var len = Memory.ReadByte(w, 0);
                            var dest = Stack.Pop();
                            if (len > p1)
                                throw new ExecutionException(ExecutionErrorCode.StringTooLong);
                            VirtualMachine.MoveLeft(dest, 0, w, 0, len + 1);
                        }
                        else
                        {
                            // store char
                            var dest = Stack.Pop();
                            Memory.WriteByte(dest, 0, 1); // make a string of len 1
                            Memory.WriteByte(dest, 1, (byte)w); // containing char on stack
                        }
                    }
                    break;
                case OpcodeValue.IXS: // Index String Array
                    {
                        var p1 = Stack.Pop();
                        var p2 = Stack.Pop();
                        Stack.Push(p2);
                        Stack.Push(p1);
                        if (p1 > Memory.ReadByte(p2, 0))
                            throw new ExecutionException(ExecutionErrorCode.InvalidIndex);
                    }
                    break;
                // record and array handling    
                case OpcodeValue.MOV: // Move Words
                    {
                        var p1 = VirtualMachine.FetchSignedByte();
                        var src = Stack.Pop();
                        var dst = Stack.Pop();
                        while (p1-- != 0)
                        {
                            Memory.Write(dst, Memory.Read(src));
                            dst = dst.Index(1);
                            src = src.Index(1);
                        }
                    }
                    break;
                case OpcodeValue.INC: // Increment Field Pointer
                    Stack.Push(Stack.Pop().Index(VirtualMachine.FetchSignedByte()));
                    break;
                case OpcodeValue.IXA: // Index Array
                    {
                        var w = Stack.Pop();
                        Stack.Push(Stack.Pop().Index((short)(w * VirtualMachine.FetchSignedByte())));
                    }
                    break;
                case OpcodeValue.IXP: // Index Packed Array
                    {
                        var p1 = VirtualMachine.FetchByte();
                        var p2 = VirtualMachine.FetchByte();
                        var w = Stack.Pop();
                        Stack.Push(Stack.Pop().Index((ushort)(w / p1))); // Address
                        Stack.Push(p2);
                        Stack.Push(w % p1 * p2
#if IXP_COMPATIBILITY
                        * 0x101
#endif
                    );
                    }
                    break;

                case OpcodeValue.LPA: // Load Packed Array
                    {
                        var p1 = VirtualMachine.FetchSignedByte();
#if WORD_MEMORY
                        Stack.Push(VirtualMachine.InterpreterProgramCounterBase +
                                   VirtualMachine.InterpreterProgramCounter / 2);
#else
                        Stack.Push(IpcBase + Ipc);
#endif
                        VirtualMachine.InterpreterProgramCounter = (ushort)(VirtualMachine.InterpreterProgramCounter + p1);
                    }
                    break;
                case OpcodeValue.LDP: // Load Packed Field   
                    {
                        var offset = (ushort)(Stack.Pop() & 0xff);
                        var size = Stack.Pop();
                        var addr = Stack.Pop();
                        if (offset + size > 16)
                        {
                            VirtualMachine.Warning("LDP: Offset({0})+Size({1}) > Bits per word",
                                offset, size);
                            throw new ExecutionException(ExecutionErrorCode.InvalidIndex);
                        }
                        Stack.Push(Memory.Read(addr) >> offset & (1 << size) - 1);
                    }
                    break;
                case OpcodeValue.STP: // Store Packed Field
                    {
                        var w = Stack.Pop();
                        var offset = (ushort)(Stack.Pop() & 0xff);
                        var size = Stack.Pop();
                        var addr = Stack.Pop();
                        if (offset + size > 16)
                        {
                            VirtualMachine.Warning("STP: Offset({0})+Size({1}) > Bits per word",
                                offset, size);
                            throw new ExecutionException(ExecutionErrorCode.InvalidIndex);
                        }
                        w = (ushort)(w & (1 << size) - 1);
                        Memory.Write
                        (
                            addr,
                            (ushort)(
                                Memory.Read(addr) & ~((1 << size) - 1 << offset)
                                |
                                w << offset
                            )
                        );
                    }
                    break;
                // TOS arithmetic: integers
                case OpcodeValue.ABI: // Absolute Integer
                    Stack.Push(Math.Abs(Stack.PopInteger()));
                    break;
                case OpcodeValue.ADI: // Add Integer
                    Stack.Push((short)(Stack.PopInteger() + Stack.PopInteger()));
                    break;
                case OpcodeValue.NGI: // Negate Integer
                    Stack.Push((short)-Stack.PopInteger());
                    break;
                case OpcodeValue.SBI: // Subtract Integer
                    {
                        var i = Stack.PopInteger();
                        Stack.Push((short)(Stack.PopInteger() - i));
                    }
                    break;
                case OpcodeValue.MPI: // Multiply Integer
                    Stack.Push((short)(Stack.Pop() * Stack.Pop()));
                    break;
                case OpcodeValue.SQI: // Square Integer
                    {
                        var i = Stack.PopInteger();
                        Stack.Push(i * i);
                    }
                    break;
                case OpcodeValue.DVI: // Divide Integer
                    {
                        var i = Stack.PopInteger();
                        if (i == 0)
                            throw new ExecutionException(ExecutionErrorCode.DivideByZero);
                        Stack.Push((short)(Stack.PopInteger() / i));
                    }
                    break;
                case OpcodeValue.MODI: // Modulo Integer
                    {
                        var i = Stack.PopInteger();
                        if (i == 0)
                            throw new ExecutionException(ExecutionErrorCode.DivideByZero);
                        Stack.Push((short)(Stack.PopInteger() % i));
                    }
                    break;
                case OpcodeValue.CHK: // Check
                    {
                        var upper = Stack.PopInteger();
                        var lower = Stack.PopInteger();
                        var value = Stack.PopInteger();
                        Stack.Push(value);
                        if (value > upper || value < lower)
                            throw new ExecutionException(ExecutionErrorCode.InvalidIndex);
                    }
                    break;
                case OpcodeValue.EQUI: // Equal Integer
                    {
                        var i = Stack.PopInteger();
                        Stack.Push(PSystem.Boolean(Stack.PopInteger() == i));
                    }
                    break;
                case OpcodeValue.NEQI: // Not Equal Integer
                    {
                        var i = Stack.PopInteger();
                        Stack.Push(PSystem.Boolean(Stack.PopInteger() == i));
                    }
                    break;
                case OpcodeValue.LEQI: // Less or Equal Integer
                    {
                        var i = Stack.PopInteger();
                        Stack.Push(PSystem.Boolean(Stack.PopInteger() <= i));
                    }
                    break;
                case OpcodeValue.LESI: // Less Integer
                    {
                        var i = Stack.PopInteger();
                        Stack.Push(PSystem.Boolean(Stack.PopInteger() < i));
                    }
                    break;
                case OpcodeValue.GEQI: // Greater or Equal Integer
                    {
                        var i = Stack.PopInteger();
                        Stack.Push(PSystem.Boolean(Stack.PopInteger() >= i));
                    }
                    break;
                case OpcodeValue.GRTI: // Greater Integer
                    {
                        var i = Stack.PopInteger();
                        Stack.Push(PSystem.Boolean(Stack.PopInteger() > i));
                    }
                    break;
                // TOS arithmetic reals  
                case OpcodeValue.FLT: // Float Tos
                    Stack.Push((float)Stack.PopInteger());
                    break;
                case OpcodeValue.FLO: // Float Next to Tos
                    {
                        var f = Stack.PopReal();
                        Stack.Push((float)Stack.PopInteger());
                        Stack.Push(f);
                    }
                    break;
                case OpcodeValue.ABR: // Absolute Real
                    Stack.Push(Math.Abs(Stack.PopReal()));
                    break;
                case OpcodeValue.ADR: // Add Real
                    Stack.Push(Stack.PopReal() + Stack.PopReal());
                    break;
                case OpcodeValue.NGR: // Negate Real
                    Stack.Push(-Stack.PopReal());
                    break;
                case OpcodeValue.SBR: // Subtract Real
                    {
                        var f = Stack.PopReal();
                        Stack.Push(Stack.PopReal() - f);
                    }
                    break;
                case OpcodeValue.MPR: // Multiply Real
                    Stack.Push(Stack.PopReal() * Stack.PopReal());
                    break;
                case OpcodeValue.SQR: // Square Real
                    {
                        var f = Stack.PopReal();
                        Stack.Push(f * f);
                    }
                    break;
                case OpcodeValue.DVR:
                    {
                        var f = Stack.PopReal();
                        //if (Math.Abs(f) < 0.000001f)
                        //    throw new ExecutionException(ExecutionErrorCode.DivideByZero);
                        Stack.Push(Stack.PopReal() / f);
                    }
                    break;
                case OpcodeValue.EQU: // Equal
                    switch ((OpcodeTypeValue)VirtualMachine.FetchByte())
                    {
                        case OpcodeTypeValue.Real:
                            Stack.Push(PSystem.Boolean(Math.Abs(Stack.PopReal() - Stack.PopReal()) <
                                                       VirtualMachine.machineEpsilonFloat));
                            break;
                        case OpcodeTypeValue.String:
                            Stack.Push(PSystem.Boolean(Array.StrCmp(Stack.Pop(), Stack.Pop()) == 0));
                            break;
                        case OpcodeTypeValue.Boolean:
                            // ReSharper disable once EqualExpressionComparison
                            Stack.Push(PSystem.Boolean((Stack.Pop() & 1) == (Stack.Pop() & 1)));
                            break;
                        case OpcodeTypeValue.Set:
                            Stack.Push(PSystem.Boolean(!Set.Pop().IsNotEqual(Set.Pop())));
                            break;
                        case OpcodeTypeValue.ByteArray:
                            Stack.Push(PSystem.Boolean(Array.ByteCmp(Stack.Pop(), Stack.Pop(),
                                                           (ushort)VirtualMachine.FetchSignedByte()) == 0));
                            break;
                        case OpcodeTypeValue.WordArray:
                            {
                                var p1 = (ushort)VirtualMachine.FetchSignedByte();
                                Stack.Push(
                                    PSystem.Boolean(Array.WordCmp(Stack.Pop(), Stack.Pop(), p1) == 0));
                            }
                            break;
                        default:
                            throw new ExecutionException(ExecutionErrorCode.UnimplementedInstruction);
                    }
                    break;
                case OpcodeValue.NEQ: // Not Equal
                    switch ((OpcodeTypeValue)VirtualMachine.FetchByte())
                    {
                        case OpcodeTypeValue.Real:
                            Stack.Push(PSystem.Boolean(Math.Abs(Stack.PopReal() - Stack.PopReal()) >=
                                                       VirtualMachine.machineEpsilonFloat));
                            break;
                        case OpcodeTypeValue.String:
                            Stack.Push(PSystem.Boolean(Array.StrCmp(Stack.Pop(), Stack.Pop()) != 0));
                            break;
                        case OpcodeTypeValue.Boolean:
                            // ReSharper disable once EqualExpressionComparison
                            Stack.Push(PSystem.Boolean((Stack.Pop() & 1) != (Stack.Pop() & 1)));
                            break;
                        case OpcodeTypeValue.Set:
                            Stack.Push(PSystem.Boolean(Set.Pop().IsNotEqual(Set.Pop())));
                            break;
                        case OpcodeTypeValue.ByteArray:
                            Stack.Push(PSystem.Boolean(Array.ByteCmp(Stack.Pop(), Stack.Pop(),
                                                           (ushort)VirtualMachine.FetchSignedByte()) != 0));
                            break;
                        case OpcodeTypeValue.WordArray:
                            {
                                var p1 = (ushort)VirtualMachine.FetchSignedByte();
                                Stack.Push(
                                    PSystem.Boolean(Array.WordCmp(Stack.Pop(), Stack.Pop(), p1) != 0));
                            }
                            break;
                        default:
                            throw new ExecutionException(ExecutionErrorCode.UnimplementedInstruction);
                    }
                    break;
                case OpcodeValue.LEQ: // Less or Equal
                    switch ((OpcodeTypeValue)VirtualMachine.FetchByte())
                    {
                        case OpcodeTypeValue.Real:
                            // ReSharper disable once EqualExpressionComparison
                            Stack.Push(PSystem.Boolean(Stack.PopReal() > Stack.PopReal()));
                            break;
                        case OpcodeTypeValue.String:
                            Stack.Push(PSystem.Boolean(Array.StrCmp(Stack.Pop(), Stack.Pop()) > 0));
                            break;
                        case OpcodeTypeValue.Boolean:
                            // ReSharper disable once EqualExpressionComparison
                            Stack.Push(PSystem.Boolean((Stack.Pop() & 1) > (Stack.Pop() & 1)));
                            break;
                        case OpcodeTypeValue.Set:
                            Stack.Push(PSystem.Boolean(Set.Pop().IsImproperSubset(Set.Pop())));
                            break;
                        case OpcodeTypeValue.ByteArray:
                            Stack.Push(PSystem.Boolean(Array.ByteCmp(Stack.Pop(), Stack.Pop(),
                                                           (ushort)VirtualMachine.FetchSignedByte()) > 0));
                            break;
                        default:
                            throw new ExecutionException(ExecutionErrorCode.UnimplementedInstruction);
                    }
                    break;
                case OpcodeValue.LES: // Less
                    switch ((OpcodeTypeValue)VirtualMachine.FetchByte())
                    {
                        case OpcodeTypeValue.Real:
                            // ReSharper disable once EqualExpressionComparison
                            Stack.Push(PSystem.Boolean(Stack.PopReal() >= Stack.PopReal()));
                            break;
                        case OpcodeTypeValue.String:
                            Stack.Push(PSystem.Boolean(Array.StrCmp(Stack.Pop(), Stack.Pop()) >= 0));
                            break;
                        case OpcodeTypeValue.Boolean:
                            // ReSharper disable once EqualExpressionComparison
                            Stack.Push(PSystem.Boolean((Stack.Pop() & 1) >= (Stack.Pop() & 1)));
                            break;
                        case OpcodeTypeValue.Set:
                            Stack.Push(PSystem.Boolean(Set.Pop().IsProperSubset(Set.Pop())));
                            break;
                        case OpcodeTypeValue.ByteArray:
                            Stack.Push(PSystem.Boolean(Array.ByteCmp(Stack.Pop(), Stack.Pop(),
                                                           (ushort)VirtualMachine.FetchSignedByte()) >= 0));
                            break;
                        default:
                            throw new ExecutionException(ExecutionErrorCode.UnimplementedInstruction);
                    }
                    break;
                case OpcodeValue.GEQ: // Greater or Equal
                    switch ((OpcodeTypeValue)VirtualMachine.FetchByte())
                    {
                        case OpcodeTypeValue.Real:
                            // ReSharper disable once EqualExpressionComparison
                            Stack.Push(PSystem.Boolean(Stack.PopReal() < Stack.PopReal()));
                            break;
                        case OpcodeTypeValue.String:
                            Stack.Push(PSystem.Boolean(Array.StrCmp(Stack.Pop(), Stack.Pop()) < 0));
                            break;
                        case OpcodeTypeValue.Boolean:
                            // ReSharper disable once EqualExpressionComparison
                            Stack.Push(PSystem.Boolean((Stack.Pop() & 1) < (Stack.Pop() & 1)));
                            break;
                        case OpcodeTypeValue.Set:
                            {
                                var subset = Set.Pop();
                                Stack.Push(PSystem.Boolean(Set.Pop().IsImproperSubset(subset)));
                            }
                            break;
                        case OpcodeTypeValue.ByteArray:
                            Stack.Push(PSystem.Boolean(Array.ByteCmp(Stack.Pop(), Stack.Pop(),
                                                           (ushort)VirtualMachine.FetchSignedByte()) < 0));
                            break;
                        default:
                            throw new ExecutionException(ExecutionErrorCode.UnimplementedInstruction);
                    }
                    break;
                case OpcodeValue.GRT: // Greater
                    switch ((OpcodeTypeValue)VirtualMachine.FetchByte())
                    {
                        case OpcodeTypeValue.Real:
                            // ReSharper disable once EqualExpressionComparison
                            Stack.Push(PSystem.Boolean(Stack.PopReal() <= Stack.PopReal()));
                            break;
                        case OpcodeTypeValue.String:
                            Stack.Push(PSystem.Boolean(Array.StrCmp(Stack.Pop(), Stack.Pop()) <= 0));
                            break;
                        case OpcodeTypeValue.Boolean:
                            // ReSharper disable once EqualExpressionComparison
                            Stack.Push(PSystem.Boolean((Stack.Pop() & 1) <= (Stack.Pop() & 1)));
                            break;
                        case OpcodeTypeValue.Set:
                            {
                                var subset = Set.Pop();
                                Stack.Push(PSystem.Boolean(Set.Pop().IsProperSubset(subset)));
                            }
                            break;
                        case OpcodeTypeValue.ByteArray:
                            Stack.Push(PSystem.Boolean(Array.ByteCmp(Stack.Pop(), Stack.Pop(),
                                                           (ushort)VirtualMachine.FetchSignedByte()) <= 0));
                            break;
                        default:
                            throw new ExecutionException(ExecutionErrorCode.UnimplementedInstruction);
                    }
                    break;
                // TOS arithmaetic: logical
                case OpcodeValue.LAND: // Logical And
                    Stack.Push((ushort)(Stack.Pop() & Stack.Pop()));
                    break;
                case OpcodeValue.LOR: // Logical Or
                    Stack.Push((ushort)(Stack.Pop() | Stack.Pop()));
                    break;
                case OpcodeValue.LNOT: // Logical Not
                    Stack.Push((ushort)~Stack.Pop());
                    break;
                // Sets   
                case OpcodeValue.ADJ: // Adjust set
                    {
                        var p1 = VirtualMachine.FetchByte();
                        if (p1 != Memory.Read(VirtualMachine.StackPointer))
                            Set.Pop().Adjust(p1).Push();
                        if (p1 != Stack.Pop())
                            VirtualMachine.Panic("adj failure");
                    }
                    break;
                case OpcodeValue.SGS: // Singleton Set
                    {
                        var w = Stack.Pop();
                        if (w < 0x200)
                        {
                            var size = w >> 4 + 1;
                            for (var i = 0; i < size; i++)
                                Stack.Push(0);
                            var addr = VirtualMachine.StackPointer.Index((ushort)(w >> 4));
                            Memory.Write(addr, (ushort)(Memory.Read(addr) | 1 << w % 16));
                            Stack.Push(size);
                        }
                        else
                            throw new ExecutionException(ExecutionErrorCode.InvalidIndex);
                    }
                    break;
                case OpcodeValue.SRS: // Subrange Set
                    {
                        var p1 = Stack.Pop();
                        var p2 = Stack.Pop();
                        if (p1 < 0x200 && p2 < 0x200)
                            if (p2 > p1)
                                Stack.Push(0);
                            else
                            {
                                var size = p1 >> 4 + 1;
                                for (var i = 0; i < size; i++)
                                    Stack.Push(0);
                                while (p2 <= p1)
                                {
                                    var addr = VirtualMachine.StackPointer.Index((ushort)(p2 >> 4));
                                    Memory.Write(addr, (ushort)(Memory.Read(addr) | 1 << p2 % 16));
                                    p2++;
                                }
                                Stack.Push(size);
                            }
                        else
                            throw new ExecutionException(ExecutionErrorCode.InvalidIndex);
                    }
                    break;
                case OpcodeValue.INN: // Set In Operation
                    {
                        var size = Stack.Pop();
                        var addr = VirtualMachine.StackPointer;
                        VirtualMachine.StackPointer = VirtualMachine.StackPointer.Index(size);
                        var val = Stack.Pop();
                        Stack.Push(val >= size << 4
                            ? PSystem.Boolean(false)
                            : PSystem.Boolean((Memory.Read(addr.Index((ushort)(val >> 4))) & 1 << val % 16)
                                              != 0));
                    }
                    break;
                case OpcodeValue.UNI: // Set Union
                    {
                        var set = Set.Pop();
                        var size = Stack.Pop();
                        if (size > set.Size)
                            set = set.Adjust(size);
                        var i = 0;
                        for (; i < size; i++)
                            set.Data[i] |= Stack.Pop();
                        set.Push();
                    }
                    break;
                case OpcodeValue.INT: // Set Intersection
                    {
                        var set = Set.Pop();
                        var size = Stack.Pop();
                        if (size > set.Size)
                            set = set.Adjust(size);
                        var i = 0;
                        for (; i < size; i++)
                            set.Data[i] &= Stack.Pop();
                        while (i < set.Size)
                            set.Data[i++] = 0;
                        set.Push();
                    }
                    break;
                case OpcodeValue.DIF: // Set Difference
                    {
                        var set = Set.Pop();
                        var size = Stack.Pop();
                        if (size > set.Size)
                            set.Adjust(size);
                        var i = 0;
                        for (; i < size; i++)
                            set.Data[i] = (ushort)(Stack.Pop() & ~set.Data[i]);
                        while (i < set.Size)
                            set.Data[i++] = 0;
                        set.Push();
                    }
                    break;
                // Jumps   
                case OpcodeValue.UJP: // Unconditional Jump
                    {
                        var w = VirtualMachine.GetJumpTarget((sbyte)VirtualMachine.FetchByte());
                        VirtualMachine.InterpreterProgramCounter = w;
                    }
                    break;
                case OpcodeValue.FJP: // False Jump
                    {
                        var p1 = VirtualMachine.FetchByte();
                        if ((Stack.Pop() & 1) == 1)
                            break;
                        var w = VirtualMachine.GetJumpTarget((sbyte)p1);
                        VirtualMachine.InterpreterProgramCounter = w;
                    }
                    break;
                case OpcodeValue.EFJ: // Equal False Jump
                    {
                        var p1 = VirtualMachine.FetchByte();
                        // ReSharper disable once EqualExpressionComparison
                        if (Stack.Pop() != Stack.Pop())
                            VirtualMachine.InterpreterProgramCounter = VirtualMachine.GetJumpTarget((sbyte)p1);
                    }
                    break;
                case OpcodeValue.NFJ: // Not Equal False Jump
                    {
                        var p1 = VirtualMachine.FetchByte();
                        // ReSharper disable once EqualExpressionComparison
                        if (Stack.Pop() == Stack.Pop())
                            VirtualMachine.InterpreterProgramCounter = VirtualMachine.GetJumpTarget((sbyte)p1);
                    }
                    break;
                case OpcodeValue.XJP: // Case Jump
                    {
                        VirtualMachine.InterpreterProgramCounter =
                            (ushort)(VirtualMachine.InterpreterProgramCounter + 1 & ~1);
                        // should these be native word fetches?
                        var lo = (short)VirtualMachine.FetchWord();
                        var hi = (short)VirtualMachine.FetchWord();
                        var value = Stack.PopInteger();
                        if (value >= lo && value <= hi)
                        {
                            VirtualMachine.InterpreterProgramCounter =
                                (ushort)(VirtualMachine.InterpreterProgramCounter + 2 * (value - lo) + 2);
                            VirtualMachine.InterpreterProgramCounter -=
                                Memory.Read(
                                    VirtualMachine.InterpreterProgramCounterBase.Index(
                                        (short)(VirtualMachine.InterpreterProgramCounter / 2)));
                        }
                    }
                    break;
                case OpcodeValue.CLP: // Call Local Procedure
                    VirtualMachine.Call(VirtualMachine.Segment, VirtualMachine.FetchByte(),
                        VirtualMachine.MarkStackPointer);
                    break;
                case OpcodeValue.CGP: // Call Global Procedure
                    VirtualMachine.Call(VirtualMachine.Segment, VirtualMachine.FetchByte(), VirtualMachine.Base);
                    break;
                case OpcodeValue.CIP: // Call Intermediate Procedure
                    {
                        var p1 = VirtualMachine.FetchByte();
                        VirtualMachine.Call(VirtualMachine.Segment, p1,
                            VirtualMachine.GetStaticLink(VirtualMachine.Segment, p1));
                    }
                    break;
                case OpcodeValue.CBP: // Call Base Procedure 
                    VirtualMachine.Call(VirtualMachine.Segment, VirtualMachine.FetchByte(), VirtualMachine.baseMp);
                    break;
                case OpcodeValue.CXP: // Call External Procedure
                    {
                        var p1 = VirtualMachine.FetchByte();
                        var p2 = VirtualMachine.FetchByte();
                        if (p1 != 0) // Not for segment 0
                            VirtualMachine.CspLoadSegment(p1);
                        var w = VirtualMachine.SegmentDictionary[p1].SegmentPointer;
                        if (VirtualMachine.Call(w, p2, VirtualMachine.GetStaticLink(w, p2)) != 0)
                            VirtualMachine.CspUnloadSegment(p1);
                    }
                    break;
                case OpcodeValue.RNP: // Return from Non-Base Procedure
                    VirtualMachine.StackPointer =
                        Memory.Read(VirtualMachine.MarkStackPointer.Index((short)MethodStack.StackPointer));
                    VirtualMachine.Return(VirtualMachine.FetchByte());
                    break;
                case OpcodeValue.RBP: // Return from Base Procedure
                    VirtualMachine.StackPointer =
                        Memory.Read(VirtualMachine.MarkStackPointer.Index((short)MethodStack.StackPointer));
                    VirtualMachine.Base = Stack.Pop();
                    Memory.Write(PSystem.StackBase, VirtualMachine.Base);
                    if (VirtualMachine.Base < VirtualMachine.ProgramStackPointer ||
                        VirtualMachine.Base > VirtualMachine.baseMp)
                        VirtualMachine.Panic("RBP: Base {0:X4} out of range", VirtualMachine.Base);
                    VirtualMachine.Return(VirtualMachine.FetchByte());
                    break;
                case OpcodeValue.CSP:
                    if (!VirtualMachine.CallStandardProcedure((StandardCall)VirtualMachine.FetchByte()))
                        return false;
                    break;
                case OpcodeValue.BPT:
                    {
                        var p1 = VirtualMachine.FetchSignedByte();
                        if (Memory.Read(PSystem.BugState) >= 3
                            || p1 == Memory.Read(PSystem.Breakpoints0)
                            || p1 == Memory.Read(PSystem.Breakpoints1)
                            || p1 == Memory.Read(PSystem.Breakpoints2)
                            || p1 == Memory.Read(PSystem.Breakpoints3))
                            throw new ExecutionException(ExecutionErrorCode.Breakpoint);
                    }
                    break;
                case OpcodeValue.XIT:
                    return false;
                case OpcodeValue.NOP:
                    break;
                default:
                    throw new ExecutionException(ExecutionErrorCode.UnimplementedInstruction);
            }
            return true;
        }

        private static ushort LookupFile(ushort unit, string name)
        {
            DiskIO.Read(unit, VirtualMachine.NewPointer, 0, 2048, 2);
            if (Memory.Read(PSystem.IOResultPointer) != 0)
                return 0;

            for (var i = 0; i < Memory.Read(VirtualMachine.NewPointer.Index(8)); i++)
            {
                var entry = VirtualMachine.NewPointer.Index((ushort)(13 + 13 * i));
                var mismatch = false;
                int len;
                for (len = 0; len < Memory.ReadByte(entry.Index(3), 0); len++)
                {
                    if (char.ToUpperInvariant((char)Memory.ReadByte(entry.Index(3), (short)(len + 1)))
                        == char.ToUpperInvariant(name[len]))
                        continue;
                    mismatch = true;
                    break;
                }
                if (mismatch || len != name.Length)
                    continue;
                return Memory.Read(entry.Index(0));
            }

            return 0;
        }

        /// <summary>
        ///     Segment 0 is split, and the pointer in the Procedure Dictionary have
        ///     been corrected so that after loading the two halves correctly to the
        ///     respective "correct" the address pointer is.
        ///     This routine corrects the pointer in the segment dictionary.  In
        ///     addition, it determines the first address, in which the second half
        ///     really should be loaded.  Then an offset is determined by the pointers
        ///     must be corrected in the second half.
        /// </summary>
        /// <param name="loadAddr"></param>
        private static void FixupSeg0(ushort loadAddr)
        {
            var seg = VirtualMachine.SegmentDictionary[0].SegmentPointer;
            var segBase = VirtualMachine.SegmentDictionary[0].SegBase;
            ushort addr = 0;
            for (var i = 1; i <= VirtualMachine.GetSegmentProcedureCount(seg); i++)
            {
                var jtab = VirtualMachine.GetProcedureActivationRecord(seg, i);
                if (jtab < segBase && jtab > addr)
                    addr = jtab;
            }
            if (addr == 0)
                return;
            addr = addr.Index(1);
            var offset = (ushort)(loadAddr - addr);
            if (offset == 0)
                return;
            for (var i = 1; i <= VirtualMachine.GetSegmentProcedureCount(seg); i++)
            {
                var jtab = VirtualMachine.GetProcedureActivationRecord(seg, i);
                if (jtab >= segBase)
                    continue;
                addr = seg.Index(-1);
#if WORD_MEMORY
                Memory.Write(addr, (ushort)(Memory.Read(addr) - 2 * offset));
#else
                Memory.Wr(addr, (ushort) (Memory.Rd(addr) - offset));
#endif
            }
        }

        private static void Load(ushort unit, ushort block)
        {
            DiskIO.Read(unit, VirtualMachine.NewPointer, 0, 0x200, block);
            if (Memory.Read(PSystem.IOResultPointer) != 0)
                return;

            // create segment dictionary
            for (var i = 0; i < 16; i++)
            {
                var codeAddr = (ushort)(Memory.Read(VirtualMachine.NewPointer.Index((ushort)(i << 1))) + block);
                var codeLeng = Memory.Read(VirtualMachine.NewPointer.Index((ushort)((i << 1) + 1)));
                var nameChars = new char[8];
                for (var j = 0; j < 4; j++)
                {
                    var dc = Memory.Read(VirtualMachine.NewPointer.Index((ushort)((i << 2) + 0x20 + j)));
                    nameChars[j * 2] = (char)(dc & 0xFF);
                    nameChars[j * 2 + 1] = (char)(dc >> 8);
                }
                var segInfo = Memory.Read(VirtualMachine.NewPointer.Index((ushort)(i + 0x80)));

                Debug.Assert((codeLeng & 1) == 0);
                var segNum = (ushort)(segInfo & 0xFF);
                var si = VirtualMachine.SegmentDictionary[segNum];
                si.Name = new(nameChars);
                //si.Id = segNum;
                VirtualMachine.SegmentDictionary[segNum] = si;
                if (codeLeng == 0 || codeAddr == 0)
                    continue;
                if ((segInfo & 0xF00) != 0)
                {
                    Memory.Write(PSystem.SegmentUnitPointer(segNum), unit);
                    Memory.Write(PSystem.SegmentBlockPointer(segNum), codeAddr);
                    Memory.Write(PSystem.SegmentSizePointer(segNum), codeLeng);
                }
                if (segNum != 0 && VirtualMachine.SegmentDictionary[0].SegBase != 0)
                    continue;
                var sd0 = VirtualMachine.SegmentDictionary[0];
                if (sd0.UseCount == 0)
                {
                    sd0.UseCount = 1;
                    sd0.OldKp = VirtualMachine.ProgramStackPointer;
                    sd0.SegmentPointer = VirtualMachine.ProgramStackPointer.Index(-1);
#if WORD_MEMORY
                    VirtualMachine.ProgramStackPointer = (ushort)(VirtualMachine.ProgramStackPointer - codeLeng / 2);
#else
                    Kp -= codeLeng;
#endif
                    sd0.SegBase = VirtualMachine.ProgramStackPointer;
                    VirtualMachine.SegmentDictionary[0] = sd0;
                    DiskIO.Read(unit, VirtualMachine.ProgramStackPointer, 0, codeLeng, codeAddr);
                }
                else
                {
                    VirtualMachine.FixupSeg0(VirtualMachine.ProgramStackPointer);
#if WORD_MEMORY
                    VirtualMachine.ProgramStackPointer = (ushort)(VirtualMachine.ProgramStackPointer - codeLeng / 2);
#else
                    Kp -= codeLeng;
#endif
                    DiskIO.Read(unit, VirtualMachine.ProgramStackPointer, 0, codeLeng, codeAddr);
                }
            }
        }

        private static void LoadSystem(ref ushort rootUnit, string fileName)
        {
            ushort unit;
            var tempRoot = rootUnit;
            var block = VirtualMachine.LookupFile(tempRoot, fileName);
            if (block != 0)
                unit = tempRoot;
            else
                for (unit = 4; unit < DiskIO.MaxUnits; unit++)
                {
                    if (unit == 6)
                        unit = 9;
                    block = VirtualMachine.LookupFile(unit, fileName);
                    if (block != 0)
                        break;
                }
            if (block == 0 || unit == 0)
                VirtualMachine.Panic("file \"{0}\": not found", fileName);

            VirtualMachine.Load(unit, block);
            if (Memory.Read(PSystem.IOResultPointer) != 0)
                VirtualMachine.Panic("file \"{0}\": unit {1}, block {2}: IOError {3}",
                    fileName, unit, block, Memory.Read(PSystem.IOResultPointer));


            if (VirtualMachine.SegmentDictionary[0].UseCount == 0)
                VirtualMachine.Panic("file \"{0}\": not a valid system file, no segment 0",
                    fileName);

            VirtualMachine.Call(VirtualMachine.SegmentDictionary[0].SegmentPointer, 1, 0);
            rootUnit = unit;
        }

        /// <summary>
        ///     The data_read function is used to emulate the memory occupied by the
        ///     THEDATE variable in the system variables.  In this way, the system's
        ///     idea of th ecurrent date will always be up-to-date without manual
        ///     intervention.
        /// </summary>
        /// <param name="addr">The address being read</param>
        /// <returns>The word or byte value requested.</returns>
        private static ushort DateRead(ushort addr)
        {
            var now = DateTime.Now;
            var value = (ushort)(now.Year % 100 << 9
                                  | now.Month + 1
                                  | now.Day << 4);

#if WORD_MEMORY
            return value;
#else
            if ((addr & 1) != 0 == (Memory.Endianness == Endianness.Big))
                return (ushort)(value & 0xFF);
            return (ushort)(value >> 8);
#endif
        }

        private static ushort CrtInfoWidthRead(ushort addr)
        {
            var value = (ushort)Console.WindowWidth;
#if WORD_MEMORY
            return value;
#else
            if ((addr & 1) != 0 == (Memory.Endianness == Endianness.Big))
                return (ushort)(value & 0xFF);
            return (ushort)(value >> 8);
#endif
        }

        private static ushort CrtInfoHeightRead(ushort addr)
        {
            var value = (ushort)Console.WindowHeight;
#if WORD_MEMORY
            return value;
#else
            if ((addr & 1) != 0 == (Memory.Endianness == Endianness.Big))
                return (ushort)(value & 0xFF);
            return (ushort)(value >> 8);
#endif
        }

        [Flags]
        private enum MiscInfo : ushort
        {
            //None,
            HasClock,
            Has8510A,
            HasLcCrt = 4,
            HasXyCrt = 8,

            //SlowTerm = 16,
            //Editor = 32,
            //NoBreak = 64,
            //UserKind = 128,
            IsFlipped = 256,
            WordMachine = 512
        }

        private static ushort MiscInfoRead(ushort addr)
        {
            var value = MiscInfo.HasXyCrt
                        | MiscInfo.HasLcCrt
                        | MiscInfo.Has8510A
                        | MiscInfo.HasClock;

            if (Memory.Endianness == Endianness.Big)
                value |= MiscInfo.IsFlipped;

#if WORD_MEMORY
            value |= MiscInfo.WordMachine;
            return (ushort)value;
#else
            return (((addrs & 1) == 1) == (Memory.Endianness == Endianness.Big))
                ? (ushort)((ushort)value & 0xFF)
                : (ushort)((ushort)value >> 8);
#endif
        }

        private static void MiscInfoWrite(ushort addr, ushort value)
        {
#if WORD_MEMORY
            //value |= MiscInfo.WordMachine;
            //return (ushort)value;
#else
            if (((addrs & 1) == 1) == (Memory.Endianness == Endianness.Big))
                ? (ushort)((ushort)value & 0xFF)
                : (ushort)((ushort)value >> 8);
#endif
        }

        private static void Usage()
        {
            var prog = Assembly.GetEntryAssembly()?.FullName;
            Console.Error.WriteLine("Usage: {0} [ <option>... ]", prog);
            Console.Error.WriteLine("       {0} -V", prog);
            Environment.Exit(1);
        }

        public static void Main(string[] args)
        {
            const string systemName = "SYSTEM.PASCAL";
            var unit = 4;
            var dump = false;

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                switch (arg)
                {
                    case "-w":
                    case "-r":
                    case "-f":
                        DiskIO.DiskMode mode;
                        switch (arg[1])
                        {
                            case 'w':
                                mode = DiskIO.DiskMode.ReadWrite;
                                break;
                            case 'r':
                                mode = DiskIO.DiskMode.ReadOnly;
                                break;
                            default:
                                mode = DiskIO.DiskMode.Forget;
                                break;
                        }
                        DiskIO.Mount((ushort)unit, args[++i], mode);
                        unit++;
                        if (unit == 6)
                            unit = 9;
                        break;
                    case "-d":
                        dump = true;
                        break;
                    case "-g":
                        VirtualMachine.traceLevel = 0x7FFF;
                        break;
                }
            }

            VirtualMachine.NewPointer = VirtualMachine.heapBottom;
            VirtualMachine.ProgramStackPointer = VirtualMachine.kpTop.Index(-VirtualMachine.systemCommuincationSize);
            VirtualMachine.SystemCommunicationPointer = VirtualMachine.ProgramStackPointer;
            VirtualMachine.StackPointer = VirtualMachine.spTop;
            VirtualMachine.MarkStackPointer = VirtualMachine.ProgramStackPointer;

            var rootUnit = (ushort)4;
            VirtualMachine.LoadSystem(ref rootUnit, systemName);
            VirtualMachine.baseMp = VirtualMachine.MarkStackPointer;
            VirtualMachine.StackPointer = VirtualMachine.StackPointer.Index(-1);

            Memory.Write(VirtualMachine.LocalAddress(1), VirtualMachine.SystemCommunicationPointer);

            // emulation

            if (VirtualMachine.GetLocalVariableSize(VirtualMachine.JumpTable) > 67 * 2)
                Memory.SetEmulateWord(VirtualMachine.LocalAddress(67), VirtualMachine.DateRead, null);

            Memory.SetEmulateWord(PSystem.CrtInfoWidthPointer, VirtualMachine.CrtInfoWidthRead, null);
            Memory.SetEmulateWord(PSystem.CrtInfoHeightPointer, VirtualMachine.CrtInfoHeightRead, null);

            Memory.SetEmulateWord(PSystem.MiscInfoPointer, VirtualMachine.MiscInfoRead, null);

            // end emulation

            Memory.Write(PSystem.GlobalDirectoryPointer, 0);

            Memory.Write(PSystem.SystemUnitPointer, rootUnit);

            if (!dump)
                VirtualMachine.Processor();
            else
                VirtualMachine.Disassemble();

            while (unit-- > 4)
            {
                if (unit == 8)
                    unit = 5;
                DiskIO.Unmount((ushort)unit);
            }
        }

        private static void Disassemble()
        {
            using StreamWriter sw = new("dis.txt");
            IndentedTextWriter writer = new(sw, " ");
            writer.WriteLine("SYSTEM.PASCAL:");
            writer.Indent++;

            for (var i = 0; i < 0x20; i++)
            {
                var si = VirtualMachine.SegmentDictionary[i];
                //if (si.Name == null)
                //    continue;
                var segUnit = Memory.Read(PSystem.SegmentUnitPointer((ushort)i));
                var segBlock = Memory.Read(PSystem.SegmentBlockPointer((ushort)i));
                var segSize = Memory.Read(PSystem.SegmentSizePointer((ushort)i));


                writer.WriteLine("Segment {0}: ", si.Name.Trim());
                writer.Indent++;
                writer.WriteLine("Id: " + i);
                writer.WriteLine("Unit: " + segUnit);
                writer.WriteLine("Address: 0x{0:X4}", segBlock << 9);
                writer.WriteLine("Length: 0x{0:X4}", segSize);
                VirtualMachine.DisassembleSegment((ushort)i, writer);
                writer.Indent--;
            }
        }

        private static void DisassembleSegment(ushort segNo, IndentedTextWriter writer)
        {
            VirtualMachine.CspLoadSegment(segNo);
            var si = VirtualMachine.SegmentDictionary[segNo];
            var procs = VirtualMachine.GetSegmentProcedureCount(si.SegmentPointer);
            Debug.Assert(VirtualMachine.GetSegmentNumber(si.SegmentPointer) == segNo);

            if (procs != 0)
            {
                writer.WriteLine("Procedures:");
                writer.Indent++;
            }

            for (var i = 1; i <= procs; i++)
                VirtualMachine.DisassembleProcedure(si, i, writer);

            if (procs != 0)
                writer.Indent--;

            VirtualMachine.CspUnloadSegment((byte)segNo);
        }

        private static void DisassembleProcedure(SegmentInfo si, int i, IndentedTextWriter writer)
        {
            var seg = si.SegmentPointer;
            writer.WriteLine("proc {0}_{1:X}:", si.Name, i);
            writer.Indent++;
            var jTab = VirtualMachine.GetProcedureActivationRecord(seg, i);
            var dataSize = VirtualMachine.GetLocalVariableSize(jTab);
            var paramSize = VirtualMachine.GetParameterSize(jTab);
            writer.WriteLine("Lexical Level: " + VirtualMachine.GetLexLevel(jTab));
            writer.WriteLine("Local Count: " + dataSize / 2);
            writer.WriteLine("Parameter Count: " + paramSize / 2);
            //writer.WriteLine("Entry: 0x{0:X4}", ProcBase(jTab));
            //writer.WriteLine("Exit: 0x{0:X4}", ProcExitIpc(jTab));
            writer.WriteLine("PCode:");
            writer.Indent++;
            VirtualMachine.DisassemblePcode(si, i, writer);
            writer.Indent -= 2;
        }

        private static void DisassemblePcode(SegmentInfo si, int procId, IndentedTextWriter writer)
        {
            var jTab = VirtualMachine.GetProcedureActivationRecord(si.SegmentPointer, procId);
            VirtualMachine.InterpreterProgramCounterBase = VirtualMachine.GetProcedureInstructionPointerBase(jTab);
            VirtualMachine.InterpreterProgramCounter = 0;
            var procExit = VirtualMachine.GetExitIpc(jTab);
            while (VirtualMachine.InterpreterProgramCounter <= procExit)
            {
                var opcodeInfo = OpcodeInfo.OpCodes[VirtualMachine.FetchByte()];
                writer.Write("0x{0:X4}: {1}", VirtualMachine.InterpreterProgramCounter - 1,
                    Enum.GetName(typeof(OpcodeValue), opcodeInfo.Code));
                opcodeInfo.Write(opcodeInfo, si, jTab, writer);
                writer.WriteLine(" ;" + opcodeInfo.Comment);
            }
        }

        public static string Escape(string raw)
        {
            StringBuilder sb = new(raw.Length);
            foreach (var c in raw)
                sb.Append(c switch {'\t' => "\\t", '\n' => "\\n", '\r' => "\\r", _ => c.ToString()});
            return sb.ToString();
        }

        private static void ExecutionExceptionHandler(ExecutionException ee)
        {
            try
            {
                var newSeg = VirtualMachine.SegmentDictionary[0].SegmentPointer;

                Memory.Write(PSystem.Error, (ushort)ee.Code);
#if BOMBPROC
                Memory.Wr(PSystem.BombProc, ProcNumber(JTab));
#endif
#if BOMBSEG
                Memory.Wr(PSystem.BombSeg, SegNumber(Seg));
#endif
                Memory.Write(PSystem.BombIpc, VirtualMachine.currentIpc);
                Memory.Write(PSystem.MiscInfoPointer, (ushort)(Memory.Read(PSystem.MiscInfoPointer) | 1 << 10));

                VirtualMachine.Call(newSeg, 2, VirtualMachine.baseMp);

                Memory.Write(PSystem.BombP, VirtualMachine.MarkStackPointer);
            }
            catch (ExecutionException)
            {
                VirtualMachine.Panic("XeqError: recursion");
            }
        }

        //private static void TermClose()
        //{
        //    throw new NotImplementedException();
        //}

        private static readonly float[] powerOfTen =
        {
            1e0f, 1e1f, 1e2f, 1e3f, 1e4f, 1e5f,
            1e6f, 1e7f, 1e8f, 1e9f, 1e10f, 1e11f,
            1e12f, 1e13f, 1e14f, 1e15f, 1e16f, 1e17f,
            1e18f, 1e19f, 1e20f, 1e21f, 1e22f, 1e23f,
            1e24f, 1e25f, 1e26f, 1e27f, 1e28f, 1e29f,
            1e30f, 1e31f, 1e32f, 1e33f, 1e34f, 1e35f,
            1e36f, 1e37f, 1e38f
        };

        private static bool CallStandardProcedure(StandardCall standardCall)
        {
            switch (standardCall)
            {
                case StandardCall.CSP_IOC:
                    if (Memory.Read(PSystem.IOResultPointer) != 0)
                        throw new ExecutionException(ExecutionErrorCode.UserIO);
                    break;
                case StandardCall.CSP_NEW:
                    VirtualMachine.ClearGlobalDirectoryPointer();
                    {
                        var w = Stack.Pop();
                        Memory.Write(Stack.Pop(), VirtualMachine.NewPointer);
                        VirtualMachine.NewPointer = VirtualMachine.NewPointer.Index(w);
                    }
                    VirtualMachine.StackCheck();
                    break;
                case StandardCall.CSP_MVL:
                    //Moveleft(src, dst, numbytes).  Tos
                    //(numbytes) gives the number of bytes to move.
                    //Tos-1  (dst) is a byte pointer to the
                    //destination array's first byte which will
                    //receive a moved byte.  Tos-2  (src) is a byte
                    //pointer to the source array's first byte which
                    //will be moved.  Copy  tos  bytes from the
                    //source array to the destination array,
                    //proceeding to the right through both arrays.
                    {
                        var len = Stack.PopInteger();
                        var dstOffset = Stack.PopInteger();
                        var dst = Stack.Pop();
                        var srcOffset = Stack.PopInteger();
                        var src = Stack.Pop();
                        VirtualMachine.MoveLeft(dst, dstOffset, src, srcOffset, len);
                    }
                    break;
                case StandardCall.CSP_MVR:
                    //Moveright(src, dst, numbytes).  Tos
                    //(numbytes) gives the number of bytes to move.
                    //Tos-1  (dst) is a byte pointer to the
                    //destination array's first byte which will
                    //receive a moved byte.  Tos-2  (src) is a byte
                    //pointer to the source array's first byte which
                    //will be moved.  Copy  tos  bytes from the
                    //source array to the destination array,
                    //proceeding to the left through both arrays.
                    {
                        var len = Stack.PopInteger();
                        var dstOffset = Stack.PopInteger();
                        var dst = Stack.Pop();
                        var srcOffset = Stack.PopInteger();
                        var src = Stack.Pop();
                        VirtualMachine.MoveRight(dst, dstOffset, src, srcOffset, len);
                    }
                    break;
                case StandardCall.CSP_XIT:
                    //Exit from procedure.  Tos is the
                    //procedure number,  tos-I  is the segment
                    //number.  First, set IPC to point to
                    //the exit code of the currently
                    //executing procedure.  Then, if the
                    //current procedure is the one to exit
                    //from, return control to the instruction
                    //fetch loop.

                    //Otherwise, change the IPC of each
                    //Markstack to point to the exit code of the
                    //procedure that invoked it, until the desired
                    //procedure is found.

                    //If at any time the saved IPC of main
                    //body of the operating system is about to
                    //be changed, give an execution error.
                    {
                        var procNo = Stack.Pop();
                        var segNo = Stack.Pop();
                        var xMp = VirtualMachine.MarkStackPointer;
                        var xSeg = VirtualMachine.Segment;
                        var xJTab = VirtualMachine.JumpTable;

                        VirtualMachine.InterpreterProgramCounter = VirtualMachine.GetExitIpc(xJTab);
                        while (VirtualMachine.GetProcedureNumber(xJTab) != procNo ||
                               VirtualMachine.GetSegmentNumber(xSeg) != segNo)
                        {
                            if (xMp == 0
                                || (xJTab = Memory.Read(xMp.Index((short)MethodStack.JumpTable))) == 0
                                || (xSeg = Memory.Read(xMp.Index((short)MethodStack.SegmentId))) == 0)
                                throw new ExecutionException(ExecutionErrorCode.ExitFromUncalledProcedure);
                            Memory.Write(xMp.Index((short)MethodStack.InterpreterProgramCounter),
                                VirtualMachine.GetExitIpc(xJTab));
                            xMp = Memory.Read(xMp.Index((short)MethodStack.Dyn));
                        }
                    }
                    break;
                case StandardCall.CSP_UREAD:
                    {
                        var w6 = Stack.Pop();
                        var w5 = Stack.Pop();
                        var w4 = Stack.Pop();
                        var w3 = Stack.Pop();
                        var w2 = Stack.Pop();
                        var w1 = Stack.Pop();
                        VirtualMachine.UnitRead(w1, w2, (short)w3, w4, w5, w6);
                    }
                    break;
                case StandardCall.CSP_UWRITE:
                    {
                        var w6 = Stack.Pop();
                        var w5 = Stack.Pop();
                        var w4 = Stack.Pop();
                        var w3 = Stack.Pop();
                        var w2 = Stack.Pop();
                        var w1 = Stack.Pop();
                        VirtualMachine.UnitWrite(w1, w2, (short)w3, w4, w5, w6);
                    }
                    break;
                case StandardCall.CSP_TIM:
                    {
                        var epoch = DateTime.Now - new DateTime(1970, 1, 1);
                        var sec = (uint)(epoch.TotalMilliseconds * 60 / 1000);
                        Memory.Write(Stack.Pop(), (ushort)(sec & 0xFFFF));
                        Memory.Write(PSystem.LowTime, (ushort)(sec & 0xFFFF));
                        Memory.Write(Stack.Pop(), (ushort)(sec >> 16 & 0xFFFF));
                        Memory.Write(PSystem.HighTime, (ushort)(sec >> 16 & 0xFFFF));
                    }
                    break;
                case StandardCall.CSP_IDS:
                    //Idsearch.  Used by the Compiler to parse
                    //reserved words and identifiers.
                    Search.CspIdSearch(Stack.Pop(), Stack.Pop());
                    break;
                case StandardCall.CSP_TRS:
                    //Treesearch(fcp, fcp2, name).  Tos-2  (fcp)
                    //is a byte pointer to the root of a binary tree
                    //Tos  (name) is a byte pointer to a location
                    //which contains the address of an 8-character
                    //name that you wish to find or to place in the
                    //tree.  Search the tree, looking for a record
                    //with the required name.  Store the address of
                    //the last node visited, on completion of the
                    //search, into the location pointed to by
                    //byte pointer  tos-1  (fcp2), and push the
                    //result of the search:

                    //    0  if the last node was a record with
                    //       the search name,
                    //    1  if the search name should be a new
                    //       record, attaching to the last tree
                    //       node by the Right Link,
                    //   -1  if the search name should be a new
                    //       record, attaching to the last tree
                    //       node by the Left Link.

                    //This is am assembly-language binary tree
                    //search used by the Compiler.  It is fast, b
                    //does NOT do type checking on the parameters
                    //The binary tree uses nodes of type

                    //  CTP =  RECORD
                    //          NAME: PACKED ARRAY [1..8] OF CHAR;
                    //          LLINK, RLINK: ^CTP;
                    //          ...
                    //           other information
                    //          ...
                    //          END;
                    Search.CspTreeSearch(Stack.Pop(), Stack.Pop(), /* initialize with root node addr */ Stack.Pop());
                    break;
                case StandardCall.CSP_FLC:
                    //Fillchar(dst, len, char).  Tos  (char)
                    //is the source character.  Tos-1  (len) is the
                    //number of bytes in the destination array
                    //which are to be filled with the source char.
                    //Tos-2  (dst) is a byte pointer to the first
                    //byte to be filled in the destination PACKED
                    //ARRAY OF CHARacters.  Copy the character
                    //from  tos  into  tos-1  characters of the
                    //destination array.
                    {
                        var ch = Stack.Pop();
                        var len = Stack.PopInteger();
                        var offset = Stack.PopInteger();
                        var addr = Stack.Pop();
                        while (len-- > 0)
                            Memory.WriteByte(addr, offset++, (byte)ch);
                    }
                    break;
                case StandardCall.CSP_SCN:
                    //Scan(maxdisp, mask, char, start, forpast).
                    //Tos (forpast) is a two-byte quantity (usually
                    //the default integer 0) which is pushed, but
                    //later discarded without being used in this
                    //implementation.  Tos-1 (start) is a byte
                    //pointer to the first character to be
                    //scanned in a PACKED ARRAY OF CHARacters.
                    //Tos-2  (char) is the character against which
                    //each scanned character of the array is to be
                    //checked.  Tos-3  (mask) is a 0 if the check is
                    //for equality, or 1 if the check is for
                    //inequality.  Tos-4  (maxdisplacement) gives
                    //the maximum number of characters to be scanned
                    //(scan to the left if negative). If a character
                    //check yields TRUE, push the number of
                    //characters scanned (negative, if scanning to
                    //the left).  If  maxdisp  is reached before
                    //character check yields TRUE, push maxdisp
                    {
                        Stack.Pop();
                        var offset = Stack.PopInteger();
                        var bufferAddress = Stack.Pop();
                        var ch = Stack.Pop(); // search
                        var match = Stack.Pop() != 0; // 0 search for == ch; != 0 Search !=ch
                        var limit = Stack.Pop();
                        ushort res;

                        if ((limit & 0x8000) != 0)
                        {
                            limit = (ushort)(0x10000 - limit);
                            for (res = 0; res < limit; res++)
                                if (Memory.ReadByte(bufferAddress, (short)(offset - res)) != ch)
                                {
                                    if (match)
                                        break;
                                }
                                else if (!match)
                                    break;
                            Stack.Push((ushort)(0x10000 - res));
                        }
                        else
                        {
                            for (res = 0; res < limit; res++)
                                if (Memory.ReadByte(bufferAddress, (short)(offset + res)) != ch)
                                {
                                    if (match)
                                        break;
                                }
                                else if (!match)
                                    break;
                            Stack.Push(res);
                        }
                    }
                    break;
                case StandardCall.CSP_USTAT:
                    {
                        var dummy = Stack.Pop();
                        var offset = Stack.PopInteger();
                        var addr = Stack.Pop();
                        var unit = Stack.Pop();
                        VirtualMachine.UnitStat(unit, addr/*, offset, dummy*/);
                    }
                    break;
                case StandardCall.CSP_LDSEG:
                    VirtualMachine.CspLoadSegment(Stack.Pop());
                    break;
                case StandardCall.CSP_ULDSEG:
                    VirtualMachine.CspUnloadSegment((byte)Stack.Pop());
                    break;
                case StandardCall.CSP_TRC:
                    {
                        var f = Stack.PopReal();
                        Stack.Push((float)(f < 0 ? Math.Ceiling(f) : Math.Floor(f)));
                    }
                    break;
                case StandardCall.CSP_RND:
                    Stack.Push((float)Math.Round(Stack.PopReal()));
                    break;
                case StandardCall.CSP_SIN:
                    Stack.Push((float)Math.Sin(Stack.PopReal()));
                    break;
                case StandardCall.CSP_COS:
                    Stack.Push((float)Math.Cos(Stack.PopReal()));
                    break;
                case StandardCall.CSP_TAN:
                    Stack.Push((float)Math.Tan(Stack.PopReal()));
                    break;
                case StandardCall.CSP_ATAN:
                    Stack.Push((float)Math.Atan(Stack.PopReal()));
                    break;
                case StandardCall.CSP_LN:
                    {
                        var f = Stack.PopReal();
                        if (Math.Abs(f) < VirtualMachine.machineEpsilonFloat)
                            throw new ExecutionException(ExecutionErrorCode.FloatingPointMath);
                        Stack.Push((float)Math.Log(f));
                    }
                    break;
                case StandardCall.CSP_EXP:
                    Stack.Push((float)Math.Exp(Stack.PopReal()));
                    break;
                case StandardCall.CSP_SQRT:
                    {
                        var f = Stack.PopReal();
                        if (Math.Abs(f) < VirtualMachine.machineEpsilonFloat)
                            throw new ExecutionException(ExecutionErrorCode.FloatingPointMath);
                        Stack.Push((float)Math.Sqrt(f));
                    }
                    break;
                case StandardCall.CSP_MRK:
                    VirtualMachine.ClearGlobalDirectoryPointer();
                    Memory.Write(Stack.Pop(), VirtualMachine.NewPointer);
                    break;
                case StandardCall.CSP_RLS:
                    VirtualMachine.NewPointer = Memory.Read(Stack.Pop());
                    VirtualMachine.StackCheck();
                    Memory.Write(PSystem.GlobalDirectoryPointer, 0);
                    break;
                case StandardCall.CSP_IOR:
                    Stack.Push(Memory.Read(PSystem.IOResultPointer));
                    break;
                case StandardCall.CSP_UBUSY:
                    Stack.Push(VirtualMachine.UnitBusy(Stack.Pop()));
                    break;
                case StandardCall.CSP_POT:
                    {
                        var index = Stack.PopInteger();
                        if (index < 0 || index > 38)
                            Stack.Push(0f);
                        else
                            Stack.Push(VirtualMachine.powerOfTen[index]);
                    }
                    break;
                case StandardCall.CSP_UWAIT:
                    VirtualMachine.UnitWait(Stack.Pop());
                    break;
                case StandardCall.CSP_UCLEAR:
                    VirtualMachine.UnitClear(Stack.Pop());
                    break;
                case StandardCall.CSP_HLT:
                    return false;
                case StandardCall.CSP_MAV:
                    Stack.Push((ushort)((Memory.Read(PSystem.GlobalDirectoryPointer) != 0
                                             ? VirtualMachine.ProgramStackPointer -
                                               Memory.Read(PSystem.GlobalDirectoryPointer)
                                             : VirtualMachine.ProgramStackPointer - VirtualMachine.NewPointer) >> 1));

                    break;
                default:
                    throw new ExecutionException(ExecutionErrorCode.UnimplementedInstruction);
            }
            return true;
        }

        private static void UnitRead(ushort unit, ushort addr, short offset, ushort len, ushort block, ushort mode)
        {
            PSystem.IOError(0);
            switch (unit)
            {
                case 1:
                case 2:
                    while (len-- != 0)
                    {
                        var key = Console.ReadKey(unit == 2);
                        Memory.WriteByte(addr, offset++, (byte)key.KeyChar);
                    }
                    break;
                case 4:
                case 5:
                case 9:
                case 10:
                case 11:
                case 12:
                    DiskIO.Read(unit, addr, offset, len, block);
                    break;
                default:
                    PSystem.IOError(2);
                    break;
            }
        }

        private static void UnitWrite(ushort unit, ushort addr, short offset, ushort len, ushort block, ushort mode)
        {
            PSystem.IOError(0);
            switch (unit)
            {
                case 1:
                case 2:
                    while (len-- != 0)
                        Console.Write((char)Memory.ReadByte(addr, offset++));
                    break;
                case 4:
                case 5:
                case 9:
                case 10:
                case 11:
                case 12:
                    DiskIO.Read(unit, addr, offset, len, block);
                    break;
                case 6:
                    throw new ExecutionException(ExecutionErrorCode.UnimplementedInstruction);
                case 13:
                    switch (len)
                    {
                        case 0: // hard reset
                            throw new ExecutionException(ExecutionErrorCode.UnimplementedInstruction);
                        case 10:

                            break;
                        default:
                            PSystem.IOError(3);
                            break;
                    }
                    break;
                default:
                    PSystem.IOError(2);
                    break;
            }
        }

        private static void UnitClear(ushort unit)
        {
            PSystem.IOError(0);
            switch (unit)
            {
                case 1:
                case 2:
                    break;

                case 4:
                case 5:
                case 11:
                case 12:
                    DiskIO.Clear(unit);
                    break;
                case 3:
                case 6:
                    throw new ExecutionException(ExecutionErrorCode.UnimplementedInstruction);
                default:
                    PSystem.IOError(9);
                    break;
            }
        }

        private static void UnitStat(ushort unit, ushort addr/*, short offset, ushort dummy*/)
        {
            PSystem.IOError(0);
            switch (unit)
            {
                case 1:
                case 2:
                    Memory.Write(addr, 1);
                    break;

                case 4:
                case 5:
                case 11:
                case 12:
                    DiskIO.Stat(unit);
                    break;
                default:
                    PSystem.IOError(9);
                    break;
            }
        }

        private static ushort UnitBusy(ushort unit)
        {
            PSystem.IOError(0);
            return 0;
        }

        private static void UnitWait(ushort unit) => PSystem.IOError(0);

        public static ushort FetchBig()
        {
            var b = VirtualMachine.FetchByte();
            if ((b & 0x80) == 0)
                return b;
            return (ushort)((b & 0x7F) << 8 | VirtualMachine.FetchByte());
        }

        /// <summary>
        ///     Returns a pointer to the first instruction of a procedure.
        /// </summary>
        /// <param name="jTab"></param>
        /// <returns></returns>
        public static ushort GetProcedureInstructionPointerBase(ushort jTab)
        {
            Memory.PointerCheck(jTab);
            return Memory.SelfRelPtr(jTab.Index(-1));
        }
    }
}