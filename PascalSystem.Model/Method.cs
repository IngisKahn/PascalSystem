namespace PascalSystem.Model
{
    using System;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    public class Method
    {
        private readonly int lexLevel;

        private readonly Collection<OpCode> opCodes = new();

        public int Id { get; }

        public Method(byte[] systemData, int jumpTable, int procedureId, Unit unit, Method? previousMethod)
        {
            this.Id = procedureId;
            this.Unit = unit;

            this.lexLevel = systemData[jumpTable + 1];

            while (previousMethod != null && previousMethod.lexLevel >= this.lexLevel)
                previousMethod = previousMethod.Parent;

            this.Parent = previousMethod;
            //jumpTable -= 2;
            var procBase = BitConverter.ToUInt16(systemData, jumpTable - 2);
            var position = jumpTable - procBase - 2;
            //jumpTable -= 2;
            var exitIpc = procBase - BitConverter.ToUInt16(systemData, jumpTable - 4) - 2;
            //jumpTable -= 2;
            this.ParameterLength = (ByteCount)BitConverter.ToUInt16(systemData, jumpTable - 6);
            //jumpTable -= 2;
            this.DataLength = (ByteCount)BitConverter.ToUInt16(systemData, jumpTable - 8);
            //jumpTable -= 2;

            var totalLength = 0;
            while (totalLength <= exitIpc)
                totalLength += OpCode.Read(this, systemData, position + totalLength, jumpTable, position, totalLength);
        }

        public WordCount ParameterLength { get; }
        public WordCount DataLength { get; }
        public int ReturnLength { get; private set; }

        public Method? Parent { get; }

        public string Name => "M" + this.Id;

        public string Descriptor => this.ParameterLength.ToString();
        
        public Unit Unit { get; }

        public IList<OpCode> OpCodes => this.opCodes;

        public void Dump(IndentedTextWriter writer)
        {
            writer.Indent += this.lexLevel;
            writer.Write("method {0}(0x{2}) ll {1}", this.Name, this.lexLevel, this.ParameterLength);
            if ((int)this.DataLength > 0)
                writer.Write(" datasize 0x{0:X}", this.DataLength);
            writer.WriteLine(':');
            writer.Indent++;
            var address = 0;
            foreach (var opCode in this.opCodes)
            {
                writer.Write("0x{0:X4}: ", address);
                writer.Indent += 8;
                opCode.Dump(writer);
                writer.Indent -= 8;
                address += opCode.Length;
            }
            writer.Indent--;
            writer.Indent -= this.lexLevel;
        }
    }

    public class OpCode
    {
        public static int Read(Method method, byte[] systemData, int totalLength, int jumpTable, int position, int i)
        {
            throw new NotImplementedException();
        }

        public void Dump(IndentedTextWriter writer)
        {
            throw new NotImplementedException();
        }

        public int Length { get; set; }
    }
}