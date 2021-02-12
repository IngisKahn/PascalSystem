namespace PascalSystem.Runtime
{
    using System;

    internal static class PSystem
    { 
        internal static void Warning(string message, params object[] arg) => Console.WriteLine("WARNING: " + message, arg);
    }
}