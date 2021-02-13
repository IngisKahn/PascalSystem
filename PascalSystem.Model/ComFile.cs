namespace PascalSystem.Model
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    public class ComFile
    {
        private readonly Dictionary<string, byte[]> files = new();

        public ComFile(string fileName, string systemName = "SYSTEM.PASCAL")
        {
            this.LoadFilesFromIndex(fileName);

            if (!this.files.TryGetValue(systemName, out var systemData))
                return;

            for (var i = 0; i < 16; i++)
            {
                var position = BitConverter.ToUInt16(systemData, i << 2);
                var length = BitConverter.ToUInt16(systemData, (i << 2) + 2);
                if (length == 0)
                    continue;
                var name = Encoding.ASCII.GetString(systemData, 0x40 + (i << 3), 8).TrimEnd();
                var number = systemData[0x100 + (i << 1)];

                this.UnitMap.Add(number, new(name, number, this, (position << 9) + length));
            }

            foreach (var unit in this.Units)
                unit.Initialize(systemData);
        }

        private void LoadFilesFromIndex(string fileName)
        {
            const int indexOffset = 0x400;
            const int entryLength = 0x1A;

            using FileStream stream = new(fileName, FileMode.Open, FileAccess.Read);
            BinaryReader reader = new(stream);
            for (var fileCount = 0; ; fileCount++)
            {
                reader.BaseStream.Seek(indexOffset + entryLength * fileCount, SeekOrigin.Begin);
                var filePosition = reader.ReadUInt16();
                var fileEnd = reader.ReadUInt16();
                if (filePosition + fileEnd == 0)
                    break;

                reader.BaseStream.Seek(2, SeekOrigin.Current);
                var nameLength = reader.ReadByte();
                var name = Encoding.ASCII.GetString(reader.ReadBytes(nameLength));
                reader.BaseStream.Seek(filePosition * 0x200, SeekOrigin.Begin);
                this.files.Add(name, reader.ReadBytes((fileEnd - filePosition) * 0x200));
            }
        }

        internal Dictionary<int, Unit> UnitMap { get; } = new();

        public IEnumerable<Unit> Units => this.UnitMap.Values;

        public void Dump(string outputFolder)
        {
            foreach (var (fileName, fileData) in this.files)
                using (var stream = new FileStream(Path.Combine(outputFolder, fileName + ".bin"), FileMode.Create))
                    stream.Write(fileData, 0, fileData.Length);
            foreach (var unit in this.UnitMap.Values)
                unit.Dump(outputFolder);
        }
    }
}
