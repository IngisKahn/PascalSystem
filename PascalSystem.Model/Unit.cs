namespace PascalSystem.Model
{
    using System;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.IO;

    public class Unit
    {
        private readonly int position;

        public Unit(string name, int number, ComFile container, int position)
        {
            this.Name = name;
            this.Number = number;
            this.Container = container;
            this.position = position;
        }

        public string Name { get; }
        public int Number { get; }
        public ComFile Container { get; }

        public Dictionary<int, Method> Methods { get; } = new();

        public void Dump(string outputFolder)
        {
            using IndentedTextWriter writer = new(new StreamWriter(Path.Combine(outputFolder, this.Name + ".seg")),
                " ");
            writer.WriteLine("segment {0}:", this.Name);
            writer.Indent++;
            foreach (var method in this.Methods.Values)
                method.Dump(writer);
        }

        internal void Initialize(byte[] systemData)
        {
            var p = this.position;
            var procCount = systemData[p - 1];
            p -= 2;
            Method lastMethod = null;
            for (var procId = 1; procId <= procCount; procId++)
            {
                p -= 2;
                var jTab = p - BitConverter.ToUInt16(systemData, p);
                Method method = new(systemData, jTab, procId, this, lastMethod);
                lastMethod = method;
                this.Methods.Add(method.Id, method);
            }
        }
    }

    public class Method
    {
        private byte[] systemData;
        private int jTab;
        private int procId;
        private Unit unit;
        private Method lastMethod;

        public Method(byte[] systemData, int jTab, int procId, Unit unit, Method lastMethod)
        {
            this.systemData = systemData;
            this.jTab = jTab;
            this.procId = procId;
            this.unit = unit;
            this.lastMethod = lastMethod;
        }

        public int Id { get; set; }

        public void Dump(IndentedTextWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}