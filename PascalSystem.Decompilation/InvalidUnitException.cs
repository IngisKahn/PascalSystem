namespace PascalSystem.Decompilation
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class InvalidUnitException : DecompilationException
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public InvalidUnitException() { }
        public InvalidUnitException(string message) : base(message) { }
        public InvalidUnitException(string message, Exception inner) : base(message, inner) { }

        protected InvalidUnitException(
            SerializationInfo info,
            StreamingContext context) : base(info, context) { }
    }
}