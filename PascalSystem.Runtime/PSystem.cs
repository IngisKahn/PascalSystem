namespace PascalSystem.Runtime
{
    using System;

    internal static class PSystem
    {
        public static ushort Boolean(bool b) => b ? 1 : 0;

        private const int segEntryLen = 3;

        public static ushort SegmentUnitPointer(ushort id) => VirtualMachine.SystemCommunicationPointer.Index(
            (ushort)(48 + PSystem.segEntryLen * id));

        public static ushort SegmentBlockPointer(ushort id) => PSystem.SegmentUnitPointer(id).Index(1);

        public static ushort SegmentSizePointer(ushort id) => PSystem.SegmentUnitPointer(id).Index(2);

        public static void IOError(ushort result) => Memory.Write(PSystem.IOResultPointer, result);

        internal static void Warning(string message, params object[] arg) => Console.WriteLine("WARNING: " + message, arg);

        public static ushort IOResultPointer => VirtualMachine.SystemCommunicationPointer.Index(0);
        public static ushort Error => VirtualMachine.SystemCommunicationPointer.Index(1);
        public static ushort SystemUnitPointer => VirtualMachine.SystemCommunicationPointer.Index(2);
        public static ushort BugState => VirtualMachine.SystemCommunicationPointer.Index(3);
        public static ushort GlobalDirectoryPointer => VirtualMachine.SystemCommunicationPointer.Index(4);
        public static ushort BombP => VirtualMachine.SystemCommunicationPointer.Index(5);
        public static ushort StackBase => VirtualMachine.SystemCommunicationPointer.Index(6);
        public static ushort LastMp => VirtualMachine.SystemCommunicationPointer.Index(7);
#if APPLE_1_3
        public static ushort BombProc { get { return VirtualMachine.Syscom.Index(8); } }
        public static ushort BombSeg { get { return VirtualMachine.Syscom.Index(9); } }
#else
        public static ushort JTab => VirtualMachine.SystemCommunicationPointer.Index(8);
        public static ushort Seg => VirtualMachine.SystemCommunicationPointer.Index(9);
#endif
        public static ushort MemTop => VirtualMachine.SystemCommunicationPointer.Index(10);
        public static ushort BombIpc => VirtualMachine.SystemCommunicationPointer.Index(11);
        public static ushort HaltLine => VirtualMachine.SystemCommunicationPointer.Index(12);
        public static ushort Breakpoints0 => VirtualMachine.SystemCommunicationPointer.Index(13);
        public static ushort Breakpoints1 => VirtualMachine.SystemCommunicationPointer.Index(14);
        public static ushort Breakpoints2 => VirtualMachine.SystemCommunicationPointer.Index(15);
        public static ushort Breakpoints3 => VirtualMachine.SystemCommunicationPointer.Index(16);
        public static ushort Retries => VirtualMachine.SystemCommunicationPointer.Index(17);
        public static ushort Extension0 => VirtualMachine.SystemCommunicationPointer.Index(18);
        public static ushort Extension1 => VirtualMachine.SystemCommunicationPointer.Index(19);
        public static ushort Extension2 => VirtualMachine.SystemCommunicationPointer.Index(20);
        public static ushort Extension3 => VirtualMachine.SystemCommunicationPointer.Index(21);
        public static ushort Extension4 => VirtualMachine.SystemCommunicationPointer.Index(22);
        public static ushort Extension5 => VirtualMachine.SystemCommunicationPointer.Index(23);
        public static ushort Extension6 => VirtualMachine.SystemCommunicationPointer.Index(24);
        public static ushort Extension7 => VirtualMachine.SystemCommunicationPointer.Index(25);
        public static ushort Extension8 => VirtualMachine.SystemCommunicationPointer.Index(26);
        public static ushort LowTime => VirtualMachine.SystemCommunicationPointer.Index(27);
        public static ushort HighTime => VirtualMachine.SystemCommunicationPointer.Index(28);
        public static ushort MiscInfoPointer => VirtualMachine.SystemCommunicationPointer.Index(29);
        public static ushort CrtType => VirtualMachine.SystemCommunicationPointer.Index(30);
        public static ushort XrtCtrl => VirtualMachine.SystemCommunicationPointer.Index(31);
        public static ushort CrtInfo => VirtualMachine.SystemCommunicationPointer.Index(37);
        public static ushort CrtInfoHeightPointer => VirtualMachine.SystemCommunicationPointer.Index(37);
        public static ushort CrtInfoWidthPointer => VirtualMachine.SystemCommunicationPointer.Index(38);
    }
}