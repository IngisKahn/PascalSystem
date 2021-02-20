namespace PascalSystem.Decompilation
{
    using System;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Model;
    using Types;

    public class Decompiler
    {
        private readonly  (Unit? Unit, MethodAnalyzer[]? MethodAnalyzers)[] unitMethods;
        public Interval[] Globals { get; }

        public MethodAnalyzer GetMethod(int unitId, int methodId)
        {
            if (unitId < 0 || unitId >= this.unitMethods.Length)
                throw new ArgumentOutOfRangeException(nameof(unitId));
            var (unit, methodAnalyzers) = this.unitMethods[unitId];
            if (unit == null || methodAnalyzers == null)
                throw new InvalidUnitException();
            if (methodId < 0 || methodId >= methodAnalyzers.Length)
                throw new ArgumentOutOfRangeException(nameof(methodId));
            return methodAnalyzers[methodId];
        }

        public Decompiler(IEnumerable<Model.Unit> units)
        {
            var source = units.OrderBy(u => u.Number).ToArray();
            if (source.Length == 0)
                throw new ArgumentException("No units present", nameof(units));
            var maxId = source.Last().Number;

            this.unitMethods = new (Unit? Unit, MethodAnalyzer[]? MethodAnalyzers)[maxId];
            this.Globals = new Interval[maxId];
            foreach (var unit in source)
            {
                this.unitMethods[unit.Number - 1] = (unit, unit.Methods.Values.OrderBy(m => m.Id)
                    .Select(m => new MethodAnalyzer(this, m))
                    .ToArray());
                this.Globals[unit.Number - 1] = new((ByteCount)0);
            }
        }

        public void ProcessUnits()
        {
            this.unitMethods[0].MethodAnalyzers?[0].Decompile();
        }

        public async Task Dump(string path)
        {
            foreach (var (unit, methodAnalyzers) in this.unitMethods)
            {
                if (unit == null || methodAnalyzers == null)
                    continue;
                await using FileStream stream = new(Path.Join(path, unit.Name + ".pas"), FileMode.Create);
                await using StreamWriter writer = new(stream);
                IndentedTextWriter w = new(writer);
                var isProgram = unit.Number == 1;
                await w.WriteLineAsync((isProgram ? "PROGRAM " : "UNIT ") + unit.Name + ';');
                // write vars
                // write lvl 1s
                foreach (var methodAnalyzer in methodAnalyzers.Where(m => m.Level == 1))
                    await methodAnalyzer.Dump(w);
                // write lvl 0
                if (!isProgram)
                    continue;
                await methodAnalyzers[0].DumpCode(w);
            }
        }
    }
}