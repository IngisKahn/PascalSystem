namespace PascalSystem.Runtime
{
    internal enum ExecutionErrorCode
    {
        System,
        InvalidIndex,
        NoSegment,
        ExitFromUncalledProcedure,
        StackOverflow,
        IntegerOverflow,
        DivideByZero,
        InvalidMemoryReference,
        UserBreak,
        SystemIO,
        UserIO,
        UnimplementedInstruction,
        FloatingPointMath,
        StringTooLong,
        HaltBreakpoint,
        Breakpoint
    }
}