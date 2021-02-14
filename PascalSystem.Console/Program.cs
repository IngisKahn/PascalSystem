namespace PascalSystem.Console
{
    using Decompilation;
    using Model;

    static class Program
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine("Hello World!");
            ComFile com = new("WIZ1.DSK");
            Decompiler decompiler = new(com.Units);
        }
    }
}
