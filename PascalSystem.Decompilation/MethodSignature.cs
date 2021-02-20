namespace PascalSystem.Decompilation
{
    using System;
    using System.CodeDom.Compiler;
    using System.Threading.Tasks;
    using Model;
    using Types;

    public class MethodSignature
    {
        public Interval Parameters { get; }
        public Base ReturnType { get; }
        public string Name { get; }
        public string FullName { get; }
        public bool IsFunction => !this.ReturnType.ResolvesTo<Types.Void>();

        public MethodSignature(Model.Method method)
        {
            this.ReturnType = method.ReturnLength == 0 ? Types.Void.Instance : new SizeRange((BitCount)1, (BitCount)16).Proxy();

            this.Parameters = new(method.ParameterLength);

            this.Name = method.Name;
            this.FullName = method.Unit.Name + "." + method.Name;
        }

        public MethodSignature(string name, Types.Base[] parameters)
        {
            this.Name = name;
            this.FullName = "PASCALSYSTEM." + name;

            if (parameters.Length == 0)
                throw new ArgumentException("Method must have at least 1 return parameter");

            this.ReturnType = parameters[0];

            this.Parameters = new((WordCount)(parameters.Length - 1));
            for (var i = 1; i < parameters.Length; i++)
                this.Parameters.MeetAt((WordCount)(i - 1), parameters[i]);
        }

        public async Task Dump(IndentedTextWriter writer)
        {
            await writer.WriteAsync(this.IsFunction ? "FUNCTION " : "PROCEDURE ");
            await writer.WriteAsync(this.Name);

            await writer.WriteAsync('(');
            for (var i = 1; i < this.Parameters.Count; i++)
            {
                if (i != 1)
                    await writer.WriteAsync(", ");
                var param = this.Parameters.GetTypeAtOffset((WordCount)i);
                await writer.WriteAsync("param" + i + " : " + param);
            }

            await writer.WriteLineAsync(this.IsFunction ? ") : " + this.ReturnType : ")");
        }
    }
}