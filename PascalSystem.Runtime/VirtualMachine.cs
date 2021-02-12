namespace PascalSystem.Runtime
{
    using System;

    internal static class VirtualMachine
    {

        public struct SegmentInfo
        {
            public int UseCount;

            public ushort OldKp;

            //public ushort Id;
            public ushort SegmentPointer;

            public ushort SegBase;
            public string Name;
        }
        public static ushort StackPointer { get; set; }
        public static SegmentInfo[] SegmentDictionary { get; set; }
        public static int InterpreterProgramCounter { get; set; }

        public static void Panic(string format, params object[] parameters)
        {
            //TermClose();
            Console.Error.WriteLine("panic: " + format, parameters);
            VirtualMachine.DumpCore();
            Environment.Exit(1);
        }

        private static void DumpCore()
        {
            throw new NotImplementedException();
        }

        public static ushort GetProcedureInstructionPointerBase(ushort jTab)
        {
            throw new NotImplementedException();
        }

        public static byte FetchByte()
        {
            throw new NotImplementedException();
        }

        public static int FetchWord()
        {
            throw new NotImplementedException();
        }

        public static int FetchBig()
        {
            throw new NotImplementedException();
        }

        public static object? GetProcedureNumber(ushort jtab)
        {
            throw new NotImplementedException();
        }

        public static int FetchSignedByte()
        {
            throw new NotImplementedException();
        }

        public static object? Escape(string getString)
        {
            throw new NotImplementedException();
        }
    }
}