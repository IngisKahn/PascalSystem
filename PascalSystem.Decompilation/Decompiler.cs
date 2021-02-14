namespace PascalSystem.Decompilation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Model;

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
                                                                          .Select(m => new MethodAnalyzer(m))
                                                                          .ToArray());
        }

        public void ProcessUnits()
        {

        }
    }
}