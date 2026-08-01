using BenchmarkDotNet.Attributes;
using ExcelFormulaLabs.Analytics;

namespace ExcelFormulaLabs.Benchmarks
{
    /// <summary>
    /// Benchmarks for LinalgCore — covers heavy matrix decompositions used by LINALG.* UDFs.
    /// These are the primary candidates for async UDF offloading.
    /// </summary>
    [MemoryDiagnoser]
    [ShortRunJob]
    public class LinalgBenchmarks
    {
        private double[,] _50x50 = null!;
        private double[,] _100x100 = null!;
        private double[,] _symmetric50 = null!;
        private double[] _rhs50 = null!;

        [GlobalSetup]
        public void Setup()
        {
            var rng = new Random(42);
            _50x50 = new double[50, 50];
            _100x100 = new double[100, 100];
            _symmetric50 = new double[50, 50];
            _rhs50 = new double[50];

            for (int i = 0; i < 50; i++)
            {
                _rhs50[i] = rng.NextDouble();
                for (int j = 0; j < 50; j++)
                {
                    _50x50[i, j] = rng.NextDouble();
                    _symmetric50[i, j] = rng.NextDouble();
                }
            }
            // Make symmetric
            for (int i = 0; i < 50; i++)
                for (int j = i + 1; j < 50; j++)
                    _symmetric50[j, i] = _symmetric50[i, j];

            for (int i = 0; i < 100; i++)
                for (int j = 0; j < 100; j++)
                    _100x100[i, j] = rng.NextDouble();
        }

        [Benchmark(Baseline = true)]
        public (double[,] U, double[] S, double[,] Vt) Svd_50x50()
            => LinalgCore.Svd(_50x50);

        [Benchmark]
        public (double[,] Q, double[,] R) Qr_50x50()
            => LinalgCore.Qr(_50x50);

        [Benchmark]
        public double[] Solve_50()
            => LinalgCore.Solve(_50x50, _rhs50);

        [Benchmark]
        public double[] Eigenvalues_50()
            => LinalgCore.Eigenvalues(_symmetric50);

        [Benchmark]
        public double[,] Cholesky_50()
            => LinalgCore.Cholesky(MakePositiveDefinite(_symmetric50));

        [Benchmark]
        public double Determinant_50()
            => LinalgCore.Determinant(_50x50);

        [Benchmark]
        public (double[,] U, double[] S, double[,] Vt) Svd_100x100()
            => LinalgCore.Svd(_100x100);

        /// <summary>Ensure positive-definite by computing A*A^T + n*I.</summary>
        private static double[,] MakePositiveDefinite(double[,] a)
        {
            int n = a.GetLength(0);
            var result = new double[n, n];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < n; k++)
                        sum += a[i, k] * a[j, k];
                    result[i, j] = sum + (i == j ? n : 0);
                }
            return result;
        }
    }
}
