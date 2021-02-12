namespace PascalSystem.Runtime
{
    using System;

    internal static class PSystem
    { 
        internal static void Warning(string message, params object[] arg) => Console.WriteLine("WARNING: " + message, arg);

        public static void IOError(int p0)
        {
            throw new NotImplementedException();
        }

        public static ushort Boolean(bool p0)
        {
            throw new NotImplementedException();
        }
    }
}