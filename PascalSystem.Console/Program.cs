namespace PascalSystem.Console
{
    using System.CodeDom.Compiler;
    using System.Data;
    using System.IO;
    using System.Threading.Tasks;
    using Decompilation;
    using Model;

    static class Program
    {
        static async Task Main(string[] args)
        {
            ComFile com = new("WIZ1.DSK");
            Decompiler decompiler = new(com.Units);
            decompiler.ProcessUnits();
            if (!Directory.Exists("src"))
                Directory.CreateDirectory("src");
            await decompiler.Dump("src");
        }
    }
}
