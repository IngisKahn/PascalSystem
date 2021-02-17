namespace PascalSystem.Decompilation
{
    using System;
    using System.Collections.Generic;
    using System.Collections;
    using Model;

    public class BasicBlock
    {
        private readonly MethodAnalyzer methodAnalyzer;
        private readonly int id;
        private readonly int startIndex;
        private readonly int endIndex;

        public BasicBlock(MethodAnalyzer methodAnalyzer, int id, int startIndex, int endIndex)
        {
            this.methodAnalyzer = methodAnalyzer;
            this.id = id;
            this.startIndex = startIndex;
            this.endIndex = endIndex;
        }
    }

    public class MethodAnalyzer : IEnumerable<BasicBlock>
    {
        private readonly Model.Method method;
        private readonly Decompiler decompiler;
        private bool decompiled;
        private Dictionary<int, int> opAddressToIndex = new();

        public MethodAnalyzer(Decompiler decompiler, Model.Method method)
        {
            this.decompiler = decompiler;
            this.method = method;
        }

        public IEnumerator<BasicBlock> GetEnumerator() => throw new NotImplementedException();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        private readonly List<BasicBlock> blockList = new();

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
            switch (opCode.Id)
            {
                case OpcodeValue.NOP:
                    break;
                default:
                    //throw new DecompilationException("Invalid Op Code: " + opCode.Id);
                    return;
            }
        }
    }
}
