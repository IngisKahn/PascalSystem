namespace PascalSystem.Decompilation
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Model;
    using Types;

    public class Decompiler
    {
        private readonly  (Unit? Unit, MethodAnalyzer[]? MethodAnalyzers)[] unitMethods;
        
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
            foreach (var unit in source)
                this.unitMethods[unit.Number - 1] = (unit, unit.Methods.Values.OrderBy(m => m.Id)
                                                                          .Select(m => new MethodAnalyzer(this, m))
                                                                          .ToArray());
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

                var isProgram = unit.Number == 1;

                await writer.WriteLineAsync((isProgram ? "PROGRAM " : "UNIT ") + unit.Name + ';');
                await writer.WriteLineAsync();
            }
        }
    }

    public class MethodSignature
    {
        public Interval Parameters { get; }
        public Base ReturnType { get; }
        public string Name { get; }
        public MethodSignature(Model.Method method)
        {
            this.ReturnType = method.ReturnLength == 0 ? Types.Void.Instance : new SizeRange((BitCount)1, (BitCount)16).Proxy();

            this.Parameters = new(method.ParameterLength);

            this.Name = method.Unit.Name + "." + method.Name;
        }

        public MethodSignature(string name, Types.Base[] parameters)
        {
            this.Name = name;

            if (parameters.Length == 0)
                throw new ArgumentException("Method must have at least 1 return parameter");

            this.ReturnType = parameters[0];

            this.Parameters = new((WordCount)(parameters.Length - 1));
            for (var i = 1; i < parameters.Length; i++)
                this.Parameters.MeetAt((WordCount)(i - 1), parameters[i]);
        }
    }
}