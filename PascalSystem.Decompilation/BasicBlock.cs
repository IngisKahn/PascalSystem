namespace PascalSystem.Decompilation
{
    using System;
    using System.Collections.Generic;
    using System.Collections;

    public class BasicBlock
    {
    }

    public class MethodAnalyzer : IEnumerable<BasicBlock>
    {
        private readonly Model.Method method;

        public MethodAnalyzer(Model.Method method) => this.method = method;

        public IEnumerator<BasicBlock> GetEnumerator() => throw new NotImplementedException();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }

}
