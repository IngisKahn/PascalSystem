namespace PascalSystem.Model
{
    using System;
    using System.CodeDom.Compiler;
    using System.IO;
    using System.Reflection.Emit;
    using System.Text;

    public partial class OpCode
    {
        public OpCode(OpCodeValue code) => this.Id = code;
        public OpCodeValue Id { get; }

        public virtual int Length => 1;

        public virtual void Dump(IndentedTextWriter writer) => writer.WriteLine(this);

        internal static int Read(Method method, byte[] systemData, int position, int jTab, int procBase, int ipc)
        {
            var code = (OpCodeValue)systemData[position++];

            switch (code)
            {
                case OpCodeValue.LDCI:
                    return method.AddOpCode(new ConstantWord(BitConverter.ToUInt16(systemData, position)));
                case OpCodeValue.LDL:
                case OpCodeValue.LLA:
                case OpCodeValue.STL:
                    return method.AddOpCode(new LocalWord(code, OpCode.ReadBig(systemData, position)));
                case OpCodeValue.LDO:
                case OpCodeValue.LAO:
                case OpCodeValue.SRO:
                    return method.AddOpCode(new GlobalWord(code, OpCode.ReadBig(systemData, position)));
                case OpCodeValue.LOD:
                case OpCodeValue.LDA:
                case OpCodeValue.STR:
                    return method.AddOpCode(new IntermediateWord(code, systemData[position++],
                        OpCode.ReadBig(systemData, position)));
                case OpCodeValue.LDE:
                case OpCodeValue.LAE:
                case OpCodeValue.STE:
                    return method.AddOpCode(new ExternalWord(code,
                        ((Unit)method.Unit).Container.UnitMap[systemData[position]].Name, systemData[position++],
                        OpCode.ReadBig(systemData, position)));
                case OpCodeValue.LDC:
                {
                    var count = systemData[position++];
                    var value = new int[count];
                    for (var i = 0; i < count; i++)
                    {
                        value[i] = BitConverter.ToUInt16(systemData, position);
                        position += 2;
                    }

                    return method.AddOpCode(new ConstantWordList(value));
                }
                case OpCodeValue.LDM:
                case OpCodeValue.STM:
                case OpCodeValue.SAS:
                case OpCodeValue.ADJ:
                case OpCodeValue.CLP:
                case OpCodeValue.CGP:
                case OpCodeValue.CIP:
                case OpCodeValue.CBP:
                    return method.AddOpCode(new CountByte(code, systemData[position]));
                case OpCodeValue.LSA:
                    return
                        method.AddOpCode(new ConstantString(Encoding.ASCII.GetString(systemData, position + 1,
                            systemData[position])));
                case OpCodeValue.MOV:
                case OpCodeValue.IND:
                case OpCodeValue.INC:
                case OpCodeValue.IXA:
                    return method.AddOpCode(new CountBig(code, OpCode.ReadBig(systemData, position)));
                case OpCodeValue.IXP:
                    return method.AddOpCode(new IndexPacked(systemData[position], systemData[position + 1]));
                case OpCodeValue.EQU:
                case OpCodeValue.NEQ:
                case OpCodeValue.LEQ:
                case OpCodeValue.LES:
                case OpCodeValue.GEQ:
                case OpCodeValue.GRT:
                    return method.AddOpCode(new Type(code, systemData[position]));
                case OpCodeValue.UJP:
                case OpCodeValue.FJP:
                case OpCodeValue.EFJ:
                case OpCodeValue.NFJ:
                {
                    int address = (sbyte)systemData[position];
                    var isInTable = address < 0;
                    if (isInTable)
                        address = BitConverter.ToUInt16(systemData, jTab - 2) + 2
                                  - (BitConverter.ToUInt16(systemData, jTab + address) - address);
                    else
                        address += ipc + 2;
                    return method.AddOpCode(new Jump(code, isInTable, address, code != OpCodeValue.UJP));
                }
                case OpCodeValue.XJP:
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

                    return method.AddOpCode(new JumpTable(min, addrs, def, isPadded));
                }
                case OpCodeValue.CXP:
                    return
                        method.AddOpCode(
                            new ExternalCall(((Unit)method.Unit).Container.UnitMap[systemData[position]].Name,
                                systemData[position++], systemData[position]));
                case OpCodeValue.CSP:
                    return method.AddOpCode(new CallStandardProcedure(systemData[position]));
                case OpCodeValue.RNP:
                case OpCodeValue.RBP:
                    return method.AddOpCode(new ExitByte(systemData[position]));
                    //return method.AddOpCode(new Exit(code));
                case OpCodeValue.XIT:
                    return method.AddOpCode(new Exit(code, false));
            }

            return method.AddOpCode(new(code));
        }

        private static int ReadBig(byte[] data, int position)
        {
            var value = (int)data[position];

            if (value >= 0x80)
                value = (value & 0x7F) << 8 | data[position + 1];

            return value;
        }

        public override string ToString() => Enum.GetName(typeof(OpCodeValue), this.Id) ?? "INVALID";

        public override int GetHashCode() => this.Id.GetHashCode();
    }
}