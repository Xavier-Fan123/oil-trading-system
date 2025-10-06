using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace OilTrading.Benchmarks;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("🚀 Oil Trading System Performance Benchmarks");
        Console.WriteLine(new string('=', 50));
        
        if (args.Length == 0)
        {
            Console.WriteLine();
            Console.WriteLine("Available benchmark suites:");
            Console.WriteLine("  1. database    - Database operations benchmarks");
            Console.WriteLine("  2. valueobject - Value object performance benchmarks");
            Console.WriteLine("  3. all         - Run all benchmarks");
            Console.WriteLine();
            Console.WriteLine("Usage: dotnet run -- <suite>");
            Console.WriteLine("Example: dotnet run -- database");
            return;
        }

        var suite = args[0].ToLowerInvariant();
        var config = DefaultConfig.Instance;

        switch (suite)
        {
            case "database":
                Console.WriteLine("Running database benchmarks...");
                BenchmarkRunner.Run<DatabaseBenchmarks>(config);
                break;
                
            case "valueobject":
                Console.WriteLine("Running value object benchmarks...");
                BenchmarkRunner.Run<ValueObjectBenchmarks>(config);
                break;
                
            case "all":
                Console.WriteLine("Running all benchmarks...");
                BenchmarkRunner.Run<DatabaseBenchmarks>(config);
                BenchmarkRunner.Run<ValueObjectBenchmarks>(config);
                break;
                
            default:
                Console.WriteLine($"Unknown benchmark suite: {suite}");
                Console.WriteLine("Available suites: database, valueobject, all");
                Environment.Exit(1);
                break;
        }

        Console.WriteLine();
        Console.WriteLine("✅ Benchmarks completed!");
        Console.WriteLine("📊 Results saved to BenchmarkDotNet.Artifacts folder");
    }
}
