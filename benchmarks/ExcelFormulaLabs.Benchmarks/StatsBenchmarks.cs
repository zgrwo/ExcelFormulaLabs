using BenchmarkDotNet.Attributes;
using ExcelFormulaLabs.Analytics;

namespace ExcelFormulaLabs.Benchmarks
{
    /// <summary>
    /// Benchmarks for StatsCore — covers hot paths used by STATS.* UDFs.
    /// </summary>
    [MemoryDiagnoser]
    [ShortRunJob]
    public class StatsBenchmarks
    {
        private double[] _small = null!;   // 100 elements
        private double[] _medium = null!;  // 10,000 elements
        private double[,] _corrMatrix = null!; // 50x50

        [GlobalSetup]
        public void Setup()
        {
            var rng = new Random(42);
            _small = Enumerable.Range(0, 100).Select(_ => rng.NextDouble() * 1000).ToArray();
            _medium = Enumerable.Range(0, 10_000).Select(_ => rng.NextDouble() * 1000).ToArray();
            _corrMatrix = new double[50, 50];
            for (int i = 0; i < 50; i++)
                for (int j = 0; j < 50; j++)
                    _corrMatrix[i, j] = rng.NextDouble();
        }

        [Benchmark(Baseline = true)]
        public double Mean_100() => StatsCore.Mean(_small);

        [Benchmark]
        public double Mean_10000() => StatsCore.Mean(_medium);

        [Benchmark]
        public double Stdev_10000() => StatsCore.Stdev(_medium);

        [Benchmark]
        public double[] ZScore_10000() => StatsCore.ZScore(_medium);

        [Benchmark]
        public double Percentile_10000() => StatsCore.Percentile(_medium, 95.0);

        [Benchmark]
        public double[,] CorrelationMatrix_50x50() => StatsCore.CorrelationMatrix(_corrMatrix);
    }
}
