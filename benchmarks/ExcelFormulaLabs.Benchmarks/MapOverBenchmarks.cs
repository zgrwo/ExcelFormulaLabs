using BenchmarkDotNet.Attributes;
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.Benchmarks
{
    /// <summary>
    /// Benchmarks for ElementWiseMapper (MapOver) — the hottest path in the UDF layer.
    /// Every array UDF call goes through MapOver, so regressions here affect all 220+ functions.
    /// </summary>
    [MemoryDiagnoser]
    [ShortRunJob]
    public class MapOverBenchmarks
    {
        private object[] _smallArray = null!;   // 10 elements
        private object[] _mediumArray = null!;  // 1000 elements
        private object[,] _2dArray = null!;     // 50x20 = 1000 elements

        [GlobalSetup]
        public void Setup()
        {
            _smallArray = Enumerable.Range(1, 10).Select(i => (object)(double)i).ToArray();
            _mediumArray = Enumerable.Range(1, 1000).Select(i => (object)(double)i).ToArray();
            _2dArray = new object[50, 20];
            for (int r = 0; r < 50; r++)
                for (int c = 0; c < 20; c++)
                    _2dArray[r, c] = (double)(r * 20 + c);
        }

        [Benchmark(Baseline = true)]
        public object MapOver_10_Sqrt()
            => ElementWiseMapper.MapOver<double, double>(_smallArray, Math.Sqrt);

        [Benchmark]
        public object MapOver_1000_Sqrt()
            => ElementWiseMapper.MapOver<double, double>(_mediumArray, Math.Sqrt);

        [Benchmark]
        public object MapOver_2D_1000_Sqrt()
            => ElementWiseMapper.MapOver<double, double>(_2dArray, Math.Sqrt);

        [Benchmark]
        public object MapOver_Scalar()
            => ElementWiseMapper.MapOver<double, double>(42.0, Math.Sqrt);
    }
}
