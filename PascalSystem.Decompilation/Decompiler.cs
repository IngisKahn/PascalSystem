﻿namespace PascalSystem.Decompilation
{
    using System;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Expressions;
    using Model;
    using Types;
    using Pointer = Types.Pointer;
    using Void = Types.Void;

    public class Decompiler
    {
        private readonly  (Unit? Unit, MethodAnalyzer[]? MethodAnalyzers)[] unitMethods;
        public Interval[] Globals { get; }

        public MethodAnalyzer GetMethod(int unitId, int methodId)
        {
            unitId--;
            methodId--;
            if (unitId < 0 || unitId >= this.unitMethods.Length)
                throw new ArgumentOutOfRangeException(nameof(unitId));
            var (unit, methodAnalyzers) = this.unitMethods[unitId];
            if (unit == null || methodAnalyzers == null)
                throw new InvalidUnitException();
            if (methodId < 0 || methodId >= methodAnalyzers.Length)
                throw new ArgumentOutOfRangeException(nameof(methodId));
            return methodAnalyzers[methodId];
        }


        public static readonly MethodSignature MoveLeft;
        public static readonly MethodSignature UnitRead;
        public static readonly MethodSignature UnitWrite;
        public static readonly MethodSignature FillChar;
        public static readonly MethodSignature Mark;
        public static readonly MethodSignature Release;
        public static readonly MethodSignature Ioresult;
        public static readonly MethodSignature Uclear;
        public static readonly MethodSignature Memavail;

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

        static Decompiler()
        {

            Decompiler.UnitRead = new("UNITREAD", Void.Instance, new Integer(), new Pointer(new Integer()), new Integer(),
                new Integer(), new Integer(), new Integer());
            Decompiler.UnitWrite = new("UNITWRITE", Void.Instance, new Integer(), new Pointer(new Integer()), new Integer(),
                new Integer(), new Integer(), new Integer());
            Decompiler.MoveLeft = new("MOVELEFT",  Void.Instance, new Pointer(), new Pointer(), new Integer());
            Decompiler.FillChar = new("FILLCHAR", Void.Instance, new Pointer(), new Integer(), new Integer(), new Character());
            Decompiler.Mark = new("MARK", Void.Instance, new Pointer());
            Decompiler.Release = new("RELEASE", Void.Instance, new Pointer());
            Decompiler.Ioresult = new("IORESULT", new Integer());
            Decompiler.Uclear = new("UCLEAR", Void.Instance, new Integer());
            Decompiler.Memavail = new("MEMAVAIL", new Integer());
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
                var globals = this.Globals[unit.Number - 1];
                if (globals?.Count > 0)
                {
                    await w.WriteLineAsync("VAR");
                    w.Indent++;
                    foreach (var global in globals)
                    {
                        GlobalVariable expression = new(global.Offset, global.Type);
                        await w.WriteLineAsync(expression + ";");
                    }
                    w.Indent--;
                    await w.WriteLineAsync();
                }
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