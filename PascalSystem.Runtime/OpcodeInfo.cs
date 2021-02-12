// ReSharper disable StringLiteralTypo
namespace PascalSystem.Runtime
{
    using System;
    using System.CodeDom.Compiler;
    using System.Text;

    using VM = VirtualMachine;

    internal struct OpcodeInfo
    {
        public readonly OpcodeValue Code;
        public readonly string Comment;
        private readonly string arguments;
        private string? stackBefore;
        private string? stackAfter;
        public readonly Action<OpcodeInfo, VirtualMachine.SegmentInfo, ushort, IndentedTextWriter> Write;

        public OpcodeInfo(OpcodeValue code, string comment, string arguments, string stackBefore,
            string? stackAfter = null)
            : this(code, comment, arguments, null, stackBefore, stackAfter) { }

        private OpcodeInfo(OpcodeValue code, string comment, string arguments,
            Action<OpcodeInfo, VirtualMachine.SegmentInfo, ushort, IndentedTextWriter>? writer = null,
            string? stackBefore = null, string? stackAfter = null)
        {
            this.Code = code;
            this.Comment = comment;
            this.arguments = arguments;
            this.stackBefore = stackBefore;
            this.stackAfter = stackAfter;
            if (writer != null)
            {
                this.Write = writer;
                return;
            }

            this.Write = (o, si, j, w) =>
            {
                var lastArg = 0;
                var times = 1;
                foreach (var argument in o.arguments)
                {
                    switch (argument)
                    {
                        case 'B':
                            while (--times >= 0)
                                w.Write(" 0x{0:X2}", lastArg = VM.FetchByte());
                            break;
                        case 'N':
                        case 'S':
                            while (--times >= 0)
                                w.Write(" " + (lastArg = VM.FetchSignedByte()));
                            break;
                        case 'W':
                            while (--times >= 0)
                                w.Write(" 0x{0:X4}", lastArg = VM.FetchWord());
                            break;
                        case 'C':
                            while (--times >= 0)
                                w.Write(" 0x{0:X4}", lastArg = VM.FetchBig());
                            break;
                        case 'A':
                            var bytes = new byte[times];
                            for (var i = 0; i < times; i++)
                                lastArg = bytes[i] = VM.FetchByte();
                            w.Write(" \"{0}\"", VM.Escape(Encoding.ASCII.GetString(bytes)));
                            break;
                        case '*':
                            times = lastArg - 2;
                            break;
                    }
                    times += 2;
                }
            };
        }

        private static ushort ParseJump(ushort jtab)
        {
            var disp = (int)VM.FetchSignedByte();
            if (disp >= 0)
                return (ushort)(VM.InterpreterProgramCounter + disp);
#if WORD_MEMORY
            var newIpc = (ushort)(Memory.Read(jtab.Index(-1)) + 2 - (Memory.Read((ushort)(jtab + disp / 2)) - disp));
#else
            var newIpc = (ushort)(Memory.Rd(jtab.Index(-1)) + 2 - (Memory.Rd((ushort)(jtab + disp)) - disp));
#endif
            return newIpc;
        }

        private static void JumpWriter(OpcodeInfo o, VirtualMachine.SegmentInfo si, ushort jtab, IndentedTextWriter w) => w.Write(" 0x{0:X4}", OpcodeInfo.ParseJump(jtab));

        private static void TypeWriter(OpcodeInfo o, VirtualMachine.SegmentInfo si, ushort jtab, IndentedTextWriter w)
        {
            switch (VM.FetchByte())
            {
                case 2:
                    w.Write(" REAL");
                    return;
                case 4:
                    w.Write(" STRING");
                    return;
                case 6:
                    w.Write(" BOOL");
                    return;
                case 8:
                    w.Write(" SET");
                    return;
                case 10:
                    w.Write(" BYTE");
                    return;
                case 12:
                    w.Write(" WORD");
                    return;
            }
        }

        private static void CallWriter(OpcodeInfo o, VirtualMachine.SegmentInfo si, ushort jtab, IndentedTextWriter w)
        {
            var id = VM.FetchByte();
            w.Write(" {0}_{1:X}", si.Name, id);
        }

        private static void ExternalVariableWiter(OpcodeInfo o, VirtualMachine.SegmentInfo si, ushort jtab,
            IndentedTextWriter w) => w.Write(" {0}_G{1:X}", VM.SegmentDictionary[VM.FetchByte()].Name, VM.FetchByte());

        private static void GlobalVariableWiter(OpcodeInfo o, VirtualMachine.SegmentInfo si, ushort jtab,
            IndentedTextWriter w) => w.Write(" {0}_G{1:X}", si.Name, VM.FetchBig());

        private static void LocalVariableWriter(OpcodeInfo o, VirtualMachine.SegmentInfo si, ushort jtab,
            IndentedTextWriter w) => w.Write(" {0}_{1:X}_L{2:X}", si.Name, VM.GetProcedureNumber(jtab), VM.FetchBig());

        private static void IntermediateVariableWriter(OpcodeInfo o, VirtualMachine.SegmentInfo si, ushort jtab,
            IndentedTextWriter w) => w.Write(" {0}_PARENT{1:X}_L{2:X}", si.Name, VM.FetchByte(), VM.FetchBig());

        public static readonly OpcodeInfo[] OpCodes =
        {
            new(OpcodeValue.SLDC_0, "Short LoaD Constant 0", ""),
            new(OpcodeValue.SLDC_1, "Short LoaD Constant 1", ""),
            new(OpcodeValue.SLDC_2, "Short LoaD Constant 2", ""),
            new(OpcodeValue.SLDC_3, "Short LoaD Constant 3", ""),
            new(OpcodeValue.SLDC_4, "Short LoaD Constant 4", ""),
            new(OpcodeValue.SLDC_5, "Short LoaD Constant 5", ""),
            new(OpcodeValue.SLDC_6, "Short LoaD Constant 6", ""),
            new(OpcodeValue.SLDC_7, "Short LoaD Constant 7", ""),
            new(OpcodeValue.SLDC_8, "Short LoaD Constant 8", ""),
            new(OpcodeValue.SLDC_9, "Short LoaD Constant 9", ""),
            new(OpcodeValue.SLDC_10, "Short LoaD Constant 10", ""),
            new(OpcodeValue.SLDC_11, "Short LoaD Constant 11", ""),
            new(OpcodeValue.SLDC_12, "Short LoaD Constant 12", ""),
            new(OpcodeValue.SLDC_13, "Short LoaD Constant 13", ""),
            new(OpcodeValue.SLDC_14, "Short LoaD Constant 14", ""),
            new(OpcodeValue.SLDC_15, "Short LoaD Constant 15", ""),
            new(OpcodeValue.SLDC_16, "Short LoaD Constant 16", ""),
            new(OpcodeValue.SLDC_17, "Short LoaD Constant 17", ""),
            new(OpcodeValue.SLDC_18, "Short LoaD Constant 18", ""),
            new(OpcodeValue.SLDC_19, "Short LoaD Constant 19", ""),
            new(OpcodeValue.SLDC_20, "Short LoaD Constant 20", ""),
            new(OpcodeValue.SLDC_21, "Short LoaD Constant 21", ""),
            new(OpcodeValue.SLDC_22, "Short LoaD Constant 22", ""),
            new(OpcodeValue.SLDC_23, "Short LoaD Constant 23", ""),
            new(OpcodeValue.SLDC_24, "Short LoaD Constant 24", ""),
            new(OpcodeValue.SLDC_25, "Short LoaD Constant 25", ""),
            new(OpcodeValue.SLDC_26, "Short LoaD Constant 26", ""),
            new(OpcodeValue.SLDC_27, "Short LoaD Constant 27", ""),
            new(OpcodeValue.SLDC_28, "Short LoaD Constant 28", ""),
            new(OpcodeValue.SLDC_29, "Short LoaD Constant 29", ""),
            new(OpcodeValue.SLDC_30, "Short LoaD Constant 30", ""),
            new(OpcodeValue.SLDC_31, "Short LoaD Constant 31", ""),
            new(OpcodeValue.SLDC_32, "Short LoaD Constant 32", ""),
            new(OpcodeValue.SLDC_33, "Short LoaD Constant 33", ""),
            new(OpcodeValue.SLDC_34, "Short LoaD Constant 34", ""),
            new(OpcodeValue.SLDC_35, "Short LoaD Constant 35", ""),
            new(OpcodeValue.SLDC_36, "Short LoaD Constant 36", ""),
            new(OpcodeValue.SLDC_37, "Short LoaD Constant 37", ""),
            new(OpcodeValue.SLDC_38, "Short LoaD Constant 38", ""),
            new(OpcodeValue.SLDC_39, "Short LoaD Constant 39", ""),
            new(OpcodeValue.SLDC_40, "Short LoaD Constant 40", ""),
            new(OpcodeValue.SLDC_41, "Short LoaD Constant 41", ""),
            new(OpcodeValue.SLDC_42, "Short LoaD Constant 42", ""),
            new(OpcodeValue.SLDC_43, "Short LoaD Constant 43", ""),
            new(OpcodeValue.SLDC_44, "Short LoaD Constant 44", ""),
            new(OpcodeValue.SLDC_45, "Short LoaD Constant 45", ""),
            new(OpcodeValue.SLDC_46, "Short LoaD Constant 46", ""),
            new(OpcodeValue.SLDC_47, "Short LoaD Constant 47", ""),
            new(OpcodeValue.SLDC_48, "Short LoaD Constant 48", ""),
            new(OpcodeValue.SLDC_49, "Short LoaD Constant 49", ""),
            new(OpcodeValue.SLDC_50, "Short LoaD Constant 50", ""),
            new(OpcodeValue.SLDC_51, "Short LoaD Constant 51", ""),
            new(OpcodeValue.SLDC_52, "Short LoaD Constant 52", ""),
            new(OpcodeValue.SLDC_53, "Short LoaD Constant 53", ""),
            new(OpcodeValue.SLDC_54, "Short LoaD Constant 54", ""),
            new(OpcodeValue.SLDC_55, "Short LoaD Constant 55", ""),
            new(OpcodeValue.SLDC_56, "Short LoaD Constant 56", ""),
            new(OpcodeValue.SLDC_57, "Short LoaD Constant 57", ""),
            new(OpcodeValue.SLDC_58, "Short LoaD Constant 58", ""),
            new(OpcodeValue.SLDC_59, "Short LoaD Constant 59", ""),
            new(OpcodeValue.SLDC_60, "Short LoaD Constant 60", ""),
            new(OpcodeValue.SLDC_61, "Short LoaD Constant 61", ""),
            new(OpcodeValue.SLDC_62, "Short LoaD Constant 62", ""),
            new(OpcodeValue.SLDC_63, "Short LoaD Constant 63", ""),
            new(OpcodeValue.SLDC_64, "Short LoaD Constant 64", ""),
            new(OpcodeValue.SLDC_65, "Short LoaD Constant 65", ""),
            new(OpcodeValue.SLDC_66, "Short LoaD Constant 66", ""),
            new(OpcodeValue.SLDC_67, "Short LoaD Constant 67", ""),
            new(OpcodeValue.SLDC_68, "Short LoaD Constant 68", ""),
            new(OpcodeValue.SLDC_69, "Short LoaD Constant 69", ""),
            new(OpcodeValue.SLDC_70, "Short LoaD Constant 70", ""),
            new(OpcodeValue.SLDC_71, "Short LoaD Constant 71", ""),
            new(OpcodeValue.SLDC_72, "Short LoaD Constant 72", ""),
            new(OpcodeValue.SLDC_73, "Short LoaD Constant 73", ""),
            new(OpcodeValue.SLDC_74, "Short LoaD Constant 74", ""),
            new(OpcodeValue.SLDC_75, "Short LoaD Constant 75", ""),
            new(OpcodeValue.SLDC_76, "Short LoaD Constant 76", ""),
            new(OpcodeValue.SLDC_77, "Short LoaD Constant 77", ""),
            new(OpcodeValue.SLDC_78, "Short LoaD Constant 78", ""),
            new(OpcodeValue.SLDC_79, "Short LoaD Constant 79", ""),
            new(OpcodeValue.SLDC_80, "Short LoaD Constant 80", ""),
            new(OpcodeValue.SLDC_81, "Short LoaD Constant 81", ""),
            new(OpcodeValue.SLDC_82, "Short LoaD Constant 82", ""),
            new(OpcodeValue.SLDC_83, "Short LoaD Constant 83", ""),
            new(OpcodeValue.SLDC_84, "Short LoaD Constant 84", ""),
            new(OpcodeValue.SLDC_85, "Short LoaD Constant 85", ""),
            new(OpcodeValue.SLDC_86, "Short LoaD Constant 86", ""),
            new(OpcodeValue.SLDC_87, "Short LoaD Constant 87", ""),
            new(OpcodeValue.SLDC_88, "Short LoaD Constant 88", ""),
            new(OpcodeValue.SLDC_89, "Short LoaD Constant 89", ""),
            new(OpcodeValue.SLDC_90, "Short LoaD Constant 90", ""),
            new(OpcodeValue.SLDC_91, "Short LoaD Constant 91", ""),
            new(OpcodeValue.SLDC_92, "Short LoaD Constant 92", ""),
            new(OpcodeValue.SLDC_93, "Short LoaD Constant 93", ""),
            new(OpcodeValue.SLDC_94, "Short LoaD Constant 94", ""),
            new(OpcodeValue.SLDC_95, "Short LoaD Constant 95", ""),
            new(OpcodeValue.SLDC_96, "Short LoaD Constant 96", ""),
            new(OpcodeValue.SLDC_97, "Short LoaD Constant 97", ""),
            new(OpcodeValue.SLDC_98, "Short LoaD Constant 98", ""),
            new(OpcodeValue.SLDC_99, "Short LoaD Constant 99", ""),
            new(OpcodeValue.SLDC_100, "Short LoaD Constant 100", ""),
            new(OpcodeValue.SLDC_101, "Short LoaD Constant 101", ""),
            new(OpcodeValue.SLDC_102, "Short LoaD Constant 102", ""),
            new(OpcodeValue.SLDC_103, "Short LoaD Constant 103", ""),
            new(OpcodeValue.SLDC_104, "Short LoaD Constant 104", ""),
            new(OpcodeValue.SLDC_105, "Short LoaD Constant 105", ""),
            new(OpcodeValue.SLDC_106, "Short LoaD Constant 106", ""),
            new(OpcodeValue.SLDC_107, "Short LoaD Constant 107", ""),
            new(OpcodeValue.SLDC_108, "Short LoaD Constant 108", ""),
            new(OpcodeValue.SLDC_109, "Short LoaD Constant 109", ""),
            new(OpcodeValue.SLDC_110, "Short LoaD Constant 110", ""),
            new(OpcodeValue.SLDC_111, "Short LoaD Constant 111", ""),
            new(OpcodeValue.SLDC_112, "Short LoaD Constant 112", ""),
            new(OpcodeValue.SLDC_113, "Short LoaD Constant 113", ""),
            new(OpcodeValue.SLDC_114, "Short LoaD Constant 114", ""),
            new(OpcodeValue.SLDC_115, "Short LoaD Constant 115", ""),
            new(OpcodeValue.SLDC_116, "Short LoaD Constant 116", ""),
            new(OpcodeValue.SLDC_117, "Short LoaD Constant 117", ""),
            new(OpcodeValue.SLDC_118, "Short LoaD Constant 118", ""),
            new(OpcodeValue.SLDC_119, "Short LoaD Constant 119", ""),
            new(OpcodeValue.SLDC_120, "Short LoaD Constant 120", ""),
            new(OpcodeValue.SLDC_121, "Short LoaD Constant 121", ""),
            new(OpcodeValue.SLDC_122, "Short LoaD Constant 122", ""),
            new(OpcodeValue.SLDC_123, "Short LoaD Constant 123", ""),
            new(OpcodeValue.SLDC_124, "Short LoaD Constant 124", ""),
            new(OpcodeValue.SLDC_125, "Short LoaD Constant 125", ""),
            new(OpcodeValue.SLDC_126, "Short LoaD Constant 126", ""),
            new(OpcodeValue.SLDC_127, "Short LoaD Constant 127", ""),
            new(OpcodeValue.ABI, "ABsolute Integer", ""),
            new(OpcodeValue.ABR, "ABsolure Real", ""),
            new(OpcodeValue.ADI, "ADd Integer", ""),
            new(OpcodeValue.ADR, "ADd Real", ""),
            new(OpcodeValue.LAND, "Logical AND", ""),
            new(OpcodeValue.DIF, "set DIFference", ""),
            new(OpcodeValue.DVI, "DiVide Integer", ""),
            new(OpcodeValue.DVR, "DiVide Real", ""),
            new(OpcodeValue.CHK, "CHecK", ""),
            new(OpcodeValue.FLO, "FLoat next to tOs", ""),
            new(OpcodeValue.FLT, "FLoat Tos", ""),
            new(OpcodeValue.INN, "set IN operation", ""),
            new(OpcodeValue.INT, "set INTersection", ""),
            new(OpcodeValue.LOR, "Logical OR", ""),
            new(OpcodeValue.MODI, "MODulo Integer", ""),
            new(OpcodeValue.MPI, "MultiPly Integer", ""),
            new(OpcodeValue.MPR, "MultiPly Real", ""),
            new(OpcodeValue.NGI, "NeGate Integer", ""),
            new(OpcodeValue.NGR, "NeGate Real", ""),
            new(OpcodeValue.LNOT, "Logical NOT", ""),
            new(OpcodeValue.SRS, "SubRange Set", ""),
            new(OpcodeValue.SBI, "SuBstract Integer", ""),
            new(OpcodeValue.SBR, "SiBstract Real", ""),
            new(OpcodeValue.SGS, "SinGleton Set", ""),
            new(OpcodeValue.SQI, "SQuare Integer", ""),
            new(OpcodeValue.SQR, "SQare Real", ""),
            new(OpcodeValue.STO, "STOre indirect", ""),
            new(OpcodeValue.IXS, "IndeX String array", ""),
            new(OpcodeValue.UNI, "set UNIon", ""),
            new(OpcodeValue.LDE, "LoaD Extended", "NS", OpcodeInfo.ExternalVariableWiter),
            new(OpcodeValue.CSP, "Call Standard Procedure", "B", (o, si, j, w) =>
            {
                var subOp = (StandardCall) VM.FetchByte();
// ReSharper disable once PossibleNullReferenceException
                w.Write(" " + Enum.GetName(typeof(StandardCall), subOp)?.Substring(4));
            }),
            new(OpcodeValue.LDCN, "LoaD Constant Nil", ""),
            new(OpcodeValue.ADJ, "ADJust set", "B"),
            new(OpcodeValue.FJP, "False JumP", "S", OpcodeInfo.JumpWriter),
            new(OpcodeValue.INC, "INCrement field pointer", "C"),
            new(OpcodeValue.IND, "INdex and loaD", "C"),
            new(OpcodeValue.IXA, "IndeX Array", "C"),
            new(OpcodeValue.LAO, "Load glObal Address", "C", OpcodeInfo.GlobalVariableWiter),
            new(OpcodeValue.LSA, "Load String Address", "B*A"),
            new(OpcodeValue.LAE, "Load Address Extended", "NS", OpcodeInfo.ExternalVariableWiter),
            new(OpcodeValue.MOV, "MOVe words", "C"),
            new(OpcodeValue.LDO, "LoaD glObal", "C", OpcodeInfo.GlobalVariableWiter),
            new(OpcodeValue.SAS, "String ASsign", "N"),
            new(OpcodeValue.SRO, "StoRe glObal", "C", OpcodeInfo.GlobalVariableWiter),
            new(OpcodeValue.XJP, "case JumP", "WWW", (o, si, j, w) =>
            {
                VM.InterpreterProgramCounter = (ushort) (VM.InterpreterProgramCounter + 1 & ~1);

                var low = VM.FetchWord();
                var high = VM.FetchWord();
                VM.FetchByte(); // UJP
                var def = OpcodeInfo.ParseJump(j);
                w.WriteLine();
                w.Indent += 9;
                var count = high - low + 1;
                var i = 0;
                while (count-- > 0)
                {
                    var addr = VirtualMachine.InterpreterProgramCounter;
                    var offset = VirtualMachine.FetchWord();
                    w.WriteLine("case {0}: UJP 0x{1:X4}", low + i++, addr - offset);
                }
                w.WriteLine("default: UJP 0x{0:X4}", def);
                w.Indent -= 9;
            }),

            new(OpcodeValue.RNP, "Return from Non-base Procedure", "N"),
            new(OpcodeValue.CIP, "Call Intermeduiate Procedure", "B", OpcodeInfo.CallWriter),
            new(OpcodeValue.EQU, "EQUal", "N", OpcodeInfo.TypeWriter),
            new(OpcodeValue.GEQ, "Greater or EQual", "N", OpcodeInfo.TypeWriter),
            new(OpcodeValue.GRT, "GReaTer", "N", OpcodeInfo.TypeWriter),
            new(OpcodeValue.LDA, "LoaD intermediate Address", "NC", OpcodeInfo.IntermediateVariableWriter),
            new(OpcodeValue.LDC, "LoaD multiple word Constant", "B*W"),
            new(OpcodeValue.LEQ, "Less or EQual", "N", OpcodeInfo.TypeWriter),
            new(OpcodeValue.LES, "LESs", "N", OpcodeInfo.TypeWriter),
            new(OpcodeValue.LOD, "LOaD intermediate", "NC", OpcodeInfo.IntermediateVariableWriter),
            new(OpcodeValue.NEQ, "Not EQual", "N", OpcodeInfo.TypeWriter),
            new(OpcodeValue.STR, "SToRe intermediate", "NC", OpcodeInfo.IntermediateVariableWriter),
            new(OpcodeValue.UJP, "Unconditional JumP", "S", OpcodeInfo.JumpWriter),
            new(OpcodeValue.LDP, "LoaD Packed field", ""),
            new(OpcodeValue.STP, "STore Packed field", ""),
            new(OpcodeValue.LDM, "LoaD Multiple words", "B"),
            new(OpcodeValue.STM, "STore Multiple words", "B"),
            new(OpcodeValue.LDB, "LoaD Byte", ""),
            new(OpcodeValue.STB, "STore Byte", ""),
            new(OpcodeValue.IXP, "IndeX Packed array", "BB"),
            new(OpcodeValue.RBP, "Return from Base Procedure", ""),
            new(OpcodeValue.CBP, "Call Base Procedure", "B", OpcodeInfo.CallWriter),
            new(OpcodeValue.EQUI, "EQUal Integer", ""),
            new(OpcodeValue.GEQI, "Greater or EQual Integer", ""),
            new(OpcodeValue.GRTI, "GReaTer Integer", ""),
            new(OpcodeValue.LLA, "Load Local Address", "C", OpcodeInfo.LocalVariableWriter),
            new(OpcodeValue.LDCI, "LoaD Constant Integer", "W"),
            new(OpcodeValue.LEQI, "Less or EQual Integer", ""),
            new(OpcodeValue.LESI, "LESs Integer", ""),
            new(OpcodeValue.LDL, "LoaD Local", "C", OpcodeInfo.LocalVariableWriter),
            new(OpcodeValue.NEQI, "Not EQual Integer", ""),
            new(OpcodeValue.STL, "STore Local", "C", OpcodeInfo.LocalVariableWriter),
            new(OpcodeValue.CXP, "Call eXternal Procedure", "NB", (o, si, j, w) =>
            {
                var id = VM.FetchByte();
                var proc = VM.FetchByte();
                w.Write(" {0}_{1:X}", VM.SegmentDictionary[id].Name, proc);
            }),
            new(OpcodeValue.CLP, "Call Local Procedure", "B", OpcodeInfo.CallWriter),
            new(OpcodeValue.CGP, "Call Global Procedure", "B", OpcodeInfo.CallWriter),
            new(OpcodeValue.LPA, "Load Packed Array", ""),
            new(OpcodeValue.STE, "STore Extended", ""),

            new(OpcodeValue.NOP, "No OPeration", ""),
            new(OpcodeValue.EFJ, "Equal False Jump", "S", OpcodeInfo.JumpWriter),
            new(OpcodeValue.NFJ, "Not equal False Jump", "S", OpcodeInfo.JumpWriter),
            new(OpcodeValue.BPT, "BreakPoinT", "B"),
            new(OpcodeValue.XIT, "eXIT", ""),
            new(OpcodeValue.NOP, "No OPeration", ""),
            new(OpcodeValue.SLDL_1, "Short LoaD Local 1", ""),
            new(OpcodeValue.SLDL_2, "Short LoaD Local 2", ""),
            new(OpcodeValue.SLDL_3, "Short LoaD Local 3", ""),
            new(OpcodeValue.SLDL_4, "Short LoaD Local 4", ""),
            new(OpcodeValue.SLDL_5, "Short LoaD Local 5", ""),
            new(OpcodeValue.SLDL_6, "Short LoaD Local 6", ""),
            new(OpcodeValue.SLDL_7, "Short LoaD Local 7", ""),
            new(OpcodeValue.SLDL_8, "Short LoaD Local 8", ""),
            new(OpcodeValue.SLDL_9, "Short LoaD Local 9", ""),
            new(OpcodeValue.SLDL_10, "Short LoaD Local 10", ""),
            new(OpcodeValue.SLDL_11, "Short LoaD Local 11", ""),
            new(OpcodeValue.SLDL_12, "Short LoaD Local 12", ""),
            new(OpcodeValue.SLDL_13, "Short LoaD Local 13", ""),
            new(OpcodeValue.SLDL_14, "Short LoaD Local 14", ""),
            new(OpcodeValue.SLDL_15, "Short LoaD Local 15", ""),
            new(OpcodeValue.SLDL_16, "Short LoaD Local 16", ""),
            new(OpcodeValue.SLDO_1, "Short LoaD glObal 1", ""),
            new(OpcodeValue.SLDO_2, "Short LoaD glObal 2", ""),
            new(OpcodeValue.SLDO_3, "Short LoaD glObal 3", ""),
            new(OpcodeValue.SLDO_4, "Short LoaD glObal 4", ""),
            new(OpcodeValue.SLDO_5, "Short LoaD glObal 5", ""),
            new(OpcodeValue.SLDO_6, "Short LoaD glObal 6", ""),
            new(OpcodeValue.SLDO_7, "Short LoaD glObal 7", ""),
            new(OpcodeValue.SLDO_8, "Short LoaD glObal 8", ""),
            new(OpcodeValue.SLDO_9, "Short LoaD glObal 9", ""),
            new(OpcodeValue.SLDO_10, "Short LoaD glObal 10", ""),
            new(OpcodeValue.SLDO_11, "Short LoaD glObal 11", ""),
            new(OpcodeValue.SLDO_12, "Short LoaD glObal 12", ""),
            new(OpcodeValue.SLDO_13, "Short LoaD glObal 13", ""),
            new(OpcodeValue.SLDO_14, "Short LoaD glObal 14", ""),
            new(OpcodeValue.SLDO_15, "Short LoaD glObal 15", ""),
            new(OpcodeValue.SLDO_16, "Short LoaD glObal 16", ""),
            new(OpcodeValue.SIND_0, "Short load INDirect", ""),
            new(OpcodeValue.SIND_1, "Short INdex 1 and loaD ", ""),
            new(OpcodeValue.SIND_2, "Short INdex 2 and loaD ", ""),
            new(OpcodeValue.SIND_3, "Short INdex 3 and loaD ", ""),
            new(OpcodeValue.SIND_4, "Short INdex 4 and loaD ", ""),
            new(OpcodeValue.SIND_5, "Short INdex 5 and loaD ", ""),
            new(OpcodeValue.SIND_6, "Short INdex 6 and loaD ", ""),
            new(OpcodeValue.SIND_7, "Short INdex 7 and loaD ", "")
        };
    }
}