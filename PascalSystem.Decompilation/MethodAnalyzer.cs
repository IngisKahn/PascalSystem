namespace PascalSystem.Decompilation
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Expressions;
    using Model;
    using Types;
    using Void = Types.Void;

    public class MethodAnalyzer : IEnumerable<BasicBlock>
    {
        private readonly Model.Method method;
        private readonly MethodSignature signature;
        private readonly Decompiler decompiler;
        private bool decompiled;
        private Dictionary<int, int> opAddressToIndex = new();

        public MethodAnalyzer(Decompiler decompiler, Model.Method method)
        {
            this.decompiler = decompiler;
            this.method = method;
            this.signature = new(method);
        }

        public IEnumerator<BasicBlock> GetEnumerator() => throw new NotImplementedException();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        private readonly List<BasicBlock> blockList = new();

        public List<Statement> Statements { get; } = new();

        private record DecompilerState(BasicBlock CurrentBlock, Stack<Expressions.Expression> VmStack);

        public void Decompile()
        {
            var opCodes = this.method.OpCodes;
            if (opCodes.Count == 0 || this.decompiled)
                return;
            this.decompiled = true;

            var blockIndexes = new int[opCodes.Count];
            var opIndexToAddress = new int[opCodes.Count];
            

            this.blockList.Add(new(this, 0, 0, opCodes.Count));

            DecompilerState state = new(this.blockList[0], new());
            
            for (int i = 0, address = 0; i < opCodes.Count; i++)
            {
                var opCode = opCodes[i];
                opIndexToAddress[i] = address;


                this.Decompile(opCode, i, state);


                address += opCode.Length;
            }
        }

        private void Decompile(OpCode opCode, int index, DecompilerState state)
        {
            if (opCode.Id <= OpcodeValue.SLDC_127) // One word Load and Stores constant
            {
                state.VmStack.Push(Expressions.Expression.Constant((int)opCode.Id));
                return;
            }

            WordCount offset;
            switch (opCode.Id)
            {
                case OpcodeValue.NOP:
                    break;
                case OpcodeValue.LDCN: // Load Constant Nil
                    state.VmStack.Push(Expression.Constant<Pointer>(null));
                    break;
                case OpcodeValue.LDCI: // Load Constant Integer
                    state.VmStack.Push(Expression.Constant<Integer>(((OpCode.ConstantWord)opCode).Value));
                    break;// One-word load and stores local
                // SLDL Short LoaD Local 1..16 
                case OpcodeValue.SLDL_1:
                case OpcodeValue.SLDL_2:
                case OpcodeValue.SLDL_3:
                case OpcodeValue.SLDL_4:
                case OpcodeValue.SLDL_5:
                case OpcodeValue.SLDL_6:
                case OpcodeValue.SLDL_7:
                case OpcodeValue.SLDL_8:
                case OpcodeValue.SLDL_9:
                case OpcodeValue.SLDL_10:
                case OpcodeValue.SLDL_11:
                case OpcodeValue.SLDL_12:
                case OpcodeValue.SLDL_13:
                case OpcodeValue.SLDL_14:
                case OpcodeValue.SLDL_15:
                case OpcodeValue.SLDL_16:
                case OpcodeValue.LDL: //Load  Local
                    offset = (WordCount)(opCode.Id == OpcodeValue.LDL
                        ? ((OpCode.LocalWord)opCode).Offset
                        : opCode.Id - OpcodeValue.SLDL_1 + 1);
                    state.VmStack.Push(this.LocalVariable(new Types.SizeRange((BitCount)1, (BitCount)16), offset));
                    break;
                case OpcodeValue.LLA: // Load Local Address
                    offset = (WordCount)((OpCode.LocalWord)opCode).Offset;
                    state.VmStack.Push(Expression.AddressOf(
                        this.LocalVariable(Void.Instance, offset)));
                    break;
                case OpcodeValue.STL: // Store Local
                    offset = (WordCount)((OpCode.LocalWord)opCode).Offset;
                    this.Statements.Add(Expression.Assign(
                        this.LocalVariable(new SizeRange((BitCount)1, (BitCount)16), offset), state.VmStack.Pop()));
                    break;
                default:
                    //throw new DecompilationException("Invalid Op Code: " + opCode.Id);
                    return;
            }
        }

        private Expression LocalVariable(Base type, WordCount offset)
        {
            offset -= (WordCount)1;
            if ((int)offset < (int)this.method.ParameterLength)
                return Expression.Parameter(offset /*+ (WordCount)method.ParentParameterOffset*/, this.signature.Parameters.MeetAt(offset, type));
            offset -= method.ParameterLength; // -method.ParametersSize + (WordCount)method.ParentLocalOffset
            return Expression.Local(offset, this.Locals.MeetAt(offset, type));
        }
    }
}