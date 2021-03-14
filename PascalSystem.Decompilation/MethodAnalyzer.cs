namespace PascalSystem.Decompilation
{
    using System;
    using System.CodeDom.Compiler;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
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
        private readonly Dictionary<int, int> opAddressToIndex = new();
        public int ParentId { get; set; } = -1;
        public HashSet<int> ChildIds { get; } = new();
        public bool IsFunction => this.Signature.IsFunction;

        private readonly Stack<CodeBlock> codeBlockStack = new();

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
                        if (preTarget is OpCode.Jump { IsConditional: false }
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
                        // pretend we jump to exit op - this false edge is needed for post dominator calculation
                        this.Split(index, opCodes.Count - 1);
                        break;
                }

            Queue<DecompilerState> stateQueue = new();
            stateQueue.Enqueue(new(this.BlockList[0], new()));
            var visited = new bool[this.BlockList.Count];
            visited[0] = true;
            while (stateQueue.Count > 0)
            {
                var (currentBlock, vmStack) = stateQueue.Dequeue();
                for (var i = currentBlock.StartIndex; i <= currentBlock.EndIndex && i < opCodes.Count; i++)
                {
                    var opCode = opCodes[i];
                    this.Decompile(opCode, i, currentBlock.Statements, vmStack);
                }

                foreach (var controlEdge in from controlEdge in currentBlock.EdgesOut
                                            where !visited[controlEdge.Destination.Id]
                                            select controlEdge)
                {
                    visited[controlEdge.Destination.Id] = true;
                    stateQueue.Enqueue(new(controlEdge.Destination, new(vmStack)));
                }
            }

            // merge any single edges (superfluous unconditional jumps)
            Stack<BasicBlock> blockStack = new();
            blockStack.Push(this.BlockList[0]);
            while (blockStack.Count > 0)
            {
                var current = blockStack.Pop();

                if (current.Statements.Count == 0 && current.EdgesOut.Count != 0) // this can only be a UJP
                {
                    current.Id = -1; // mark for death
                    var exitEdge = current.EdgesOut.Single();

                    var exit = exitEdge.Destination;

                    exit.EdgesIn.Remove(exitEdge);


                    foreach (var controlEdge in current.EdgesIn)
                    {
                        controlEdge.Destination = exit;
                        exit.EdgesIn.Add(controlEdge);
                        switch (controlEdge.Source.Statements.Last())
                        {
                            case If ifStatement:
                                if (ifStatement.TrueBlock == current)
                                    ifStatement.TrueBlock = exit;
                                else
                                    ifStatement.FalseBlock = exit;
                                break;
                            case Case caseStatement:
                                caseStatement.ReplaceBlock(current, exit);
                                break;
                        }

                    }

                    if (!exitEdge.IsBack)
                        blockStack.Push(exit);

                    continue;
                }

                while (current.EdgesOut.Count == 1)
                {
                    var next = current.EdgesOut[0].Destination;
                    if (next.EdgesIn.Count == 1) // one way in, just tack it on
                    {
                        current.Statements.AddRange(next.Statements);
                        current.EdgesOut = next.EdgesOut;
                        foreach (var edge in current.EdgesOut)
                            edge.Source = current;
                        next.Id = -1; // mark for death
                    }
                    else
                        break;
                }

                foreach (var block in current.EdgesOut.Where(e => !e.IsBack).Select(e => e.Destination))
                    blockStack.Push(block);
            }

            // remove skipped
            var temp = this.BlockList.ToArray();
            this.BlockList.Clear();
            this.BlockList.AddRange(temp.Where(b => b.Id >= 0));

            // re number
            for (var i = 0; i < this.BlockList.Count; i++)
            {
                var block = this.BlockList[i];
                block.Id = i;
                //block.Address = this.opIndexToAddress[block.StartIndex];
            }

            var dom = this.ComputeImmediateDominators(this.BlockList.Count, i => this.BlockList[i].EdgesOut, e => e.Destination.Id);
            List<BasicBlock> reverse = new(this.BlockList);
            reverse.Reverse();
            var pdom = this.ComputeImmediateDominators(this.BlockList.Count, i => reverse[i].EdgesIn, e => this.BlockList.Count - e.Source.Id - 1);
            pdom = pdom.Reverse().Select(d => this.BlockList.Count - d - 1).ToArray();

            for (var i = 0; i < dom.Length; i++)
            {
                var d = dom[i];
                if (d != i)
                {
                    this.BlockList[d].Dominates.Add(this.BlockList[i]);
                    this.BlockList[i].ImmediateDominator = this.BlockList[d];
                }
                d = pdom[i];
                if (d == i)
                    continue;
                this.BlockList[d].PostDominates.Add(this.BlockList[i]);
                this.BlockList[i].ImmediatePostDominator = this.BlockList[d];

            }
        }

        private int[] ComputeImmediateDominators(int count, Func<int, IEnumerable<BasicBlock.ControlEdge>> getEdges,
            Func<BasicBlock.ControlEdge, int> followEdge)
        {
            DominatorData[] data = Enumerable.Range(0, count).Select(_ => new DominatorData()).ToArray();
            var n = -1;

            void Dfs(int v)
            {
                var vData = data[v];
                vData.Semi = ++n;
                data[n].Vertex = vData.Label = v;
                foreach (var w in getEdges(v).Select(followEdge))
                {
                    //if (w >= count)
                    //    continue;
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
                if (vData.Ancestor == 0)
                    return vData.Label;
                Compress(v);
                return data[data[vData.Ancestor].Label].Semi >= data[vData.Label].Semi
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
            for (var i = n; i > 1; i--)
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

            for (var i = 1; i < n; i++)
            {
                var w = data[i].Vertex;
                if (dom[w] != data[data[w].Semi].Vertex)
                    dom[w] = dom[dom[w]];
            }

            return dom;
        }

        public enum CodeBlockType
        {
            /// <summary>
            /// This block only needs BEGIN/END if it has more than one statment
            /// </summary>
            Open,
            /// <summary>
            /// This block needs BEGIN/END - it is top level or it is an ambiguous IF/ELSE
            /// </summary>
            Fixed,
            /// <summary>
            /// This block does not need BEGIN/END
            /// </summary>
            Closed
        }
        public class CodeBlock
        {
            public BasicBlock StartBlock { get; set; }
            public BasicBlock? ExitBlock { get; set; }
            public string Label { get; }
            private CodeBlockType type;
            public int LoopLevel { get; }

            /// <summary>
            /// Create a code block from an initial Basic Block
            /// </summary>
            /// <param name="startBlock"></param>
            /// <param name="type">This block needs a BEGIN/END around it if it has more than one statement</param>
            /// <param name="loopLevel"></param>
            public CodeBlock(string label, BasicBlock startBlock, CodeBlockType type = CodeBlockType.Open, int loopLevel = 0)
            {
                this.Label = label;
                this.StartBlock = startBlock;
                this.type = type;
                this.LoopLevel = loopLevel;
            }
            public CodeBlock(string label, BasicBlock startBlock, BasicBlock exitBlock, CodeBlockType type = CodeBlockType.Open, int loopLevel = 0) : this(label, startBlock, type, loopLevel)
                => this.ExitBlock = exitBlock;

            public async Task Dump(IndentedTextWriter writer)
            {
                var currentBlock = this.StartBlock;
                var emitBeginEnd = false;
                var first = true;


                do // foreach basic block in this level
                {
                    // is the current block the start of a loop?
                    // if there are any back edges in, order them by outter to inner
                    var loopsIn = currentBlock.EdgesIn.Where(e => e.IsBack)
                                                                 .OrderBy(e => e, Comparer<BasicBlock.ControlEdge>.Create((a, b) =>
                                                                {
                                                                    if (!a.IsConditional)
                                                                        return -1;
                                                                    if (!b.IsConditional)
                                                                        return 1;
                                                                    var post = a.Source.CommonImmediatePostDominator(b.Source);
                                                                    return a.Source == post ? 1 : b.Source == post ? -1 : 0;
                                                                }))
                                                                 .ToArray();
                    var loopLevel = first ? this.LoopLevel : 0;
                    if (loopsIn.Length > loopLevel) // we are starting a loop
                    {
                        var loopExit = loopsIn[loopLevel];

                        if (this.type != CodeBlockType.Closed && first && loopExit.Destination != this.ExitBlock)
                        {
                            first = false;
                            emitBeginEnd = true;
                            await writer.WriteLineAsync("BEGIN");
                            writer.Indent++;
                        }

                        CodeBlock inner;
                        if (loopExit.IsConditional)
                        {

                            await writer.WriteLineAsync("REPEAT");
                            inner = new("REPEAT", currentBlock, loopExit.Source.ImmediatePostDominator ?? throw new InvalidOperationException(), CodeBlockType.Closed, loopLevel + 1);
                            //writer.Indent++;
                            await inner.Dump(writer);
                            //writer.Indent--;
                            await writer.WriteAsync("UNTIL ");
                            await ((If)loopExit.Source.Statements.Last()).Expression.DumpLine(writer);
                        }
                        else
                        {
                            await writer.WriteAsync("WHILE ");
                            await ((If)currentBlock.Statements.Single()).Expression.DumpLine(writer);
                            await writer.WriteLineAsync(" DO");
                            inner = new("WHILE", currentBlock, loopExit.Source.ImmediatePostDominator ?? throw new InvalidOperationException(), CodeBlockType.Open, loopLevel + 1);
                            writer.Indent++;
                            await inner.Dump(writer);
                            writer.Indent--;
                            await writer.WriteLineAsync("END {WHILE}");

                        }
                        currentBlock = loopExit.Source.ImmediatePostDominator;
                    }

                    if (first)
                    {
                        if (this.type != CodeBlockType.Closed)
                            if (this.type == CodeBlockType.Fixed || currentBlock.Statements.Count > 1 ||
                                currentBlock.ImmediatePostDominator != this.ExitBlock)
                            {
                                emitBeginEnd = true;
                                await writer.WriteLineAsync("BEGIN");
                            }
                        writer.Indent++;
                    }

                    first = false;

                    BasicBlock? next = null;

                    foreach (var statement in currentBlock.Statements)
                    {
                        switch (statement)
                        {
                            case If s:
                                if (currentBlock.EdgesOut.Any(e => e.IsBack))
                                    continue;
                                await writer.WriteAsync("IF ");
                                await s.Expression.Dump(writer);
                                await writer.WriteLineAsync(" THEN");
                                var exit = s.TrueBlock.CommonImmediatePostDominator(s.FalseBlock);
                                await new CodeBlock("IF", s.TrueBlock, exit).Dump(writer);
                                if (exit != s.FalseBlock)
                                {
                                    await writer.WriteLineAsync("ELSE");
                                    await new CodeBlock("ELSE", s.FalseBlock, exit).Dump(writer);
                                }

                                next = exit;
                                break;
                            case Case s:
                                await writer.WriteAsync("CASE ");
                                await s.Expression.Dump(writer);
                                await writer.WriteLineAsync(" OF");
                                writer.Indent++;
                                foreach (var (caseBlock, caseIndexes) in s.Cases)
                                {
                                    var indexes = string.Join(", ", caseIndexes);
                                    await writer.WriteLineAsync(indexes + " :");
                                    CodeBlock caseCodeBlock = new(indexes, caseBlock, s.Default);
                                    await caseCodeBlock.Dump(writer);
                                }
                                writer.Indent--;
                                await writer.WriteLineAsync("END {CASE}");
                                next = s.Default;
                                break;
                            default:
                                await statement.DumpLine(writer);
                                break;
                        }
                    }
                    currentBlock = next ?? currentBlock.EdgesOut.FirstOrDefault(e => !e.IsBack)?.Destination;

                }
                while (currentBlock != null && currentBlock != this.ExitBlock);

                writer.Indent--;
                if (emitBeginEnd)
                    writer.WriteLine("END {" + this.Label + "}");
            }
            // if unk, is loop? has multi? next block?
            // rep x
            // while
            // case x
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

            public override string ToString() => $"P{this.Parent} A{this.Ancestor} C{this.Child} V{this.Vertex} L{this.Label} Semi{this.Semi} Size{this.Size} Pred[{string.Join(',', this.Pred)}] Bucket[{string.Join(',', this.Bucket)}]";
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
                        statements.Add(Expression.If(this.OpIndexToBlock(index), nextBlock,
                            jumpToBlock,
                            vmStack.Pop()));
                        break;
                    }
                case OpCodeValue.EFJ:
                    {
                        var jump = (OpCode.Jump)opCode;
                        var jumpToBlock = this.AddressToBlock(jump.Address);
                        var nextBlock = this.OpIndexToBlock(index + 1);
                        statements.Add(Expression.If(this.OpIndexToBlock(index), nextBlock,
                            jumpToBlock,
                            Expression.Compare(0, 0, vmStack.Pop(), vmStack.Pop())));
                        break;
                    }
                case OpCodeValue.NFJ:
                    {
                        var jump = (OpCode.Jump)opCode;
                        var jumpToBlock = this.AddressToBlock(jump.Address);
                        var nextBlock = this.OpIndexToBlock(index + 1);
                        statements.Add(Expression.If(this.OpIndexToBlock(index), nextBlock,
                            jumpToBlock,
                            Expression.Compare(8, 0, vmStack.Pop(), vmStack.Pop())));
                        break;
                    }
                case OpCodeValue.UJP:
                    break;
                case OpCodeValue.XJP:
                    var xjp = (OpCode.JumpTable)opCode;
                    statements.Add(new Case(xjp.Minimum, this.AddressToBlock(xjp.DefaultAddress),
                        (from a in xjp.Addresses select this.AddressToBlock(a)).ToArray(), vmStack.Pop()));
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

        private void Jump(OpCode.Jump jump, int index, List<Expression> statements, Expression test)
        {
            var currentBlock = this.OpIndexToBlock(index);
            var jumpToBlock = this.AddressToBlock(jump.Address);
            var nextBlock = this.OpIndexToBlock(index + 1);
            //if (startBlock.Dominates.Count > 1)
            statements.Add(Expression.If(currentBlock, nextBlock,
                jumpToBlock,
                test));
            //else
            //{
            //    List<Expression> loopStatements = new(statements);
            //    statements.Clear();
            //    throw new NotImplementedException();
            //    //statements.Add(Expression.Repeat(loopStatements, test, nextBlock));
            //}
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
            {
                CodeBlock codeBlock = new(this.Signature.Name, this.BlockList[0], CodeBlockType.Fixed);
                await codeBlock.Dump(writer);
            }

            //if (this.BlockList.Count > 0)
            //    await this.BlockList[0].Dump(writer);
            //else
            //    await writer.WriteAsync(";");
            await writer.WriteLineAsync();
        }
    }
}