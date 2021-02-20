namespace PascalSystem.Decompilation
{
    using System;
    using System.CodeDom.Compiler;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Expressions;
    using Model;
    using Types;
    using Void = Types.Void;

    public class MethodAnalyzer : IEnumerable<BasicBlock>
    {
        private readonly Model.Method method;
        public MethodSignature Signature { get; }
        private readonly Decompiler decompiler;
        private bool decompiled;
        private Dictionary<int, int> opAddressToIndex = new();
        public int ParentId { get; set; } = -1;
        public HashSet<int> ChildIds { get; } = new();
        public bool IsFunction => this.Signature.IsFunction;

        public MethodAnalyzer(Decompiler decompiler, Model.Method method)
        {
            this.decompiler = decompiler;
            this.method = method;
            this.Signature = new(method);
            this.Locals = new(method.DataLength);
            this.Level = method.Level;
        }

        public IEnumerator<BasicBlock> GetEnumerator() => throw new NotImplementedException();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        private readonly List<BasicBlock> blockList = new();

        public Interval Locals { get; }

        public List<Expression> Statements { get; } = new();
        public int Level { get; }

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
            if (opCode.Id <= OpCodeValue.SLDC_127) // One word Load and Stores constant
            {
                state.VmStack.Push(Expression.Constant((int)opCode.Id));
                return;
            }

            WordCount offset;
            Types.Base type;
            switch (opCode.Id)
            {
                case OpCodeValue.NOP:
                    break;
                case OpCodeValue.LDCN: // Load Constant Nil
                    state.VmStack.Push(Expression.Constant<Pointer>(null));
                    break;
                case OpCodeValue.LDCI: // Load Constant Integer
                    state.VmStack.Push(Expression.Constant<Integer>(((OpCode.ConstantWord)opCode).Value));
                    break;// One-word load and stores local
                // SLDL Short LoaD Local 1..16 
                case OpCodeValue.SLDL_1:
                case OpCodeValue.SLDL_2:
                case OpCodeValue.SLDL_3:
                case OpCodeValue.SLDL_4:
                case OpCodeValue.SLDL_5:
                case OpCodeValue.SLDL_6:
                case OpCodeValue.SLDL_7:
                case OpCodeValue.SLDL_8:
                case OpCodeValue.SLDL_9:
                case OpCodeValue.SLDL_10:
                case OpCodeValue.SLDL_11:
                case OpCodeValue.SLDL_12:
                case OpCodeValue.SLDL_13:
                case OpCodeValue.SLDL_14:
                case OpCodeValue.SLDL_15:
                case OpCodeValue.SLDL_16:
                case OpCodeValue.LDL: //Load  Local
                    offset = (WordCount)(opCode.Id == OpCodeValue.LDL
                        ? ((OpCode.LocalWord)opCode).Offset
                        : opCode.Id - OpCodeValue.SLDL_1 + 1);
                    state.VmStack.Push(this.LocalVariable(new Types.SizeRange((BitCount)1, (BitCount)16), offset));
                    break;
                case OpCodeValue.LLA: // Load Local Address
                    offset = (WordCount)((OpCode.LocalWord)opCode).Offset;
                    state.VmStack.Push(Expression.AddressOf(
                        this.LocalVariable(Void.Instance, offset)));
                    break;
                case OpCodeValue.STL: // Store Local
                    offset = (WordCount)((OpCode.LocalWord)opCode).Offset;
                    this.Statements.Add(Expression.Assign(
                        this.LocalVariable(new SizeRange((BitCount)1, (BitCount)16), offset), state.VmStack.Pop()));
                    break;// One-word load and stores global
                          // Short Load Global Word
                case OpCodeValue.SLDO_1:
                case OpCodeValue.SLDO_2:
                case OpCodeValue.SLDO_3:
                case OpCodeValue.SLDO_4:
                case OpCodeValue.SLDO_5:
                case OpCodeValue.SLDO_6:
                case OpCodeValue.SLDO_7:
                case OpCodeValue.SLDO_8:
                case OpCodeValue.SLDO_9:
                case OpCodeValue.SLDO_10:
                case OpCodeValue.SLDO_11:
                case OpCodeValue.SLDO_12:
                case OpCodeValue.SLDO_13:
                case OpCodeValue.SLDO_14:
                case OpCodeValue.SLDO_15:
                case OpCodeValue.SLDO_16:
                case OpCodeValue.LDO: // Load Global
                    offset = (WordCount)(opCode.Id == OpCodeValue.LDO
                        ? ((OpCode.GlobalWord)opCode).Offset
                        : opCode.Id - OpCodeValue.SLDO_1 + 1);
                    type = this.decompiler.Globals[this.method.Unit.Number - 1].MeetAt(offset, new Types.SizeRange((BitCount)1, (BitCount)16));
                    state.VmStack.Push(Expression.Global(offset, type));
                    break;
                case OpCodeValue.LAO: // Load Address Global
                    offset = (WordCount)((OpCode.GlobalWord)opCode).Offset;
                    type = this.decompiler.Globals[this.method.Unit.Number - 1].MeetAt(offset, Types.Void.Instance);
                    state.VmStack.Push(Expression.AddressOf(Expression.Global(offset, type)));
                    break;
                case OpCodeValue.SRO: // Store Global
                    offset = (WordCount)((OpCode.GlobalWord)opCode).Offset;
                    type = this.decompiler.Globals[this.method.Unit.Number - 1].MeetAt(offset, new Types.SizeRange((BitCount)1, (BitCount)16));
                    this.Statements.Add(Expression.Assign(Expression.Global(offset, type), state.VmStack.Pop()));
                    break;

                case OpCodeValue.LSA: // Load String Address
                    state.VmStack.Push(Expression.Constant<Types.String>(((OpCode.ConstantString)opCode).Value));
                    break;
                case OpCodeValue.CBP: // Call Base Procedure - call the main program
                case OpCodeValue.CXP: // Call External Procedure - call a global procedure in another unit
                case OpCodeValue.CGP: // Call Global Procedure - call a top level procedure in same unit
                case OpCodeValue.CLP: // Call Local Procedure - call a direct child procedure, passing locals along
                case OpCodeValue.CIP: // Call Intermediate Procedure - call a child procedure of a parent procedure,
                                      //                               passing that scope's locals along
                    {
                        var isExternal = opCode.Id == OpCodeValue.CXP;
                        MethodAnalyzer methodAnalyzer;
                        if (isExternal)
                        {
                            var cxp = (OpCode.ExternalCall)opCode;
                            var id = cxp.Proc;
                            methodAnalyzer = this.decompiler.GetMethod(cxp.SegNumber, id);
                        }
                        else
                        {
                            var id = ((OpCode.CountByte)opCode).Count;
                            methodAnalyzer = this.decompiler.GetMethod(this.method.Unit.Number, id);
                            if (methodAnalyzer.Level > 1) // unit 0 is special
                                switch (opCode.Id) // this is our only opportunity to discover procedure hierarchy
                                {
                                    case OpCodeValue.CLP:
                                        methodAnalyzer.ParentId = this.method.Id;
                                        this.ChildIds.Add(id);
                                        break;
                                    case OpCodeValue.CIP:
                                        var callLevel = methodAnalyzer.Level;
                                        var parent = this;
                                        while (parent.Level >= callLevel)
                                            parent = this.decompiler.GetMethod(parent.method.Unit.Number, parent.ParentId);

                                        methodAnalyzer.ParentId = parent.method.Id;
                                        parent.ChildIds.Add(id);
                                        break;
                                }
                        }
                        var size = methodAnalyzer.method.ParameterLength;
                        var p = new List<Expression>();
                        while ((int)size > 0)
                        {
                            Debug.Assert(state.VmStack.Count != 0);
                            var e = state.VmStack.Pop();
                            // ReSharper disable once RedundantCast <- Resharper is wrong
                            size -= (WordCount)e.Type.Size;
                            p.Add(e);
                        }
                        Debug.Assert((int)size == 0);
                        var call = Expression.Call(methodAnalyzer, p, isExternal);
                        if (call.MethodInfo.Signature.ReturnType.ResolvesTo<Types.Void>())
                            this.Statements.Add(call);
                        else
                            state.VmStack.Push(call);
                    }
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
                return Expression.Parameter(offset /*+ (WordCount)method.ParentParameterOffset*/, this.Signature.Parameters.MeetAt(offset, type));
            offset -= method.ParameterLength; // -method.ParametersSize + (WordCount)method.ParentLocalOffset
            return Expression.Local(offset, this.Locals.MeetAt(offset, type));
        }

        public async Task Dump(IndentedTextWriter writer)
        {
            await this.Signature.Dump(writer);
            await this.DumpCode(writer);
        }

        public async Task DumpCode(IndentedTextWriter writer)
        {
            await writer.WriteLineAsync("BEGIN");
            writer.Indent++;
            foreach (var statement in this.Statements)
                await statement.Dump(writer);
            writer.Indent--;
            await writer.WriteLineAsync("END; {" + this.Signature.Name + "}");
            await writer.WriteLineAsync();
        }
    }
}