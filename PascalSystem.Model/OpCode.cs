namespace PascalSystem.Model
{
    using System;
    using System.CodeDom.Compiler;
    using System.IO;
    using System.Reflection.Emit;
    using System.Text;

    public class OpCode
    {
        public OpCode(int code) => this.Id = code;
        public int Id { get; }

        public virtual int Length => 1;

        public void Save(BinaryWriter writer) => throw new NotImplementedException();

        public virtual void Dump(IndentedTextWriter writer) => writer.WriteLine(this);

        internal static int Read(Method method, byte[] systemData, int position, int jTab, int procBase, int ipc)
        {
            var code = systemData[position++];

            switch ((OpcodeValue)code)
            {
                case OpcodeValue.LDCI:
                    return method.AddOpCode(new OpCodeConstantWord(BitConverter.ToUInt16(systemData, position)));
                case OpcodeValue.LDL:
                case OpcodeValue.LLA:
                case OpcodeValue.STL:
                    return method.AddOpCode(new OpCodeLocalWord(code, OpCode.ReadBig(systemData, position)));
                case OpcodeValue.LDO:
                case OpcodeValue.LAO:
                case OpcodeValue.SRO:
                    return method.AddOpCode(new OpCodeGlobalWord(code, OpCode.ReadBig(systemData, position)));
                case OpcodeValue.LOD:
                case OpcodeValue.LDA:
                case OpcodeValue.STR:
                    return method.AddOpCode(new OpCodeIntermediateWord(code, systemData[position++],
                        OpCode.ReadBig(systemData, position)));
                case OpcodeValue.LDE:
                case OpcodeValue.LAE:
                case OpcodeValue.STE:
                    return method.AddOpCode(new OpCodeExternalWord(code,
                        ((Unit)method.Unit).Container.UnitMap[systemData[position]].Name, systemData[position++],
                        OpCode.ReadBig(systemData, position)));
                case OpcodeValue.LDC:
                    {
                        var count = systemData[position++];
                        var value = new int[count];
                        for (var i = 0; i < count; i++)
                        {
                            value[i] = BitConverter.ToUInt16(systemData, position);
                            position += 2;
                        }
                        return method.AddOpCode(new OpCodeConstantWordList(value));
                    }
                case OpcodeValue.LDM:
                case OpcodeValue.STM:
                case OpcodeValue.SAS:
                case OpcodeValue.ADJ:
                case OpcodeValue.CLP:
                case OpcodeValue.CGP:
                case OpcodeValue.CIP:
                case OpcodeValue.CBP:
                    return method.AddOpCode(new OpCodeCountByte(code, systemData[position]));
                case OpcodeValue.LSA:
                    return
                        method.AddOpCode(new OpCodeConstantString(Encoding.ASCII.GetString(systemData, position + 1,
                            systemData[position])));
                case OpcodeValue.MOV:
                case OpcodeValue.IND:
                case OpcodeValue.INC:
                case OpcodeValue.IXA:
                    return method.AddOpCode(new OpCodeCountBig(code, OpCode.ReadBig(systemData, position)));
                case OpcodeValue.IXP:
                    return method.AddOpCode(new OpCodeIndexPacked(systemData[position], systemData[position + 1]));
                case OpcodeValue.EQU:
                case OpcodeValue.NEQ:
                case OpcodeValue.LEQ:
                case OpcodeValue.LES:
                case OpcodeValue.GEQ:
                case OpcodeValue.GRT:
                    return method.AddOpCode(new OpCodeType(code, systemData[position]));
                case OpcodeValue.UJP:
                case OpcodeValue.FJP:
                case OpcodeValue.EFJ:
                case OpcodeValue.NFJ:
                    {
                        int address = (sbyte)systemData[position];
                        var isInTable = address < 0;
                        if (isInTable)
                            address = BitConverter.ToUInt16(systemData, jTab - 2) + 2
                                      - (BitConverter.ToUInt16(systemData, jTab + address) - address);
                        else
                            address += ipc + 2;
                        return method.AddOpCode(new OpCodeJump(code, isInTable, address, code != (int)OpcodeValue.UJP));
                    }
                case OpcodeValue.XJP:
                    {
                        var lastPosition = position;
                        position = position + 1 & ~1;
                        var isPadded = position != lastPosition;
                        var min = BitConverter.ToInt16(systemData, position);
                        var max = BitConverter.ToInt16(systemData, position + 2);
                        int def = (sbyte)systemData[position + 5];
                        if (def < 0)
                            def = BitConverter.ToUInt16(systemData, jTab - 2) + 2
                                  - (BitConverter.ToUInt16(systemData, jTab + def) - def);
                        else
                            def += position + 6 - procBase;
                        position += 6;
                        var addrs = new int[max - min + 1];
                        for (var i = 0; i < max - min + 1; i++)
                        {
                            var a = position + i * 2 - procBase - BitConverter.ToUInt16(systemData, position + i * 2);
                            // fix inner opcode jump
                            if (a == position - procBase - 2)
                                a = def;
                            addrs[i] = a;
                        }
                        return method.AddOpCode(new OpCodeJumpTable(min, addrs, def, isPadded));
                    }
                case OpcodeValue.CXP:
                    return
                        method.AddOpCode(
                            new OpCodeExternalCall(((Unit)method.Unit).Container.UnitMap[systemData[position]].Name,
                                systemData[position++], systemData[position]));
                case OpcodeValue.CSP:
                    return method.AddOpCode(new OpCodeCallStandardProcedure(systemData[position]));
                case OpcodeValue.RNP:
                    return method.AddOpCode(new OpCodeExitByte(systemData[position]));
                case OpcodeValue.RBP:
                    return method.AddOpCode(new OpCodeExit(code));
                case OpcodeValue.XIT:
                    return method.AddOpCode(new OpCodeExit(code, false));
            }

            return method.AddOpCode(new OpCode(code));
        }

        private static int ReadBig(byte[] data, int position)
        {
            var value = (int)data[position];

            if (value >= 0x80)
                value = (value & 0x7F) << 8 | data[position + 1];

            return value;
        }

        public override string ToString() => Enum.GetName(typeof(OpcodeValue), this.Id) ?? "INVALID";

        public override int GetHashCode() => this.Id.GetHashCode();
    }
}