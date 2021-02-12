namespace PascalSystem.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Text;

    internal static class Memory
    {
        [Flags]
        private enum MemoryOptions
        {
            None,
            Invalid,
            ReadOnly,
            Warn = 4,
            Emulate = 8
        }

        private struct Slot
        {
#if WORD_MEMORY
            public ushort Value;
#else
            public byte Value;
#endif
            public MemoryOptions Flags;
        }

        private static readonly Slot[] mem = new Slot[0x10000];

        private record EmulationTag(EmulateReader Read, EmulateWriter? Write);

        private static readonly Dictionary<ushort, EmulationTag> emulators = new();

        public static Endianness Endianness { get; }

        static Memory()
        {
            Memory.Endianness = Endianness.Little;
            for (var i = 0; i < 0x10000; i++)
                Memory.mem[i] = new() { Flags = MemoryOptions.Invalid };
        }
#if WORD_MEMORY
        public static byte ReadByte(ushort address, short offset)
        {
            Memory.PointerCheck(address);
            address = (ushort) (address + (offset & ~1) / 2);
            offset &= 1;
            return offset == 0 == (Memory.Endianness == Endianness.Big)
                ? (byte) (Memory.Read(address) >> 8)
                : (byte) (Memory.Read(address) & 0xFF);
        }

        public static ushort Read(ushort address, bool isSystemCall = false)
        {
            Memory.PointerCheck(address);
            var m = Memory.mem[address];
            if (m.Flags == MemoryOptions.None)
                return m.Value;
            if (!isSystemCall && (m.Flags & MemoryOptions.Invalid) != 0)
                PSystem.Warning("Memory.Rd: Reading uninitialized memory 0x{0:X4}", address);
            if ((m.Flags & MemoryOptions.Warn) != 0)
                PSystem.Warning("Memory.Rd: Access to 0x{0:X4}", address);

            return (m.Flags & MemoryOptions.Emulate) != 0 ? Memory.EmulateRead(address) : m.Value;
        }

        public static void WriteByte(ushort address, short offset, byte value)
        {
            Memory.PointerCheck(address);
            address = (ushort) (address + (offset & ~1) / 2);
            offset &= 1;
            var w = Memory.Read(address, true);
            w = offset == 0 == (Memory.Endianness == Endianness.Big)
                ? (ushort)(w & 0xFF | value << 8)
                : (ushort)(w & 0xFF00 | value & 0xFF);
            Memory.Write(address, w);
        }

        public static void Write(ushort address, ushort value)
        {
            Memory.PointerCheck(address);
            var m = Memory.mem[address];
            if (m.Flags != MemoryOptions.None)
            {
                if ((m.Flags & MemoryOptions.ReadOnly) != 0)
                {
                    PSystem.Warning("Memory.Wr: 0x{0:X4} is read only", address);
                    throw new ExecutionException(ExecutionErrorCode.InvalidMemoryReference);
                }
                if ((m.Flags & MemoryOptions.Invalid) != 0)
                {
                    m.Flags &= ~MemoryOptions.Invalid;
                    Memory.mem[address] = m;
                }
                if ((m.Flags & MemoryOptions.Warn) != 0)
                    PSystem.Warning("Memory.Rd: Access to 0x{0:X4}", address);

                if ((m.Flags & MemoryOptions.Emulate) != 0)
                {
                    Memory.EmulateWrite(address, value);
                    return;
                }
            }
            m.Value = value;
            Memory.mem[address] = m;
        }
#else
        public static byte RdByte(ushort address, short offset)
        {
            PointerCheck(address);
            address += offset;
            var m = mem[address];
            if (m.Flags != MemoryOptions.None)
            {
                if ((m.Flags & MemoryOptions.Invalid) != 0)
                    PSystem.Warning("Memory.RdByte: Reading uninitialized memory 0x{0:X4}", address);
                if ((m.Flags & MemoryOptions.Warn) != 0)
                    PSystem.Warning("Memory.RdByte: Access to 0x{0:X4}", address);

                if ((m.Flags & MemoryOptions.Emulate) != 0)
                    return EmulateRead(address);
            }
            return m.Value;
        }

        public static ushort Rd(ushort address)
        {
            PointerCheck(address);
            return Endianness == Endianness.Little
                ? RdByte(address, 0) + (RdByte(address, 1) << 8)
                : RdByte(address, 1) + (RdByte(address, 0) << 8);
        }
        public static void WrByte(ushort address, short offset, byte value)
        {
            PointerCheck(address);
            address += offset;
            var m = mem[address];
            if (m.Flags != MemoryOptions.None)
            {
                if ((m.Flags & MemoryOptions.ReadOnly) != 0)
                {
                    PSystem.Warning("Memory.Wr: 0x{0:X4} is read only", address);
                    PSystem.ExecutionError(ExecutionErrorCode.InvalidMemoryReference);
                }
                if ((m.Flags & MemoryOptions.Invalid) != 0)
                {
                    m.Flags &= ~MemoryOptions.Invalid;
                    mem[address] = m;
                }
                if ((m.Flags & MemoryOptions.Warn) != 0)
                    PSystem.Warning("Memory.Rd: Access to 0x{0:X4}", address);

                if ((m.Flags & MemoryOptions.Emulate) != 0)
                {
                    EmulateWrite(address, value);
                    return;
                }
            }
            m.Value = value;
            mem[address] = m;
        }
        public static void Wr(ushort address, ushort value)
        {
            PointerCheck(address);
            if (Endianness == Endianness.Little)
            {
                WrByte(address, 0, (byte)(value & 0xFF));
                WrByte(address, 1, (byte)(value >> 8));
            }
            else
            {
                WrByte(address, 1, (byte)(value & 0xFF));
                WrByte(address, 0, (byte)(value >> 8));
            }
        }
#endif
        public static void ReadOnly(ushort from, ushort to, bool isReadOnly)
        {
            while (from <= to)
            {
                var m = Memory.mem[from];
                if (isReadOnly)
                    m.Flags |= MemoryOptions.ReadOnly;
                else
                    m.Flags &= ~MemoryOptions.ReadOnly;
                Memory.mem[from] = m;
                from++;
            }
        }

        public static void Dump(TextWriter writer, ushort from, ushort to)
        {
            var buffer = new StringBuilder(80);
            var oldBuffer = "    ";

            var count = 0;
            for (var w = from; w <= to;)
            {
#if WORD_MEMORY
                w &= 0xFFF8;
#else
                w &= 0xFFF0;
#endif
                buffer.AppendFormat(
                    "{0:X4}:                                                                  \n", w);

#if WORD_MEMORY
// ReSharper disable TooWideLocalVariableScope
                char ch1;
                ushort value;
// ReSharper restore TooWideLocalVariableScope
                for (var i = 0; i < 8; i++)
#else
                byte value;
                for (var i = 0; i < 16; i++)
#endif
                {
                    string b;
                    char ch;
                    if ((Memory.mem[w].Flags & MemoryOptions.Invalid) != 0)
                    {
                        value = Memory.mem[w].Value;
#if WORD_MEMORY
                        b = value.ToString("X4");
#else
                        b = value.ToString("X2");
#endif
                        ch = (char)(value & 0xFF);
                        if (char.IsControl(ch))
                            ch = '.';
#if WORD_MEMORY
                        ch1 = (char) (value >> 8);
                        if (char.IsControl(ch1))
                            ch1 = '.';
#endif
                    }
                    else
                    {
#if WORD_MEMORY
                        b = "....";
                        ch1 = ' ';
#else
                        b = "..";
#endif
                        ch = ' ';
                    }
#if WORD_MEMORY
                    buffer[6 + 5 * i] = b[0];
                    buffer[6 + 5 * i + 1] = b[1];
                    buffer[6 + 5 * i + 2] = b[2];
                    buffer[6 + 5 * i + 3] = b[3];
                    buffer[6 + 5 * 8 + 2 * i] = ch;
                    buffer[6 + 5 * 8 + 2 * i + 1] = ch1;
#else
                    buffer[6 + 3 * i] = b[0];
                    buffer[6 + 3 * i + 1] = b[1];
                    buffer[6 + 3 * 16 + i] = ch;
#endif
                    w++;
                }
                if (buffer.ToString(4, buffer.Length - 4) != oldBuffer.Substring(4))
                {
                    if (count > 1)
                        writer.Write(".... {0} line{1} omitted\n", count - 1, count == 2 ? "" : "s");
                    if (count > 0)
                        writer.Write(oldBuffer);
                    count = 0;
                    writer.Write(buffer.ToString());
                }
                else
                    count++;
                oldBuffer = buffer.ToString();
                buffer.Clear();
            }
        }

        public static void PointerCheck(ushort pointer)
        {
            if (pointer == 0)
                throw new ExecutionException(ExecutionErrorCode.InvalidMemoryReference);
        }

        private static ushort EmulateRead(ushort address) => Memory.emulators.TryGetValue(address, out var et) ? et.Read(address) : (ushort)0;

        private static void EmulateWrite(ushort address, ushort value)
        {
            if (Memory.emulators.TryGetValue(address, out var et))
                et.Write?.Invoke(address, value);
        }

        public delegate ushort EmulateReader(ushort address);

        public delegate void EmulateWriter(ushort address, ushort value);

        [Conditional("MEM_EMULATE")]
        public static void SetEmulateRange(ushort from, ushort to, EmulateReader reader, EmulateWriter? writer)
        {
            var et = new EmulationTag(reader, writer);

            for (var mp = from; mp < to; mp++)
            {
                var m = Memory.mem[mp];
                var flags = m.Flags;
                flags &= ~MemoryOptions.Invalid;
                flags |= MemoryOptions.Emulate;
                m.Flags = flags;
                Memory.mem[mp] = m;
                Memory.emulators[mp] = et;
            }
        }

        [Conditional("MEM_EMULATE")]
        public static void SetEmulateWord(ushort address, EmulateReader reader, EmulateWriter? writer) =>
#if WORD_MEMORY
            Memory.SetEmulateRange(address, address, reader, writer);
#else
            SetEmulateRange(address, address + 1, reader, writer);
#endif


        /// <summary>
        ///     Dereference a self relocating pointer. Self relocating pointers are
        ///     used in the segment dictionary and in procedure activation records.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static ushort SelfRelPtr(ushort address) =>
#if WORD_MEMORY
            (ushort)(address - Memory.Read(address) / 2);
#else
            return (ushort) (address - Rd(address));
#endif

    }

    public static class Extensions
    {
        [Pure]
        public static ushort Index(this ushort pointer, ushort offset) => pointer.Index((short)offset);

        [Pure]
        public static ushort Index(this ushort pointer, short offset) =>
#if WORD_MEMORY
            (ushort)(pointer + offset);
#else
            return (ushort) (pointer + (offset << 1));
#endif

    }
}
