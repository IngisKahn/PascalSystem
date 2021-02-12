namespace PascalSystem.Runtime
{
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    internal static class Stack
    {
        private const ushort spTop = 0x200;

        public static ushort Pop()
        {
            var result = Memory.Read(VirtualMachine.StackPointer);
            VirtualMachine.StackPointer = VirtualMachine.StackPointer.Index(1);
            if (VirtualMachine.StackPointer > Stack.spTop)
                VirtualMachine.Panic("Stack Underflow");
            return result;
        }

        public static short PopInteger()
        {
            var w = Stack.Pop();
            return (short)((w & 0x8000) == 0 ? w : w - 0x100000);
        }

        public static float PopReal()
        {
            Debug.Assert(sizeof(float) == 2 * sizeof(ushort));
            var fu = new FloatUnion { w0 = Stack.Pop(), w1 = Stack.Pop() };
            return fu.Float;
        }

        public static void Push(ushort value)
        {
            VirtualMachine.StackPointer = VirtualMachine.StackPointer.Index(-1);
            Memory.Write(VirtualMachine.StackPointer, value);
        }

        public static void Push(short value) => Stack.Push((ushort)value);

        public static void Push(float value)
        {
            Debug.Assert(sizeof(float) == 2 * sizeof(ushort));
            var fu = new FloatUnion { Float = value };
            Stack.Push(fu.w1);
            Stack.Push(fu.w0);
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct FloatUnion
        {
            [FieldOffset(0)] public float Float;

            [FieldOffset(0)] public ushort w0;
            [FieldOffset(2)] public ushort w1;
        }
    }
}