namespace PascalSystem.Runtime
{
    using System;
    using System.Diagnostics;
    using System.IO;

    internal static class DiskIO
    {
        public enum DiskMode
        {
            ReadOnly,
            Forget,
            ReadWrite
        }

        public const int MaxUnits = 20;

        private static readonly int[] dskTable = { 0, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 15 };

        private static readonly Unit?[] unitTable = new Unit[DiskIO.MaxUnits];

        public static void Read(ushort unit, ushort address, short addressOffset, ushort len, ushort blockNo)
        {
            var offset = blockNo * 512u;
            var track = blockNo / 8;
            var sector = (blockNo & 7) * 2;
            Debug.Assert(unit < DiskIO.MaxUnits);
            var u = DiskIO.unitTable[unit];
            if (u == null || u.Fd == null && u.Data == null)
            {
                PSystem.IOError(9);
                return;
            }
            if (offset + len > u.Size)
            {
                PSystem.IOError(64);
                return;
            }

            while (len != 0)
            {
                var size = 256;
                var sec = sector;
                if (len < size)
                    size = len;
                //if (u.Translate != null)
                //    sec = u.Translate[sector];
                int i;
                if (u.Data != null)
                    for (i = 0; i < size; i++)
                        Memory.WriteByte
                        (
                            address,
                            (short)(addressOffset + i),
                            u.Data[(track * 16 + sec) * 256 + i]
                        );
                else if (u.Fd != null)
                {
                    var buf = new byte[256];
                    bool failed;
                    try
                    {
                        u.Fd.Seek((track * 16 + sec) * 256, SeekOrigin.Begin);
                        failed = u.Fd.Read(buf, 0, size) < size;
                    }
                    catch
                    {
                        failed = true;
                    }

                    if (failed)
                    {
                        PSystem.IOError(64);
                        return;
                    }

                    for (i = 0; i < size; i++)
                        Memory.WriteByte(address, (short)(addressOffset + i), buf[i]);
                }

                addressOffset += (short)size;
                len -= (ushort)size;
                sector++;
                if (sector < 16)
                    continue;
                track++;
                sector = 0;
            }
            PSystem.IOError(0);
        }

        public static void Write(ushort unit, ushort address, short addressOffset, ushort len, ushort blockNo)
        {
            var offset = blockNo * 512u;
            var track = blockNo / 8;
            var sector = (blockNo & 7) * 2;
            Debug.Assert(unit < DiskIO.MaxUnits);
            var u = DiskIO.unitTable[unit];
            if (u == null || u.Fd == null && u.Data == null)
            {
                PSystem.IOError(9);
                return;
            }
            if (u.ReadOnly)
            {
                PSystem.IOError(16);
                return;
            }
            if (offset + len > u.Size)
            {
                PSystem.IOError(64);
                return;
            }

            while (len != 0)
            {
                var size = 256;
                var sec = sector;
                if (len < size)
                    size = len;
                //if (u.Translate != null)
                //    sec = u.Translate[sector];
                int i;
                if (u.Data != null)
                    for (i = 0; i < size; i++)
                        u.Data[(track * 16 + sec) * 256 + i] =
                            Memory.ReadByte(address, (short)(addressOffset + i));
                else if (u.Fd != null)
                {
                    var buf = new byte[256];

                    for (i = 0; i < size; i++)
                        buf[i] = Memory.ReadByte(address, (short)(addressOffset + i));

                    try
                    {
                        u.Fd.Seek((track * 16 + sec) * 256, SeekOrigin.Begin);
                        u.Fd.Write(buf, 0, size);
                    }
                    catch
                    {
                        PSystem.IOError(64);
                        return;
                    }
                }
                addressOffset += (short)size;
                len -= (ushort)size;
                sector++;
                if (sector < 16)
                    continue;
                track++;
                sector = 0;
            }
            PSystem.IOError(0);
        }

        public static void Clear(ushort unit)
        {
            Debug.Assert(unit < DiskIO.MaxUnits);
            PSystem.IOError(0);
        }

        public static void Stat(ushort unit)
        {
            Debug.Assert(unit < DiskIO.MaxUnits);
            var u = DiskIO.unitTable[unit];
            if (u == null || u.Fd == null && u.Data == null)
                PSystem.IOError(9);
            else
                PSystem.IOError(0);
        }

        public static void Unmount(ushort unit)
        {
            Debug.Assert(unit < DiskIO.MaxUnits);
            var u = DiskIO.unitTable[unit];
            if (u == null)
                return;

            u.Data = null;

            if (u.Fd == null)
                return;

            u.Fd.Close();
            u.Fd = null;
        }

        public static bool Mount(ushort unit, string fileName, DiskMode mode)
        {
            Debug.Assert(unit < DiskIO.MaxUnits);
            DiskIO.Unmount(unit);

            FileStream fd;
            try
            {
                fd = new FileStream(fileName, FileMode.Open,
                    mode == DiskMode.ReadWrite ? FileAccess.ReadWrite : FileAccess.Read);
            }
            catch (Exception e)
            {
                Console.Write(e);
                return false;
            }
            var u = new Unit
            {
                Fd = fd,
                Size = (uint)fd.Length,
                ReadOnly = mode == DiskMode.ReadOnly
            };

            if (fileName.EndsWith(".dsk"))
                u.Translate = DiskIO.dskTable;

            if (mode == DiskMode.Forget)
            {
                u.Data = new byte[u.Size];

                try
                {
                    fd.Read(u.Data, 0, (int)u.Size);
                }
                catch (Exception e)
                {
                    Console.Write(e);
                    u.Data = null;
                    return false;
                }
                finally
                {
                    fd.Close();
                    u.Fd = null;
                }
            }
            DiskIO.unitTable[unit] = u;

            return true;
        }

        public static Endianness GetEndianness(ushort unit)
        {
            Debug.Assert(unit < DiskIO.MaxUnits);
            var u = DiskIO.unitTable[unit];

            if (u == null || u.Size < 0x402)
                return Endianness.Little;

            if (u.Data != null && u.Size >= 0x402)
                return u.Data[0x402] != 0 ? Endianness.Little : Endianness.Big;

            if (u.Fd == null)
                return Endianness.Little;

            u.Fd.Seek(0x402, SeekOrigin.Begin);
            return u.Fd.ReadByte() != 0 ? Endianness.Little : Endianness.Big;
        }

        private sealed class Unit
        {
            public byte[]? Data;
            public Stream? Fd;
            public bool ReadOnly;
            public uint Size;
            public int[]? Translate;
        }
    }
}