namespace PascalSystem.Runtime
{
    using System;

    internal static class VirtualMachine
    {

        public static ushort StackPointer { get; set; }

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
    }
}