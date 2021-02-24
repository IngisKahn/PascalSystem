namespace PascalSystem.Decompilation
{
    using System;
    using System.CodeDom.Compiler;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Xml.Linq;
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

        public List<BasicBlock> BlockList { get; } = new();
        private int[]? opIndexToAddress;
        private int[]? blockIndexes;

        public Interval Locals { get; }

        public int Level { get; }

        private record DecompilerState(BasicBlock CurrentBlock, Stack<Expressions.Expression> VmStack);

        private BasicBlock OpIndexToBlock(int index) => this.BlockList[this.blockIndexes?[index] ?? throw new DecompilationException()];
        private BasicBlock AddressToBlock(int address) => this.OpIndexToBlock(this.opAddressToIndex[address]);
        private void Split(int fromIndex, int toIndex, bool addEdge = true)
        {
            if (this.blockIndexes is null || toIndex >= this.blockIndexes.Length)
                return;
            var fromBlockIndex = this.blockIndexes[fromIndex];
            var fromBlock = this.BlockList[fromBlockIndex];

            var splitFrom = fromBlock.SplitAt(fromIndex + 1);
            if (splitFrom != null)
            {
                this.BlockList.Insert(fromBlockIndex + 1, splitFrom);
                fromIndex++;
                while (fromIndex < this.blockIndexes.Length)
                    this.blockIndexes[fromIndex++]++;
            }
            var toBlockIndex = this.blockIndexes[toIndex];
            var toBlock = this.BlockList[toBlockIndex];
            var splitTo = toBlock.SplitAt(toIndex);
            if (splitTo != null)
            {
                this.BlockList.Insert(toBlockIndex + 1, splitTo);
                while (toIndex < this.blockIndexes.Length)
                    this.blockIndexes[toIndex++]++;
                toBlock = splitTo;
            }
            if (addEdge)
                fromBlock.AddEdge(toBlock);
        }
        public void Decompile()
        {
            var opCodes = this.method.OpCodes;
            if (opCodes.Count == 0 || this.decompiled)
                return;
            this.decompiled = true;

            this.blockIndexes = new int[opCodes.Count];
            this.opIndexToAddress = new int[opCodes.Count];


            this.BlockList.Add(new(this, 0, 0, opCodes.Count));
            
            // map the addresses
            for (int i = 0, address = 0; i < opCodes.Count; address += opCodes[i++].Length)
            {
                this.opAddressToIndex[address] = i;
                this.opIndexToAddress[i] = address;
            }
            
            // split blocks
            for (var index = 0; index < opCodes.Count; index++)
                switch (opCodes[index])
                {
                    case OpCode.Jump jump:
                        // split after jump
                        this.Split(index, index + 1, jump.IsConditional);
                        var target = this.opAddressToIndex[jump.Address];
                        //split target
                        this.Split(index, target);
                        if (target == 0)
                            break;
                        // add edge if fall thru from pre target
                        var preTarget = opCodes[target - 1];
                        if (preTarget is OpCode.Jump {IsConditional: true}
                            || preTarget is OpCode.JumpTable || preTarget is OpCode.Exit)
                            break;
                        this.OpIndexToBlock(target - 1).AddEdge(this.OpIndexToBlock(target));
                        break;
                    case OpCode.JumpTable switchCase:
                        // split to default (may be else block or after)
                        this.Split(index, this.opAddressToIndex[switchCase.DefaultAddress]);
                        foreach (var address in switchCase.Addresses)
                            this.Split(index, this.opAddressToIndex[address]);
                        break;
                    case OpCode.Exit:
                        // split with no edge
                        this.Split(index, index + 1, false);
                        break;
                }

            DominatorData[] data = this.BlockList.Select(_ => new DominatorData()).ToArray();
            var n = 0;
            void Dfs(int v)
            {
                var vData = data[v];
                vData.Semi = ++n;
                data[n].Vertex = vData.Label = v;
                foreach (var w in this.BlockList[v].EdgesOut.Select(e => e.Destination.Id))
                {
                    var wData = data[w];
                    if (wData.Semi == 0)
                    {
                        wData.Parent = v;
                        Dfs(w);
                    }

                    wData.Pred.Add(v);
                }
            }

            Dfs(0);

            void Compress(int v)
            {
                var vData = data[v];
                if (data[vData.Ancestor].Ancestor == 0)
                    return;
                Compress(vData.Ancestor);
                if (data[data[vData.Ancestor].Label].Semi < data[vData.Label].Semi)
                    vData.Label = data[vData.Ancestor].Label;
                vData.Ancestor = data[vData.Ancestor].Ancestor;
            }

            int Eval(int v)
            {
                var vData = data[v];
                return vData.Ancestor == 0
                    ? vData.Label
                    : data[data[vData.Ancestor].Label].Semi >= data[vData.Label].Semi
                        ? vData.Label
                        : data[vData.Ancestor].Label;
            }

            void Link(int v, int w)
            {
                var s = w;
                var vData = data[v];
                var wData = data[w];
                while (data[wData.Label].Semi < data[data[data[s].Child].Label].Semi)
                    if (data[s].Size + data[data[data[s].Child].Child].Size >= 2 * data[data[s].Child].Size)
                    {
                        data[data[s].Child].Ancestor = s;
                        data[s].Child = data[data[s].Child].Child;
                    }
                    else
                    {
                        data[data[s].Child].Size = data[s].Size;
                        s = data[s].Ancestor = data[s].Child;
                    }

                data[s].Label = wData.Label;
                vData.Size += wData.Size;
                if (vData.Size < 2 * wData.Size)
                {
                    var t = s;
                    s = vData.Child;
                    vData.Child = t;
                }

                while (s != 0)
                {
                    data[s].Ancestor = v;
                    s = data[s].Child;
                }
            }

            var dom = new int[this.BlockList.Count];
            for (var i = n; i > 2; i--)
            {
                var w = data[i].Vertex;
                var wData = data[w];
                foreach (var uData in wData.Pred.Select(v => data[Eval(v)])
                                                .Where(uData => uData.Semi < wData.Semi))
                    wData.Semi = uData.Semi;

                data[data[wData.Semi].Vertex].Bucket.Add(w);
                Link(wData.Parent, w);
                foreach (var v in data[wData.Parent].Bucket)
                {
                    data[wData.Parent].Bucket.Remove(v);
                    var u = Eval(v);
                    dom[v] = data[u].Semi < data[v].Semi ? u : wData.Parent;
                }
            }

            for (var i = 2; i < n; i++)
            {
                var w = data[i].Vertex;
                if (dom[w] != data[data[w].Semi].Vertex)
                    dom[w] = dom[dom[w]];
            }

            // re number
            for (var i = 0; i < this.BlockList.Count; i++)
            {
                var block = this.BlockList[i];
                block.Id = i;
                //block.Address = this.opIndexToAddress[block.StartIndex];
            }

            Queue<DecompilerState> stateQueue = new();
            stateQueue.Enqueue(new(this.BlockList[0], new()));
            var visited = new bool[this.BlockList.Count];
            while (stateQueue.Count > 0)
            {
                var (currentBlock, vmStack) = stateQueue.Dequeue();
                visited[currentBlock.Id] = true;
                for (var i = currentBlock.StartIndex; i <= currentBlock.EndIndex; i++)
                {
                    var opCode = opCodes[i];
                    this.Decompile(opCode, i, currentBlock.Statements, vmStack);
                }

                foreach (var controlEdge in from controlEdge in currentBlock.EdgesOut where !visited[controlEdge.Destination.Id] select controlEdge)
                    stateQueue.Enqueue(new(controlEdge.Destination, new(vmStack)));
            }
        }

        private class DominatorData
        {
            public int Parent { get; set; }
            public int Ancestor { get; set; }
            public int Child { get; set; }
            public int Vertex { get; set; }
            public int Label { get; set; }
            public int Semi { get; set; }
            public int Size { get; set; } = 1;
            public HashSet<int> Pred { get; } = new();
            public HashSet<int> Bucket { get; } = new();
        }

        private void Decompile(OpCode opCode, int index, List<Expression> statements, Stack<Expression> vmStack)
        {
            if (opCode.Id <= OpCodeValue.SLDC_127) // One word Load and Stores constant
            {
                vmStack.Push(Expression.Constant((int)opCode.Id));
                return;
            }

            WordCount offset;
            Types.Base type;
            Expression tempX;
            switch (opCode.Id)
            {
                case OpCodeValue.NOP:
                    break;
                case OpCodeValue.LDCN: // Load Constant Nil
                    vmStack.Push(Expression.Constant<Pointer>(null));
                    break;
                case OpCodeValue.LDCI: // Load Constant Integer
                    vmStack.Push(Expression.Constant<Integer>(((OpCode.ConstantWord)opCode).Value));
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
                    vmStack.Push(this.LocalVariable(new Types.SizeRange((BitCount)1, (BitCount)16), offset));
                    break;
                case OpCodeValue.LLA: // Load Local Address
                    offset = (WordCount)((OpCode.LocalWord)opCode).Offset;
                    vmStack.Push(Expression.AddressOf(
                        this.LocalVariable(Void.Instance, offset)));
                    break;
                case OpCodeValue.STL: // Store Local
                    offset = (WordCount)((OpCode.LocalWord)opCode).Offset;
                    statements.Add(Expression.Assign(
                        this.LocalVariable(new SizeRange((BitCount)1, (BitCount)16), offset), vmStack.Pop()));
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
                    vmStack.Push(Expression.Global(offset, type));
                    break;
                case OpCodeValue.LAO: // Load Address Global
                    offset = (WordCount)((OpCode.GlobalWord)opCode).Offset;
                    type = this.decompiler.Globals[this.method.Unit.Number - 1].MeetAt(offset, Types.Void.Instance);
                    vmStack.Push(Expression.AddressOf(Expression.Global(offset, type)));
                    break;
                case OpCodeValue.SRO: // Store Global
                    offset = (WordCount)((OpCode.GlobalWord)opCode).Offset;
                    type = this.decompiler.Globals[this.method.Unit.Number - 1].MeetAt(offset, new Types.SizeRange((BitCount)1, (BitCount)16));
                    statements.Add(Expression.Assign(Expression.Global(offset, type), vmStack.Pop()));
                    break;

                case OpCodeValue.LSA: // Load String Address
                    vmStack.Push(Expression.Constant<Types.String>(((OpCode.ConstantString)opCode).Value));
                    break;
                case OpCodeValue.ADI:
                case OpCodeValue.SBI:
                case OpCodeValue.MPI:
                case OpCodeValue.DVI:
                case OpCodeValue.MODI:
                case OpCodeValue.LAND: // Logical And
                case OpCodeValue.LOR: // Logical Or
                    tempX = vmStack.Pop();
                    vmStack.Push(Expression.BinaryIMath(opCode.Id, vmStack.Pop(), tempX));
                    break;
                case OpCodeValue.ADR:
                case OpCodeValue.SBR:
                case OpCodeValue.MPR:
                case OpCodeValue.DVR:
                    tempX = vmStack.Pop();
                    vmStack.Push(Expression.BinaryRMath(opCode.Id, vmStack.Pop(), tempX));
                    break;
                case OpCodeValue.NEQI:
                case OpCodeValue.EQUI:
                case OpCodeValue.GRTI:
                case OpCodeValue.LESI:
                case OpCodeValue.GEQI:
                case OpCodeValue.LEQI:
                    tempX = vmStack.Pop();
                    vmStack.Push(Expression.Compare(opCode.Id - OpCodeValue.EQUI, 0, vmStack.Pop(), tempX));
                    break;
                case OpCodeValue.NEQ:
                case OpCodeValue.EQU:
                case OpCodeValue.GRT:
                case OpCodeValue.LES:
                case OpCodeValue.GEQ:
                case OpCodeValue.LEQ:
                    tempX = vmStack.Pop();
                    vmStack.Push(Expression.Compare(opCode.Id - OpCodeValue.EQU, ((OpCode.Type)opCode).TypeCode,
                        vmStack.Pop(), tempX));
                    break;
                case OpCodeValue.LNOT: // Logical Not
                case OpCodeValue.NGI: // Negate Integer
                case OpCodeValue.ABI: // Absolute Value Integer
                case OpCodeValue.SQI: // Square Integer
                    vmStack.Push(Expression.UnaryIMath(opCode.Id, vmStack.Pop()));
                    break;
                case OpCodeValue.NGR: // Negate Real
                case OpCodeValue.ABR: // Absolute Value Real
                case OpCodeValue.SQR: // Square Real
                    vmStack.Push(Expression.UnaryRMath(opCode.Id, vmStack.Pop()));
                    break;
                case OpCodeValue.FJP:
                    {
                        var jump = (OpCode.Jump)opCode;
                        var jumpToBlock = this.AddressToBlock(jump.Address);
                        var nextBlock = this.OpIndexToBlock(index + 1);
                        statements.Add(Expression.If(nextBlock,
                            jumpToBlock,
                            vmStack.Pop(),
                                      jumpToBlock.Id > nextBlock.Id && jumpToBlock.EdgesIn.All(e => e.Source.Id != e.Destination.Id - 1)));
                        break;
                    }
                case OpCodeValue.EFJ:
                    {
                        var jump = (OpCode.Jump)opCode;
                        var jumpToBlock = this.AddressToBlock(jump.Address);
                        var nextBlock = this.OpIndexToBlock(index + 1);
                        statements.Add(Expression.If(nextBlock,
                            jumpToBlock,
                            Expression.Compare(0, 0, vmStack.Pop(), vmStack.Pop()),
                            jumpToBlock.Id > nextBlock.Id &&
                            jumpToBlock.EdgesIn.All(e => e.Source.Id != e.Destination.Id - 1)));
                        break;
                    }
                case OpCodeValue.NFJ:
                {
                    var jump = (OpCode.Jump)opCode;
                    var jumpToBlock = this.AddressToBlock(jump.Address);
                    var nextBlock = this.OpIndexToBlock(index + 1);
                    statements.Add(Expression.If(nextBlock,
                        jumpToBlock,
                        Expression.Compare(8, 0, vmStack.Pop(), vmStack.Pop()),
                        jumpToBlock.Id > nextBlock.Id &&
                        jumpToBlock.EdgesIn.All(e => e.Source.Id != e.Destination.Id - 1)));
                    break;
                    }
                case OpCodeValue.UJP:
                    break;
                case OpCodeValue.XJP:
                    var xjp = (OpCode.JumpTable)opCode;
                    statements.Add(new Case(xjp.Minimum, this.AddressToBlock(xjp.DefaultAddress),
                        from a in xjp.Addresses select this.AddressToBlock(a), vmStack.Pop()));
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
                            Debug.Assert(vmStack.Count != 0);
                            var e = vmStack.Pop();
                            // ReSharper disable once RedundantCast <- Resharper is wrong
                            size -= (WordCount)e.Type.Size;
                            p.Add(e);
                        }
                        Debug.Assert((int)size == 0);
                        var call = Expression.Call(methodAnalyzer.Signature, p, isExternal);
                        if (call.MethodInfo.ReturnType.ResolvesTo<Types.Void>())
                            statements.Add(call);
                        else
                            vmStack.Push(call);
                    }
                    break;
                case OpCodeValue.RNP:
                    break;
                case OpCodeValue.CSP:
                    var subType = ((OpCode.CallStandardProcedure)opCode).SubType;
                    switch (subType)
                    {
                        case OpCode.CallStandardProcedure.StandardCall.CSP_IOC:
                        case OpCode.CallStandardProcedure.StandardCall.CSP_NEW:
                            throw new InvalidOperationException();
                        case OpCode.CallStandardProcedure.StandardCall.CSP_MVL:
                            {
                                var p = new List<Expression>(3);
                                var x = 3;
                                while (x-- > 0)
                                    p.Insert(0, vmStack.Pop());
                                statements.Add(
                                    Expression.Call(Decompiler.MoveLeft, p));
                            }
                            break;
                        case OpCode.CallStandardProcedure.StandardCall.CSP_MVR:
                            throw new InvalidOperationException();
                        case OpCode.CallStandardProcedure.StandardCall.CSP_XIT:
                            {
                                var proc = (short)(((Constant)vmStack.Pop()).Value ?? -1);
                                var unitNumber = (short)(((Constant)vmStack.Pop()).Value ?? -1);
                                var site = this.decompiler.GetMethod(unitNumber, proc);
                                statements.Add(new Exit(site.method.Unit.Name, proc));
                            }
                            break;
                        case OpCode.CallStandardProcedure.StandardCall.CSP_UREAD:
                        case OpCode.CallStandardProcedure.StandardCall.CSP_UWRITE:
                            {
                                var p = new List<Expression>(6);
                                var x = 6;
                                while (x-- > 0)
                                    p.Insert(0, vmStack.Pop());
                                statements.Add(
                                    Expression.Call(
                                        subType == OpCode.CallStandardProcedure.StandardCall.CSP_UREAD
                                            ? Decompiler.UnitRead
                                            : Decompiler.UnitWrite, p));
                            }
                            break;
                        case OpCode.CallStandardProcedure.StandardCall.CSP_IDS:
                        case OpCode.CallStandardProcedure.StandardCall.CSP_TRS:
                        case OpCode.CallStandardProcedure.StandardCall.CSP_TIM:
                            throw new InvalidOperationException();
                        case OpCode.CallStandardProcedure.StandardCall.CSP_FLC:
                            {
                                var p = new List<Expression>(4);
                                var x = 4;
                                while (x-- > 0)
                                    p.Insert(0, vmStack.Pop());
                                statements.Add(
                                    Expression.Call(Decompiler.FillChar, p));
                            }
                            break;
                        case OpCode.CallStandardProcedure.StandardCall.CSP_SCN:
                        case OpCode.CallStandardProcedure.StandardCall.CSP_USTAT:
                        case OpCode.CallStandardProcedure.StandardCall.CSP_LDSEG:
                        case OpCode.CallStandardProcedure.StandardCall.CSP_ULDSEG:
                        case OpCode.CallStandardProcedure.StandardCall.CSP_TRC:
                        case OpCode.CallStandardProcedure.StandardCall.CSP_RND:
                        case OpCode.CallStandardProcedure.StandardCall.CSP_SIN:
                        case OpCode.CallStandardProcedure.StandardCall.CSP_COS:
                        case OpCode.CallStandardProcedure.StandardCall.CSP_TAN:
                        case OpCode.CallStandardProcedure.StandardCall.CSP_ATAN:
                        case OpCode.CallStandardProcedure.StandardCall.CSP_LN:
                        case OpCode.CallStandardProcedure.StandardCall.CSP_EXP:
                        case OpCode.CallStandardProcedure.StandardCall.CSP_SQRT:
                            throw new InvalidOperationException();
                        case OpCode.CallStandardProcedure.StandardCall.CSP_MRK:
                            {
                                var p = new Expression[1];
                                p[0] = vmStack.Pop();
                                statements.Add(Expression.Call(Decompiler.Mark, p));
                            }
                            break;
                        case OpCode.CallStandardProcedure.StandardCall.CSP_RLS:
                            {
                                var p = new Expression[1];
                                p[0] = vmStack.Pop();
                                statements.Add(Expression.Call(Decompiler.Release, p));
                            }
                            break;
                        case OpCode.CallStandardProcedure.StandardCall.CSP_IOR:
                            vmStack.Push(Expression.Call(Decompiler.Ioresult, new Expression[0]));
                            break;
                        case OpCode.CallStandardProcedure.StandardCall.CSP_UBUSY:
                        case OpCode.CallStandardProcedure.StandardCall.CSP_POT:
                        case OpCode.CallStandardProcedure.StandardCall.CSP_UWAIT:
                            throw new InvalidOperationException();
                        case OpCode.CallStandardProcedure.StandardCall.CSP_UCLEAR:
                            {
                                var p = new Expression[1];
                                p[0] = vmStack.Pop();
                                statements.Add(Expression.Call(Decompiler.Uclear, p));
                            }
                            break;
                        case OpCode.CallStandardProcedure.StandardCall.CSP_HLT:
                            throw new InvalidOperationException();
                        case OpCode.CallStandardProcedure.StandardCall.CSP_MAV:
                            vmStack.Push(Expression.Call(Decompiler.Ioresult, new Expression[0]));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
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
            if (this.Locals?.Count > 0)
            {
                await writer.WriteLineAsync("VAR");
                writer.Indent++;
                foreach (var local in this.Locals)
                {
                    LocalVariable expression = new(local.Offset, local.Type);
                    await writer.WriteLineAsync(expression + ";");
                }
                writer.Indent--;
            }

            if (this.BlockList.Count > 0)
                await this.BlockList[0].Dump(writer);
            else
                await writer.WriteAsync(";");
            await writer.WriteLineAsync();
        }
    }
}