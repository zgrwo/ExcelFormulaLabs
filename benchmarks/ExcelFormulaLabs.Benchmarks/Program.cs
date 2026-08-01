using BenchmarkDotNet.Running;

namespace ExcelFormulaLabs.Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Run all benchmarks: dotnet run -c Release
            // Run specific:     dotnet run -c Release --filter *MapOver*
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
