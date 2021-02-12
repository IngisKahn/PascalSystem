namespace PascalSystem.Runtime
{
    using System;

    internal sealed class ExecutionException : Exception
    {
        public ExecutionException(ExecutionErrorCode code) => this.Code = code;
        public ExecutionErrorCode Code { get; }
    }
}