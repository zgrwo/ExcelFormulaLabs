using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.Analytics
{
    /// <summary>Column role in a SOLVE data table, decided by the header prefix.</summary>
    internal enum SolveRole { Incoming, Variable, Fixed, Output }

    /// <summary>
    /// Parsed SOLVE table layout: column roles (by header prefix) and row classification.
    /// HistoryRows / RequestRows contain 0-based data-row indices excluding the header row.
    /// </summary>
    internal sealed class SolveSchema
    {
        public string[] Headers = Array.Empty<string>();
        public int[] Incoming = Array.Empty<int>();
        public int[] Variable = Array.Empty<int>();
        public int[] Fixed = Array.Empty<int>();
        public int[] Output = Array.Empty<int>();
        /// <summary>Feature columns in table order: incoming + variable + fixed.</summary>
        public int[] Features = Array.Empty<int>();
        public List<int> HistoryRows = new List<int>();
        public List<int> RequestRows = new List<int>();
        public bool HasHeaders = true;
    }

    /// <summary>
    /// Forward model: y = Intercept + Σ Coef[t]·Π x_j^Powers[t][j], in original data units.
    /// Rate models (ADR-0008) instead represent the removal rate g(x) and predict
    /// Output = Incoming − time·g, where the paired incoming and time columns are excluded
    /// from the fitted terms (their Coef entries stay 0).
    /// </summary>
    internal sealed class SolveModel
    {
        public string Kind = SolveCore.ModelLinear;
        public string[] BaseNames = Array.Empty<string>();
        public int[][] Powers = Array.Empty<int[]>();
        public double[] Coef = Array.Empty<double>();
        public double Intercept;
        /// <summary>Feature position of the paired Incoming column (rate kind only; -1 otherwise).</summary>
        public int RateIncomingIndex = -1;
        /// <summary>Feature position of the time column (rate kind only; -1 otherwise).</summary>
        public int RateTimeIndex = -1;
    }

    /// <summary>
    /// SOLVE.* core: table parsing, forward models (linear / poly 2nd-order), cross-validation
    /// gating, bounded multi-start pattern search inversion and reachability sampling.
    /// Pure logic — zero Excel dependency (see ADR-0007).
    /// </summary>
    internal static class SolveCore
    {
        internal const int MaxHistoryRows = 5000;
        internal const int MaxRequestRows = 200;
        internal const int MaxFeatureColumns = 50;
        internal const int MaxVariables = 20;
        internal const int MaxOutputs = 20;
        internal const int PolyTermLimit = 100;
        internal const int MaxStartsLimit = 50;
        internal const int MinStarts = 1;
        internal const int MaxEvaluationsPerStart = 4000;
        internal const int ReachabilitySamples = 2000;
        internal const double PolyRidgeLambda = 1e-5;
        internal const string ModelAuto = "auto";
        internal const string ModelLinear = "linear";
        internal const string ModelPoly = "poly";
        internal const string ModelRate = "rate";
        internal const string SchemeFiveFold = "5折";
        internal const string SchemeLoo = "LOO";
        internal const string SchemeSkipped = "跳过";
        internal const string StatusReachable = "可达";
        internal const string StatusUnreachable = "不可达";
        internal const string TypeForward = "前向方程";
        internal const string TypeRate = "速率方程";
        internal const string TypeInverse = "反解公式";
        internal const string RateColumnSuffix = "速率";

        // 目标优先权重（ADR-0007 决策 4）：目标偏差主导，proximity 仅在等价解集内选点。
        // 计划原始权重为 1（F_target + 0.02·proximity），在精确夹具上会把推荐值拉偏 ~2%·s，
        // 与验收锚点（推荐 4.0±1e-4 / 预测 13.0±1e-6）矛盾；字典序化后两者同时成立。
        private const double TargetPriority = 1e6;
        private const double ProximityWeight = 0.02;
        // 优化器步长下限：1e-9·边界跨度（相对量纲，禁用绝对 ε）。
        private const double StepFloorFraction = 1e-9;
        // 可达判定与"已达成"判定容差（相对量纲）。
        private const double ReachToleranceFraction = 1e-9;
        private const double AchievedSigmaTolerance = 1e-6;
        private const double R2TieTolerance = 1e-9;

        // ──────────────────────────── 表解析 ────────────────────────────

        /// <summary>null / DBNull / ExcelMissing / ExcelEmpty / 空白串 均视为空单元格。</summary>
        internal static bool IsBlank(object? cell)
        {
            if (cell == null || cell is DBNull) return true;
            if (InputNormalizer.IsExcelEmptyValue(cell) || InputNormalizer.IsExcelMissing(cell)) return true;
            if (cell is string s) return string.IsNullOrWhiteSpace(s);
            return false;
        }

        private static SolveRole? ClassifyHeader(string header)
        {
            if (header.StartsWith("Incoming", StringComparison.OrdinalIgnoreCase) || header.StartsWith("来料", StringComparison.Ordinal))
                return SolveRole.Incoming;
            if (header.StartsWith("Variable", StringComparison.OrdinalIgnoreCase) || header.StartsWith("可调", StringComparison.Ordinal)
                || header.StartsWith("变量", StringComparison.Ordinal))
                return SolveRole.Variable;
            if (header.StartsWith("Fixed", StringComparison.OrdinalIgnoreCase) || header.StartsWith("固定", StringComparison.Ordinal))
                return SolveRole.Fixed;
            if (header.StartsWith("Output", StringComparison.OrdinalIgnoreCase) || header.StartsWith("输出", StringComparison.Ordinal))
                return SolveRole.Output;
            return null;
        }

        /// <summary>
        /// Parse a table: classify columns by header prefix and rows into history / request.
        /// History row = every adjustable (Variable) cell numeric; otherwise request row.
        /// Request rows must carry numeric incoming conditions (they are known inputs, not unknowns).
        /// Fully blank rows are ignored.
        /// </summary>
        internal static SolveSchema ParseSchema(object[,] data, bool hasHeaders = true)
        {
            if (data == null) throw new ArgumentException("Data table is null.");
            if (!hasHeaders)
                throw new ArgumentException("SOLVE requires a header row to identify column roles (data must include headers).");
            int rows = data.GetLength(0), cols = data.GetLength(1);
            if (rows < 1 || cols < 1)
                throw new ArgumentException("Data table is empty.");
            if (rows < 2)
                throw new ArgumentException("Data table must contain a header row and at least one data row.");

            var schema = new SolveSchema { HasHeaders = true, Headers = new string[cols] };
            var incoming = new List<int>();
            var variable = new List<int>();
            var fixedCols = new List<int>();
            var output = new List<int>();
            for (int c = 0; c < cols; c++)
            {
                string header = InputNormalizer.ToString(data[0, c]).Trim();
                schema.Headers[c] = header;
                switch (ClassifyHeader(header))
                {
                    case SolveRole.Incoming: incoming.Add(c); break;
                    case SolveRole.Variable: variable.Add(c); break;
                    case SolveRole.Fixed: fixedCols.Add(c); break;
                    case SolveRole.Output: output.Add(c); break;
                }
            }
            if (variable.Count == 0)
                throw new ArgumentException(
                    "No Variable column found. Name adjustable columns with a 'Variable*' (or '可调*'/'变量*') prefix.");
            if (output.Count == 0)
                throw new ArgumentException(
                    "No Output column found. Name result columns with an 'Output*' (or '输出*') prefix.");
            schema.Incoming = incoming.ToArray();
            schema.Variable = variable.ToArray();
            schema.Fixed = fixedCols.ToArray();
            schema.Output = output.ToArray();
            schema.Features = incoming.Concat(variable).Concat(fixedCols).ToArray();

            for (int i = 1; i < rows; i++)
            {
                bool allBlank = true;
                for (int c = 0; c < cols && allBlank; c++)
                    if (!IsBlank(data[i, c])) allBlank = false;
                if (allBlank) continue;

                bool allVariableNumeric = true;
                bool anyVariablePresent = false;
                foreach (int vc in schema.Variable)
                {
                    if (IsBlank(data[i, vc])) { allVariableNumeric = false; continue; }
                    anyVariablePresent = true;
                    if (!InputNormalizer.IsNumericCell(data[i, vc])) allVariableNumeric = false;
                }
                int dataRow = i - 1;
                if (allVariableNumeric && anyVariablePresent)
                {
                    schema.HistoryRows.Add(dataRow);
                    continue;
                }
                // Request row: incoming conditions must be present and numeric.
                foreach (int ic in schema.Incoming)
                {
                    if (IsBlank(data[i, ic]))
                        throw new ArgumentException(
                            $"Incoming column '{schema.Headers[ic]}' is blank in request row {i + 1}. " +
                            "Incoming values are known conditions and must be provided for every request.");
                    if (!InputNormalizer.IsNumericCell(data[i, ic]))
                        throw new ArgumentException(
                            $"Incoming column '{schema.Headers[ic]}' contains a non-numeric value in request row {i + 1}.");
                }
                schema.RequestRows.Add(dataRow);
            }
            return schema;
        }

        // ──────────────────────────── 前向模型 ────────────────────────────

        private static string NormalizeModel(string? model)
        {
            if (string.IsNullOrWhiteSpace(model)) return ModelAuto;
            switch (model!.Trim().ToLowerInvariant())
            {
                case ModelAuto: return ModelAuto;
                case ModelLinear: return ModelLinear;
                case ModelPoly: return ModelPoly;
                case ModelRate: return ModelRate;
                default:
                    throw new ArgumentException(
                        $"Unknown model '{model}'. Use \"auto\", \"linear\", \"poly\" or \"rate\".");
            }
        }

        private static string RequireFitModel(string? model)
        {
            string m = NormalizeModel(model);
            if (m == ModelAuto)
                throw new ArgumentException("FitModel requires an explicit model: \"linear\", \"poly\" or \"rate\".");
            return m;
        }

        /// <summary>Number of expanded design terms: linear = k; rate = k (linear g, two columns constrained); poly = k + k(k+1)/2.</summary>
        internal static int ExpandedTermCount(int baseCount, string model)
        {
            if (baseCount < 0) throw new ArgumentException("baseCount must be non-negative.");
            string m = model?.Trim().ToLowerInvariant() ?? "";
            if (m == ModelLinear || m == ModelRate) return baseCount;
            if (m == ModelPoly) return baseCount + baseCount * (baseCount + 1) / 2;
            throw new ArgumentException($"Unknown model '{model}'. Use \"linear\", \"poly\" or \"rate\".");
        }

        private static int[][] BuildPowers(int baseCount, string model)
        {
            if (model == ModelLinear || model == ModelRate)
            {
                var linear = new int[baseCount][];
                for (int j = 0; j < baseCount; j++)
                {
                    var p = new int[baseCount];
                    p[j] = 1;
                    linear[j] = p;
                }
                return linear;
            }
            var powers = new List<int[]>(ExpandedTermCount(baseCount, model));
            for (int j = 0; j < baseCount; j++)
            {
                var p = new int[baseCount];
                p[j] = 1;
                powers.Add(p);
            }
            for (int j = 0; j < baseCount; j++)
            {
                var p = new int[baseCount];
                p[j] = 2;
                powers.Add(p);
            }
            for (int a = 0; a < baseCount; a++)
                for (int b = a + 1; b < baseCount; b++)
                {
                    var p = new int[baseCount];
                    p[a] = 1;
                    p[b] = 1;
                    powers.Add(p);
                }
            return powers.ToArray();
        }

        private static double PowerProduct(double[] x, int[] powers)
        {
            double v = 1.0;
            for (int j = 0; j < powers.Length; j++)
            {
                int e = powers[j];
                if (e == 1) v *= x[j];
                else if (e == 2) v *= x[j] * x[j];
                else if (e > 0)
                {
                    double p = 1.0;
                    for (int t = 0; t < e; t++) p *= x[j];
                    v *= p;
                }
            }
            return v;
        }

        private static void ValidateMatrix(double[][] X, double[] y, string paramName)
        {
            if (X == null || X.Length == 0) throw new ArgumentException($"{paramName}: X must contain at least one row.");
            if (y == null || y.Length != X.Length)
                throw new ArgumentException($"{paramName}: Y length ({y?.Length ?? 0}) must equal X row count ({X.Length}).");
            int k = X[0]?.Length ?? 0;
            if (k == 0) throw new ArgumentException($"{paramName}: X must contain at least one column.");
            for (int i = 0; i < X.Length; i++)
            {
                if (X[i] == null || X[i].Length != k)
                    throw new ArgumentException($"{paramName}: X row {i} length differs from {k}.");
                for (int j = 0; j < k; j++)
                    if (double.IsNaN(X[i][j]) || double.IsInfinity(X[i][j]))
                        throw new ArgumentException($"{paramName}: X contains a non-finite value at [{i},{j}]. History data must be finite.");
                if (double.IsNaN(y[i]) || double.IsInfinity(y[i]))
                    throw new ArgumentException($"{paramName}: Y contains a non-finite value at row {i}. History outputs must be finite.");
            }
        }

        /// <summary>
        /// Fit a forward model on history data. Terms are standardized (mean/sample-sd, sd=0 → dropped)
        /// before fitting — OLS (QR) for linear/rate, Ridge (augmented QR, λ=1e-5) for poly — then
        /// de-standardized back to original units. The rate model (ADR-0008) fits
        /// g = (IncomingZx − Output)/t with the paired incoming and time columns excluded.
        /// </summary>
        internal static SolveModel FitModel(double[][] X, double[] y, string model,
            int rateIncoming = -1, int rateTime = -1)
        {
            string m = RequireFitModel(model);
            ValidateMatrix(X, y, "FitModel");
            int n = X.Length, k = X[0].Length;
            if (m == ModelRate)
            {
                if (rateIncoming < 0 || rateIncoming >= k || rateTime < 0 || rateTime >= k || rateIncoming == rateTime)
                    throw new ArgumentException(
                        "Model 'rate' requires valid paired incoming and time feature positions.");
                if (n < k + 1)
                    throw new ArgumentException(
                        $"Not enough history rows for rate model: {n} rows for {k} feature columns (need at least {k + 1}).");
                var yRate = new double[n];
                for (int i = 0; i < n; i++)
                {
                    double tValue = X[i][rateTime];
                    if (double.IsNaN(tValue) || double.IsInfinity(tValue) || tValue <= 0)
                        throw new ArgumentException(
                            $"Model 'rate' requires a positive finite time at history row {i} (got {tValue}).");
                    yRate[i] = (X[i][rateIncoming] - y[i]) / tValue;
                    if (double.IsNaN(yRate[i]) || double.IsInfinity(yRate[i]))
                        throw new ArgumentException("Model 'rate': removal rate is numerically unstable.");
                }
                return FitExpanded(X, yRate, ModelRate, BuildPowers(k, ModelRate), ridged: false,
                    new[] { rateIncoming, rateTime }, rateIncoming, rateTime);
            }
            int terms = ExpandedTermCount(k, m);
            if (m == ModelPoly && terms > PolyTermLimit)
                throw new ArgumentException(
                    $"Poly expansion has {terms} terms, which exceeds the limit of {PolyTermLimit}. " +
                    "Reduce the number of feature columns or use model=\"linear\".");
            if (n < terms + 1)
                throw new ArgumentException(
                    $"Not enough history rows for {m} model: {n} rows for {terms} terms (need at least {terms + 1}).");
            return FitExpanded(X, y, m, BuildPowers(k, m), ridged: m == ModelPoly,
                Array.Empty<int>(), -1, -1);
        }

        private static SolveModel FitExpanded(double[][] X, double[] y, string kind, int[][] powers,
            bool ridged, int[] excluded, int rateIncoming, int rateTime)
        {
            int n = X.Length, k = X[0].Length, terms = powers.Length;
            var raw = new double[terms][];
            var mean = new double[terms];
            var scale = new double[terms];
            var active = new List<int>();
            for (int t = 0; t < terms; t++)
            {
                if (excluded.Contains(t)) continue; // rate：配对来料/时间列受约束，系数保持 0
                var col = new double[n];
                double sum = 0;
                for (int i = 0; i < n; i++) { col[i] = PowerProduct(X[i], powers[t]); sum += col[i]; }
                double mu = sum / n;
                double ss = 0;
                for (int i = 0; i < n; i++) { double d = col[i] - mu; ss += d * d; }
                double sd = n > 1 ? Math.Sqrt(ss / (n - 1)) : 0.0;
                if (double.IsNaN(sd) || double.IsInfinity(sd)) sd = 0.0;
                mean[t] = mu;
                scale[t] = sd > 0 ? sd : 1.0;
                raw[t] = col;
                if (sd > 0) active.Add(t);
            }

            var result = new SolveModel
            {
                Kind = kind,
                BaseNames = Enumerable.Range(1, k).Select(i => "x" + i).ToArray(),
                Powers = powers,
                RateIncomingIndex = rateIncoming,
                RateTimeIndex = rateTime,
            };
            if (active.Count == 0)
            {
                // 所有可拟合特征列都是常量 → 仅截距模型（y 为目标/速率均值）。
                double mu = 0;
                for (int i = 0; i < n; i++) mu += (y[i] - mu) / (i + 1);
                result.Intercept = mu;
                result.Coef = new double[terms];
                return result;
            }

            var Z = new double[n][];
            for (int i = 0; i < n; i++)
            {
                Z[i] = new double[active.Count];
                for (int a = 0; a < active.Count; a++)
                {
                    int t = active[a];
                    Z[i][a] = (raw[t][i] - mean[t]) / scale[t];
                }
            }
            NumericGuard.AgainstNonFinite(ToRect(Z), y);
            Dictionary<string, object> fit = ridged
                ? RegressionCore.FitRidge(ToRect(Z), y, PolyRidgeLambda, true)
                : RegressionCore.FitOLS(ToRect(Z), y, true);
            var beta = (double[])fit["coefficients"];

            var coef = new double[terms];
            double intercept = beta[0];
            for (int a = 0; a < active.Count; a++)
            {
                int t = active[a];
                coef[t] = beta[a + 1] / scale[t];
                intercept -= beta[a + 1] * mean[t] / scale[t];
            }
            result.Coef = coef;
            result.Intercept = intercept;
            return result;
        }

        private static double[,] ToRect(double[][] jagged)
        {
            int r = jagged.Length, c = r > 0 ? jagged[0].Length : 0;
            var m = new double[r, c];
            for (int i = 0; i < r; i++)
                for (int j = 0; j < c; j++) m[i, j] = jagged[i][j];
            return m;
        }

        /// <summary>Evaluate a fitted model in original units (rate: Output = Incoming − time·g).</summary>
        internal static double Predict(SolveModel model, double[] x)
        {
            if (model == null) throw new ArgumentException("Model is null.");
            if (x == null || x.Length != model.BaseNames.Length)
                throw new ArgumentException(
                    $"Prediction input has {x?.Length ?? 0} values but the model expects {model.BaseNames.Length}.");
            double g = EvaluateG(model, x);
            if (model.Kind == ModelRate)
                return x[model.RateIncomingIndex] - x[model.RateTimeIndex] * g;
            return g;
        }

        /// <summary>Evaluate the fitted expression g (rate models) / y (others) in original units.</summary>
        private static double EvaluateG(SolveModel model, double[] x)
        {
            double y = model.Intercept;
            for (int t = 0; t < model.Coef.Length; t++)
                if (model.Coef[t] != 0.0) y += model.Coef[t] * PowerProduct(x, model.Powers[t]);
            return y;
        }

        /// <summary>Removal rate at the given feature row (rate-kind models only).</summary>
        internal static double PredictRate(SolveModel model, double[] x)
        {
            if (model == null || model.Kind != ModelRate)
                throw new ArgumentException("PredictRate requires a rate-kind model.");
            return EvaluateG(model, x);
        }

        // ──────────────────────────── 交叉验证与 auto 门控 ────────────────────────────

        private static double SampleStdDev(double[] v)
        {
            int n = v.Length;
            if (n < 2) return 0.0;
            double mean = 0;
            for (int i = 0; i < n; i++) mean += (v[i] - mean) / (i + 1);
            double ss = 0;
            for (int i = 0; i < n; i++) { double d = v[i] - mean; ss += d * d; }
            if (double.IsNaN(ss) || double.IsInfinity(ss)) throw new ArgumentException("Output values are too large for double precision (std dev overflow).");
            return Math.Sqrt(ss / (n - 1));
        }

        private static double Median(double[] values)
        {
            var copy = (double[])values.Clone();
            Array.Sort(copy);
            int n = copy.Length;
            if (n == 0) return double.NaN;
            return n % 2 == 1 ? copy[n / 2] : 0.5 * (copy[n / 2 - 1] + copy[n / 2]);
        }

        private static (double[][] X, double[] y) Subset(double[][] X, double[] y, List<int> idx)
        {
            var xs = new double[idx.Count][];
            var ys = new double[idx.Count];
            for (int i = 0; i < idx.Count; i++) { xs[i] = X[idx[i]]; ys[i] = y[idx[i]]; }
            return (xs, ys);
        }

        /// <summary>
        /// Out-of-fold cross-validation: deterministic shuffle (XorShift64), "5折" when n ≥ 20,
        /// otherwise "LOO" (requires n ≥ 5). R²/MAE are computed on out-of-fold predictions.
        /// </summary>
        internal static (string Scheme, double R2, double Mae) CrossValidate(double[][] X, double[] y, string model,
            long seed, int rateIncoming = -1, int rateTime = -1)
        {
            string m = RequireFitModel(model);
            ValidateMatrix(X, y, "CrossValidate");
            int n = X.Length;
            if (n < 5)
                throw new ArgumentException($"Cross-validation needs at least 5 history rows (got {n}).");

            var order = new int[n];
            for (int i = 0; i < n; i++) order[i] = i;
            var rng = new XorShift64((ulong)seed);
            for (int i = n - 1; i > 0; i--)
            {
                int j = (int)rng.NextLong(i + 1);
                int tmp = order[i]; order[i] = order[j]; order[j] = tmp;
            }
            bool fiveFold = n >= 20;
            int folds = fiveFold ? 5 : n;
            var pred = new double[n];
            for (int f = 0; f < folds; f++)
            {
                var train = new List<int>(n);
                var test = new List<int>();
                for (int i = 0; i < n; i++)
                {
                    bool inFold = fiveFold ? i % 5 == f : i == f;
                    var bucket = inFold ? test : train;
                    bucket.Add(order[i]);
                }
                var (xt, yt) = Subset(X, y, train);
                var (xv, _) = Subset(X, y, test);
                var modelF = FitModel(xt, yt, m, rateIncoming, rateTime);
                for (int t = 0; t < test.Count; t++)
                {
                    pred[test[t]] = Predict(modelF, xv[t]);
                    if (double.IsNaN(pred[test[t]]) || double.IsInfinity(pred[test[t]]))
                        throw new ArgumentException("Cross-validation produced a non-finite prediction; model is numerically unstable.");
                }
            }
            double yMean = 0;
            for (int i = 0; i < n; i++) yMean += (y[i] - yMean) / (i + 1);
            double sse = 0, tss = 0, mae = 0;
            for (int i = 0; i < n; i++)
            {
                double r = pred[i] - y[i];
                sse += r * r;
                mae += Math.Abs(r);
                double d = y[i] - yMean;
                tss += d * d;
            }
            if (double.IsNaN(sse) || double.IsInfinity(sse) || double.IsNaN(tss) || double.IsInfinity(tss))
                throw new ArgumentException("Cross-validation statistics are numerically unstable.");
            if (tss == 0)
                throw new ArgumentException("Cannot cross-validate: constant response variable y.");
            return (fiveFold ? SchemeFiveFold : SchemeLoo, 1.0 - sse / tss, mae / n);
        }

        /// <summary>
        /// True when poly is structurally unavailable for this sample: expansion over the 100-term
        /// limit, or the smallest training fold cannot exceed the terms + intercept (prevents a
        /// fold-level FitModel failure instead of a controlled skip).
        /// </summary>
        private static bool PolyExcludedBySample(int n, int k)
        {
            if (ExpandedTermCount(k, ModelPoly) > PolyTermLimit) return true;
            int terms = ExpandedTermCount(k, ModelPoly);
            int minTrain = n >= 20 ? n - (n + 4) / 5 : n - 1;
            return minTrain < terms + 2;
        }

        /// <summary>Rate is structurally unavailable when positions are missing or the smallest fold cannot fit.</summary>
        internal static bool RateExcludedBySample(int n, int k, int rateIncoming, int rateTime)
        {
            if (rateIncoming < 0 || rateTime < 0 || rateIncoming >= k || rateTime >= k || rateIncoming == rateTime) return true;
            return n < k + 1;
        }

        /// <summary>
        /// Cross-validate linear / poly / rate, pick the highest R² (ties below 1e-9 keep the earlier
        /// candidate: linear &gt; poly &gt; rate), then refit the chosen model on the full history set.
        /// </summary>
        internal static (SolveModel Model, string Chosen, string Scheme, double R2, double Mae, bool PolySkipped, bool RateSkipped) FitAuto(
            double[][] X, double[] y, long seed, int rateIncoming = -1, int rateTime = -1)
        {
            ValidateMatrix(X, y, "FitAuto");
            var linearCv = CrossValidate(X, y, ModelLinear, seed);
            bool polySkipped = PolyExcludedBySample(X.Length, X[0].Length);
            (string Scheme, double R2, double Mae) polyCv = (string.Empty, 0.0, 0.0);
            if (!polySkipped)
            {
                try { polyCv = CrossValidate(X, y, ModelPoly, seed); }
                catch (ArgumentException) { polySkipped = true; }
            }
            bool rateSkipped = RateExcludedBySample(X.Length, X[0].Length, rateIncoming, rateTime);
            (string Scheme, double R2, double Mae) rateCv = (string.Empty, 0.0, 0.0);
            if (!rateSkipped)
            {
                try { rateCv = CrossValidate(X, y, ModelRate, seed, rateIncoming, rateTime); }
                catch (ArgumentException) { rateSkipped = true; }
            }
            string chosen = ModelLinear;
            double bestR2 = linearCv.R2, bestMae = linearCv.Mae;
            string bestScheme = linearCv.Scheme;
            if (!polySkipped && polyCv.R2 > bestR2 + R2TieTolerance)
            {
                chosen = ModelPoly; bestR2 = polyCv.R2; bestMae = polyCv.Mae; bestScheme = polyCv.Scheme;
            }
            if (!rateSkipped && rateCv.R2 > bestR2 + R2TieTolerance)
            {
                chosen = ModelRate; bestR2 = rateCv.R2; bestMae = rateCv.Mae; bestScheme = rateCv.Scheme;
            }
            var model = FitModel(X, y, chosen, rateIncoming, rateTime);
            return (model, chosen, bestScheme, bestR2, bestMae, polySkipped, rateSkipped);
        }

        private static double NextUniform(ref XorShift64 rng) => (rng.Next() >> 11) * (1.0 / 9007199254740992.0);

        private static double Clamp(double v, double lo, double hi) => v < lo ? lo : v > hi ? hi : v;

        // ──────────────────────────── 反解优化与可达性 ────────────────────────────

        /// <summary>
        /// Bounded multi-start inversion for one or more request rows.
        /// X: history features n×k; Y: history outputs n×m; variableCols: adjustable column indices;
        /// requests: r×k initial values (NaN = none); targets: r×m (NaN = output not targeted);
        /// bounds: v×2 [lower, upper].
        /// Returns r×(v+m+2): recommendations… predictions… max deviation σ, status (0 = 可达, 1 = 不可达).
        /// </summary>
        internal static double[,] SolveInverse(
            double[][] X, double[][] Y, int[] variableCols, double[][] requests, double[][] targets,
            double[][] bounds, string model, long seed, int maxStarts)
            => SolveInverseFull(X, Y, variableCols, requests, targets, bounds, model, seed, maxStarts, null, out _);

        /// <summary>
        /// Full variant used by the table wrapper: ratePairs[j] = [paired incoming position, time position]
        /// selects the rate model (ADR-0008); rates returns r×m removal rates (NaN for non-rate outputs).
        /// </summary>
        internal static double[,] SolveInverseFull(
            double[][] X, double[][] Y, int[] variableCols, double[][] requests, double[][] targets,
            double[][] bounds, string model, long seed, int maxStarts, int[][]? ratePairs, out double[,]? rates)
        {
            string m = NormalizeModel(model);
            if (X == null || X.Length == 0) throw new ArgumentException("X must contain at least one history row.");
            int n = X.Length, k = X[0].Length;
            if (Y == null || Y.Length != n) throw new ArgumentException($"Y row count ({Y?.Length ?? 0}) must equal X row count ({n}).");
            int outCount = Y[0].Length;
            if (outCount == 0) throw new ArgumentException("Y must contain at least one output column.");
            for (int i = 0; i < n; i++)
                if (Y[i].Length != outCount)
                    throw new ArgumentException($"Y row {i} length differs from {outCount}.");
            if (variableCols == null || variableCols.Length == 0)
                throw new ArgumentException("At least one variable column is required.");
            int v = variableCols.Length;
            if (variableCols.Length > MaxVariables)
                throw new ArgumentException($"Too many variable columns: {variableCols.Length} (limit {MaxVariables}).");
            for (int c = 0; c < v; c++)
            {
                if (variableCols[c] < 0 || variableCols[c] >= k)
                    throw new ArgumentException($"Variable column index {variableCols[c]} is out of range [0,{k - 1}].");
                for (int d = c + 1; d < v; d++)
                    if (variableCols[d] == variableCols[c])
                        throw new ArgumentException($"Variable column index {variableCols[c]} is duplicated.");
            }
            if (bounds == null || bounds.Length != v) throw new ArgumentException($"Bounds must have {v} rows (one per variable).");
            for (int c = 0; c < v; c++)
            {
                if (bounds[c].Length != 2)
                    throw new ArgumentException($"Bounds row {c} must have 2 columns [lower, upper].");
                double lo = bounds[c][0], hi = bounds[c][1];
                if (double.IsNaN(lo) || double.IsInfinity(lo) || double.IsNaN(hi) || double.IsInfinity(hi))
                    throw new ArgumentException($"Bounds row {c} contains a non-finite value.");
                if (lo > hi)
                    throw new ArgumentException($"Bounds row {c} has lower > upper ({lo} > {hi}).");
            }
            if (requests == null || requests.Length == 0)
                throw new ArgumentException("At least one request row is required.");
            int r = requests.Length;
            if (r > MaxRequestRows) throw new ArgumentException($"Too many request rows: {r} (limit {MaxRequestRows}).");
            for (int i = 0; i < r; i++)
                if (requests[i].Length != k)
                    throw new ArgumentException($"Request row {i} must have {k} feature values.");
            if (targets == null || targets.Length != r)
                throw new ArgumentException($"Targets must have {r} rows (one per request).");
            for (int i = 0; i < r; i++)
            {
                if (targets[i].Length != outCount)
                    throw new ArgumentException($"Target row {i} must have {outCount} columns.");
                bool any = false;
                for (int j = 0; j < outCount; j++)
                    if (!double.IsNaN(targets[i][j])) { any = true; break; }
                if (!any)
                    throw new ArgumentException($"Request row {i} has no output target (all targets are blank).");
            }
            if (maxStarts < MinStarts || maxStarts > MaxStartsLimit)
                throw new ArgumentException($"max_starts must be between {MinStarts} and {MaxStartsLimit} (got {maxStarts}).");
            if (ratePairs != null)
            {
                if (ratePairs.Length != outCount)
                    throw new ArgumentException($"ratePairs must have {outCount} rows (one per output).");
                for (int j = 0; j < outCount; j++)
                    if (ratePairs[j] != null && ratePairs[j].Length != 2)
                        throw new ArgumentException($"ratePairs row {j} must have 2 entries [incoming, time].");
            }

            // One model + deviation scale per output.
            var models = new SolveModel[outCount];
            var scales = new double[outCount];
            var medians = new double[k];
            for (int c = 0; c < k; c++)
            {
                var col = new double[n];
                for (int i = 0; i < n; i++) col[i] = X[i][c];
                medians[c] = Median(col);
            }
            for (int j = 0; j < outCount; j++)
            {
                var yj = new double[n];
                for (int i = 0; i < n; i++) yj[i] = Y[i][j];
                int[]? spec = ratePairs?[j];
                int rateIncoming = spec != null && spec.Length == 2 ? spec[0] : -1;
                int rateTime = spec != null && spec.Length == 2 ? spec[1] : -1;
                models[j] = m == ModelAuto
                    ? FitAuto(X, yj, seed, rateIncoming, rateTime).Model
                    : FitModel(X, yj, m, rateIncoming, rateTime);
                double sd = SampleStdDev(yj);
                scales[j] = sd > 0 ? sd : 1.0;
            }

            // rate 时间有效性：可调时间必须边界为正；条件时间必须逐请求有限且 >0（ADR-0008）。
            for (int j = 0; j < outCount; j++)
            {
                if (models[j].Kind != ModelRate) continue;
                int timePos = models[j].RateTimeIndex;
                bool timeIsVariable = false;
                for (int c = 0; c < v; c++)
                    if (variableCols[c] == timePos)
                    {
                        timeIsVariable = true;
                        if (bounds[c][0] <= 0)
                            throw new ArgumentException(
                                $"Model 'rate' requires a positive time bound (lower bound is {bounds[c][0]}).");
                    }
                if (timeIsVariable) continue;
                for (int q = 0; q < r; q++)
                {
                    double tv = requests[q][timePos];
                    if (double.IsNaN(tv) || double.IsInfinity(tv) || tv <= 0)
                        throw new ArgumentException(
                            $"Model 'rate' requires a positive finite time in request row {q + 1} (got {tv}).");
                }
            }

            rates = null;
            for (int j = 0; j < outCount; j++)
                if (models[j].Kind == ModelRate) { rates = new double[r, outCount]; break; }

            var result = new double[r, v + outCount + 2];
            var rng = new XorShift64((ulong)seed);
            for (int q = 0; q < r; q++)
            {
                double[] request = requests[q];
                double[] target = targets[q];
                double[] feature = new double[k];
                for (int c = 0; c < k; c++) feature[c] = request[c];

                // Reachability: 2000 deterministic uniform samples inside bounds (endpoints included).
                var minOut = new double[outCount];
                var maxOut = new double[outCount];
                for (int j = 0; j < outCount; j++) { minOut[j] = double.PositiveInfinity; maxOut[j] = double.NegativeInfinity; }
                var sampleU = new double[v];
                for (int s = 0; s < ReachabilitySamples; s++)
                {
                    for (int c = 0; c < v; c++)
                    {
                        double lo = bounds[c][0], hi = bounds[c][1];
                        sampleU[c] = s == 0 ? lo : s == 1 ? hi : lo + (hi - lo) * NextUniform(ref rng);
                    }
                    for (int c = 0; c < v; c++) feature[variableCols[c]] = sampleU[c];
                    for (int j = 0; j < outCount; j++)
                    {
                        double p = Predict(models[j], feature);
                        if (double.IsNaN(p) || double.IsInfinity(p)) continue;
                        if (p < minOut[j]) minOut[j] = p;
                        if (p > maxOut[j]) maxOut[j] = p;
                    }
                }
                for (int j = 0; j < outCount; j++)
                    if (double.IsInfinity(minOut[j]) || double.IsInfinity(maxOut[j]))
                        throw new ArgumentException(
                            $"Reachability sampling produced no finite predictions for output {j} " +
                            $"(model={models[j].Kind}); model is numerically unstable.");

                Func<double[], double> objective = u =>
                {
                    for (int c = 0; c < v; c++) feature[variableCols[c]] = u[c];
                    double fTarget = 0;
                    for (int j = 0; j < outCount; j++)
                    {
                        if (double.IsNaN(target[j])) continue;
                        double p = Predict(models[j], feature);
                        if (double.IsNaN(p) || double.IsInfinity(p)) return double.PositiveInfinity;
                        double d = (p - target[j]) / scales[j];
                        fTarget += d * d;
                        if (double.IsInfinity(fTarget)) return double.PositiveInfinity;
                    }
                    double proximity = 0;
                    for (int c = 0; c < v; c++)
                    {
                        double range = bounds[c][1] - bounds[c][0];
                        if (range <= 0) continue;
                        double z = (u[c] - medians[variableCols[c]]) / range;
                        proximity += z * z;
                    }
                    return TargetPriority * fTarget + ProximityWeight * proximity;
                };

                double[]? bestU = null;
                double bestE = double.PositiveInfinity;
                for (int start = 0; start < maxStarts; start++)
                {
                    var u0 = new double[v];
                    for (int c = 0; c < v; c++)
                    {
                        double lo = bounds[c][0], hi = bounds[c][1];
                        double init = request[variableCols[c]];
                        double center = medians[variableCols[c]];
                        if (start == 0)
                            u0[c] = Clamp(double.IsNaN(init) || double.IsInfinity(init) ? center : init, lo, hi);
                        else
                            u0[c] = lo + (hi - lo) * NextUniform(ref rng);
                    }
                    var found = LocalSearch(objective, u0, bounds);
                    double e = objective(found);
                    if (e < bestE)
                    {
                        bestE = e;
                        bestU = found;
                    }
                }
                if (bestU == null)
                    throw new ArgumentException("Optimization failed to evaluate any start point.");

                for (int c = 0; c < v; c++) feature[variableCols[c]] = bestU[c];
                double maxDeviation = 0;
                bool allReachable = true;
                for (int j = 0; j < outCount; j++)
                {
                    double p = Predict(models[j], feature);
                    if (double.IsNaN(p) || double.IsInfinity(p))
                        throw new ArgumentException("Optimization produced a non-finite prediction; model is numerically unstable.");
                    if (double.IsNaN(target[j])) continue;
                    double dev = Math.Abs(p - target[j]) / scales[j];
                    if (dev > maxDeviation) maxDeviation = dev;
                    double magnitude = Math.Max(Math.Max(Math.Abs(minOut[j]), Math.Abs(maxOut[j])), Math.Abs(maxOut[j] - minOut[j]));
                    double tol = ReachToleranceFraction * Math.Max(magnitude, 1e-300);
                    bool inRange = target[j] >= minOut[j] - tol && target[j] <= maxOut[j] + tol;
                    bool achieved = dev <= AchievedSigmaTolerance;
                    if (!inRange && !achieved) allReachable = false;
                }
                for (int c = 0; c < v; c++) result[q, c] = bestU[c];
                for (int j = 0; j < outCount; j++) result[q, v + j] = Predict(models[j], feature);
                result[q, v + outCount] = maxDeviation;
                result[q, v + outCount + 1] = allReachable ? 0.0 : 1.0;
                if (rates != null)
                    for (int j = 0; j < outCount; j++)
                        rates[q, j] = models[j].Kind == ModelRate ? PredictRate(models[j], feature) : double.NaN;
            }
            return result;
        }

        /// <summary>
        /// Coordinate-rotation pattern search with step halving, bounded by clamp.
        /// Step floor = 1e-9 × bound range; per-start evaluation cap = 4000.
        /// </summary>
        private static double[] LocalSearch(Func<double[], double> eval, double[] start, double[][] bounds)
        {
            int v = start.Length;
            var u = (double[])start.Clone();
            var step = new double[v];
            for (int c = 0; c < v; c++) step[c] = (bounds[c][1] - bounds[c][0]) * 0.25;
            double best = eval(u);
            int evaluations = 1;
            while (true)
            {
                bool improved = false;
                for (int c = 0; c < v; c++)
                {
                    if (step[c] <= 0) continue;
                    for (int dir = 1; dir >= -1; dir -= 2)
                    {
                        double candidate = Clamp(u[c] + dir * step[c], bounds[c][0], bounds[c][1]);
                        if (candidate == u[c]) continue;
                        var trial = (double[])u.Clone();
                        trial[c] = candidate;
                        double e = eval(trial);
                        evaluations++;
                        if (e < best)
                        {
                            u = trial;
                            best = e;
                            improved = true;
                        }
                        if (evaluations >= MaxEvaluationsPerStart) return u;
                    }
                }
                if (improved) continue;
                bool anyLargeStep = false;
                for (int c = 0; c < v; c++)
                {
                    double range = bounds[c][1] - bounds[c][0];
                    if (range > 0 && step[c] > StepFloorFraction * range)
                    {
                        step[c] *= 0.5;
                        anyLargeStep = true;
                    }
                }
                if (!anyLargeStep) return u;
            }
        }

        // ──────────────────────────── UDF 装配层 ────────────────────────────

        private static (double[][] X, double[][] Y) BuildHistory(SolveSchema schema, object[,] data)
        {
            int n = schema.HistoryRows.Count, k = schema.Features.Length, outCount = schema.Output.Length;
            var X = new double[n][];
            var Y = new double[n][];
            var fixedFlags = new bool[k];
            for (int c = 0; c < k; c++)
            {
                int col = schema.Features[c];
                fixedFlags[c] = schema.Fixed.Contains(col);
            }
            for (int rowIdx = 0; rowIdx < n; rowIdx++)
            {
                int i = schema.HistoryRows[rowIdx] + 1;
                var x = new double[k];
                for (int c = 0; c < k; c++)
                {
                    object cell = data[i, schema.Features[c]];
                    if (IsBlank(cell))
                    {
                        if (!fixedFlags[c])
                            throw new ArgumentException(
                                $"History row {i + 1} is missing a value in column '{schema.Headers[schema.Features[c]]}'.");
                        x[c] = double.NaN; // fixed column blank → column median (filled below)
                        continue;
                    }
                    double val = InputNormalizer.ToDouble(cell);
                    if (double.IsNaN(val) || double.IsInfinity(val))
                        throw new ArgumentException(
                            $"History row {i + 1} has a non-numeric value in column '{schema.Headers[schema.Features[c]]}'.");
                    x[c] = val;
                }
                var y = new double[outCount];
                for (int j = 0; j < outCount; j++)
                {
                    object cell = data[i, schema.Output[j]];
                    double val = InputNormalizer.ToDouble(cell);
                    if (IsBlank(cell) || double.IsNaN(val) || double.IsInfinity(val))
                        throw new ArgumentException(
                            $"History row {i + 1} has a non-numeric output in column '{schema.Headers[schema.Output[j]]}'.");
                    y[j] = val;
                }
                X[rowIdx] = x;
                Y[rowIdx] = y;
            }
            // Fixed-column blanks → column median.
            for (int c = 0; c < k; c++)
            {
                bool anyNaN = false;
                for (int i = 0; i < n && !anyNaN; i++) if (double.IsNaN(X[i][c])) anyNaN = true;
                if (!anyNaN) continue;
                var col = new double[n];
                int cnt = 0;
                for (int i = 0; i < n; i++) if (!double.IsNaN(X[i][c])) col[cnt++] = X[i][c];
                if (cnt == 0)
                    throw new ArgumentException($"Fixed column '{schema.Headers[schema.Features[c]]}' has no numeric history value to impute.");
                double med = Median(col.Take(cnt).ToArray());
                for (int i = 0; i < n; i++) if (double.IsNaN(X[i][c])) X[i][c] = med;
            }
            return (X, Y);
        }

        private static double[] BuildRequestFeatures(
            SolveSchema dataSchema, object[,] data, int arrayRow,
            int[]? requestToDataCol, object[,]? request, int requestRow, string rowLabel, double[] medians)
        {
            int k = dataSchema.Features.Length;
            var x = new double[k];
            for (int c = 0; c < k; c++)
            {
                int dataCol = dataSchema.Features[c];
                object? cell;
                if (request == null)
                {
                    cell = data[arrayRow, dataCol];
                }
                else
                {
                    int mapIdx = requestToDataCol![dataCol];
                    cell = mapIdx >= 0 ? request[requestRow, mapIdx] : null;
                }
                bool isIncoming = dataSchema.Incoming.Contains(dataCol);
                bool isFixed = dataSchema.Fixed.Contains(dataCol);
                if (IsBlank(cell))
                {
                    if (isIncoming)
                        throw new ArgumentException(
                            $"Incoming column '{dataSchema.Headers[dataCol]}' is blank in {rowLabel}.");
                    x[c] = isFixed ? medians[dataCol] : double.NaN;
                    continue;
                }
                double val = InputNormalizer.ToDouble(cell);
                if (double.IsNaN(val) || double.IsInfinity(val))
                {
                    if (isIncoming)
                        throw new ArgumentException(
                            $"Incoming column '{dataSchema.Headers[dataCol]}' is non-numeric in {rowLabel}.");
                    if (isFixed)
                        throw new ArgumentException(
                            $"Fixed column '{dataSchema.Headers[dataCol]}' is non-numeric in {rowLabel}.");
                    x[c] = double.NaN; // variable cell: value acts as initial guess only
                    continue;
                }
                x[c] = val;
            }
            return x;
        }

        /// <summary>Column medians indexed by DATA column index (ignored columns stay 0).</summary>
        private static double[] BuildHistoryMedians(SolveSchema schema, double[][] X)
        {
            int k = X[0].Length, n = X.Length;
            var medians = new double[schema.Headers.Length];
            var col = new double[n];
            for (int c = 0; c < k; c++)
            {
                for (int i = 0; i < n; i++) col[i] = X[i][c];
                medians[schema.Features[c]] = Median(col);
            }
            return medians;
        }

        private static int[] MapRequestColumns(SolveSchema dataSchema, SolveSchema requestSchema)
        {
            var map = new int[dataSchema.Headers.Length];
            for (int i = 0; i < map.Length; i++) map[i] = -1;
            for (int rc = 0; rc < requestSchema.Headers.Length; rc++)
            {
                if (ClassifyHeader(requestSchema.Headers[rc]) == null) continue;
                int found = -1;
                for (int dc = 0; dc < dataSchema.Headers.Length && found < 0; dc++)
                    if (string.Equals(dataSchema.Headers[dc], requestSchema.Headers[rc], StringComparison.OrdinalIgnoreCase))
                        found = dc;
                if (found < 0)
                    throw new ArgumentException(
                        $"Request table column '{requestSchema.Headers[rc]}' does not match any data column.");
                map[found] = rc;
            }
            return map;
        }

        // ──────────────────────────── rate 配对与时间列（ADR-0008）────────────────────────────

        private sealed class SolveRatePlan
        {
            public int TimePosition;
            public int[] IncomingPositions = Array.Empty<int>();
        }

        private static string StripRolePrefix(string header, params string[] prefixes)
        {
            foreach (var p in prefixes)
                if (header.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                    return header.Substring(p.Length).Trim();
            return header.Trim();
        }

        /// <summary>Unique feature column whose header ends with "Time"/"时间" (-1 when absent; throws when required and ambiguous/missing).</summary>
        private static int TryFindTimeColumn(SolveSchema schema, bool required)
        {
            int found = -1;
            foreach (int c in schema.Features)
            {
                string h = schema.Headers[c];
                if (!h.EndsWith("Time", StringComparison.OrdinalIgnoreCase) && !h.EndsWith("时间", StringComparison.Ordinal))
                    continue;
                if (found >= 0)
                {
                    if (required)
                        throw new ArgumentException(
                            $"Multiple time columns found ('{schema.Headers[found]}', '{h}'). " +
                            "The rate model needs exactly one feature header ending with 'Time' or '时间'.");
                    return -1;
                }
                found = c;
            }
            if (found < 0 && required)
                throw new ArgumentException(
                    "The rate model requires a time column: exactly one feature header ending with 'Time' or '时间'.");
            return found;
        }

        private static int FindPairedIncoming(SolveSchema schema, int outputCol)
        {
            string suffix = StripRolePrefix(schema.Headers[outputCol], "Output", "输出");
            foreach (int c in schema.Incoming)
                if (string.Equals(StripRolePrefix(schema.Headers[c], "Incoming", "来料"), suffix, StringComparison.OrdinalIgnoreCase))
                    return c;
            return -1;
        }

        /// <summary>
        /// Build the per-output rate binding (time position + paired incoming positions).
        /// required=true (explicit model=rate) throws on missing/ambiguous binding; otherwise returns null.
        /// </summary>
        private static SolveRatePlan? BuildRatePlan(SolveSchema schema, bool required)
        {
            int timeCol = TryFindTimeColumn(schema, required);
            if (timeCol < 0) return null;
            int timePos = Array.IndexOf(schema.Features, timeCol);
            var inc = new int[schema.Output.Length];
            for (int j = 0; j < schema.Output.Length; j++)
            {
                int col = FindPairedIncoming(schema, schema.Output[j]);
                if (col < 0)
                {
                    if (required)
                        throw new ArgumentException(
                            $"The rate model requires a matching Incoming column for '{schema.Headers[schema.Output[j]]}' " +
                            "(same suffix after the role prefix, e.g. IncomingZ1 ↔ OutputZ1).");
                    return null;
                }
                inc[j] = Array.IndexOf(schema.Features, col);
            }
            return new SolveRatePlan { TimePosition = timePos, IncomingPositions = inc };
        }

        private static int[][]? BuildRatePairs(SolveRatePlan? plan, int outCount)
        {
            if (plan == null) return null;
            var pairs = new int[outCount][];
            for (int j = 0; j < outCount; j++) pairs[j] = new[] { plan.IncomingPositions[j], plan.TimePosition };
            return pairs;
        }

        /// <summary>
        /// UDF-facing entry: parse tables, fit per-output models and invert.
        /// Returns an object[,] table [请求行, variables…, outputs预测…, (速率…, 最大偏差σ, 状态)].
        /// </summary>
        internal static object[,] Inverse(object[,] data, object[,]? request, object[,]? bounds,
            string model, long seed, int maxStarts, bool hasHeaders = true)
        {
            var schema = ParseSchema(data, hasHeaders);
            if (schema.HistoryRows.Count == 0)
                throw new ArgumentException("Data table contains no history rows (every row has a blank adjustable column).");
            if (schema.HistoryRows.Count > MaxHistoryRows)
                throw new ArgumentException($"Too many history rows: {schema.HistoryRows.Count} (limit {MaxHistoryRows}).");
            int k = schema.Features.Length;
            if (k > MaxFeatureColumns)
                throw new ArgumentException($"Too many feature columns: {k} (limit {MaxFeatureColumns}).");
            if (schema.Output.Length > MaxOutputs)
                throw new ArgumentException($"Too many output columns: {schema.Output.Length} (limit {MaxOutputs}).");
            string mdl = NormalizeModel(model);
            bool rateMode = mdl == ModelRate;
            var ratePlan = rateMode || mdl == ModelAuto ? BuildRatePlan(schema, rateMode) : null;

            var (X, Y) = BuildHistory(schema, data);
            var medians = BuildHistoryMedians(schema, X);
            var requests = new List<double[]>();
            var targets = new List<double[]>();
            var labels = new List<string>();
            int outCount = schema.Output.Length;

            // Request rows embedded in data (data viewed as pure history when request table is given).
            if (request != null && schema.RequestRows.Count > 0)
                throw new ArgumentException(
                    "Data table must not contain request rows when a separate request table is provided.");
            if (request == null)
            {
                foreach (int dataRow in schema.RequestRows)
                {
                    int arrayRow = dataRow + 1;
                    requests.Add(BuildRequestFeatures(schema, data, arrayRow, null, null, 0, $"data row {arrayRow + 1}", medians));
                    var t = new double[outCount];
                    bool any = false;
                    for (int j = 0; j < outCount; j++)
                    {
                        object cell = data[arrayRow, schema.Output[j]];
                        if (IsBlank(cell)) { t[j] = double.NaN; continue; }
                        double targetValue = InputNormalizer.ToDouble(cell);
                        if (double.IsNaN(targetValue) || double.IsInfinity(targetValue))
                            throw new ArgumentException(
                                $"Request row (data row {arrayRow + 1}) has a non-numeric target in column '{schema.Headers[schema.Output[j]]}'.");
                        t[j] = targetValue;
                        any = true;
                    }
                    if (!any)
                        throw new ArgumentException($"Request row (data row {arrayRow + 1}) has no output target.");
                    targets.Add(t);
                    labels.Add($"数据第{arrayRow + 1}行");
                }
            }
            else
            {
                var requestSchema = ParseSchema(request, true);
                var map = MapRequestColumns(schema, requestSchema);
                int rows = request.GetLength(0);
                int emitted = 0;
                for (int i = 1; i < rows; i++)
                {
                    bool allBlank = true;
                    for (int c = 0; c < request.GetLength(1) && allBlank; c++)
                        if (!IsBlank(request[i, c])) allBlank = false;
                    if (allBlank) continue;
                    requests.Add(BuildRequestFeatures(schema, data, 0, map, request, i, $"request table row {i + 1}", medians));
                    var t = new double[outCount];
                    bool any = false;
                    for (int j = 0; j < outCount; j++)
                    {
                        int rc = map[schema.Output[j]];
                        object? cell = rc >= 0 ? request[i, rc] : null;
                        if (IsBlank(cell)) { t[j] = double.NaN; continue; }
                        double targetValue = InputNormalizer.ToDouble(cell);
                        if (double.IsNaN(targetValue) || double.IsInfinity(targetValue))
                            throw new ArgumentException(
                                $"Request table row {i + 1} has a non-numeric target in column '{schema.Headers[schema.Output[j]]}'.");
                        t[j] = targetValue;
                        any = true;
                    }
                    if (!any)
                        throw new ArgumentException($"Request table row {i + 1} has no output target.");
                    targets.Add(t);
                    emitted++;
                    labels.Add($"请求{emitted}");
                }
            }
            if (requests.Count == 0)
                throw new ArgumentException("No request rows found. Leave at least one adjustable column blank (or pass a request table).");
            if (requests.Count > MaxRequestRows)
                throw new ArgumentException($"Too many request rows: {requests.Count} (limit {MaxRequestRows}).");

            // SolveInverse 的 variableCols 是**特征矩阵列位置**；表列索引需先经 Features 映射。
            // （Features 顺序 = incoming + variable + fixed，固定列插在可调列之前时两者不相等。）
            var variablePositions = new int[schema.Variable.Length];
            for (int c = 0; c < variablePositions.Length; c++)
                variablePositions[c] = Array.IndexOf(schema.Features, schema.Variable[c]);
            var boundsArr = BuildBounds(schema, X, bounds);
            var ratePairs = BuildRatePairs(ratePlan, outCount);
            var core = SolveInverseFull(X, Y, variablePositions, requests.ToArray(), targets.ToArray(),
                boundsArr, mdl, seed, maxStarts, ratePairs, out var rates);

            // 速率块：显式 rate，或 auto 下至少一个输出选中 rate（否则布局与 v1 完全一致）。
            bool rateBlock = false;
            if (rates != null)
                for (int j = 0; j < outCount && !rateBlock; j++)
                    rateBlock = !double.IsNaN(rates[0, j]);

            int v = schema.Variable.Length;
            int width = 1 + v + outCount + (rateBlock ? outCount : 0) + 2;
            var table = new object[core.GetLength(0) + 1, width];
            table[0, 0] = "请求行";
            for (int c = 0; c < v; c++) table[0, 1 + c] = schema.Headers[schema.Variable[c]];
            for (int j = 0; j < outCount; j++) table[0, 1 + v + j] = schema.Headers[schema.Output[j]] + "预测";
            int sigmaCol = 1 + v + outCount;
            if (rateBlock)
            {
                for (int j = 0; j < outCount; j++)
                    table[0, sigmaCol + j] = schema.Headers[schema.Output[j]] + RateColumnSuffix;
                sigmaCol += outCount;
            }
            table[0, sigmaCol] = "最大偏差σ";
            table[0, width - 1] = "状态";
            for (int q = 0; q < core.GetLength(0); q++)
            {
                table[q + 1, 0] = labels[q];
                for (int c = 0; c < v; c++) table[q + 1, 1 + c] = core[q, c];
                for (int j = 0; j < outCount; j++) table[q + 1, 1 + v + j] = core[q, v + j];
                if (rateBlock)
                    for (int j = 0; j < outCount; j++)
                    {
                        double rate = rates![q, j];
                        table[q + 1, 1 + v + outCount + j] = double.IsNaN(rate) ? null! : (object)rate;
                    }
                table[q + 1, sigmaCol] = core[q, v + outCount];
                table[q + 1, width - 1] = core[q, v + outCount + 1] == 0.0 ? StatusReachable : StatusUnreachable;
            }
            return table;
        }

        private static double[][] BuildBounds(SolveSchema schema, double[][] X, object[,]? bounds)
        {
            int n = X.Length, v = schema.Variable.Length;
            var lo = new double[v];
            var hi = new double[v];
            for (int c = 0; c < v; c++)
            {
                int pos = Array.IndexOf(schema.Features, schema.Variable[c]);
                double min = double.PositiveInfinity, max = double.NegativeInfinity;
                for (int i = 0; i < n; i++)
                {
                    double val = X[i][pos];
                    if (val < min) min = val;
                    if (val > max) max = val;
                }
                lo[c] = min;
                hi[c] = max;
            }
            if (bounds == null || (bounds.GetLength(0) == 1 && IsBlank(bounds[0, 0])))
                return WrapBounds(lo, hi);
            int rows = bounds.GetLength(0);
            if (rows == 0) return WrapBounds(lo, hi);
            int cols = bounds.GetLength(1);
            if (cols == 2)
            {
                if (rows > v)
                    throw new ArgumentException($"Bounds table has {rows} rows but there are only {v} variable columns.");
                for (int i = 0; i < rows; i++)
                {
                    lo[i] = InputNormalizer.ToDouble(bounds[i, 0]);
                    hi[i] = InputNormalizer.ToDouble(bounds[i, 1]);
                    ValidateBoundPair(lo[i], hi[i], i + 1);
                }
                return WrapBounds(lo, hi);
            }
            if (cols == 3)
            {
                for (int i = 0; i < rows; i++)
                {
                    object key = bounds[i, 0];
                    int idx;
                    if (IsBlank(key))
                        throw new ArgumentException($"Bounds table row {i + 1} is missing the variable name/index.");
                    if (InputNormalizer.IsNumericCell(key))
                    {
                        long oneBased = InputNormalizer.ToLong(key);
                        if (oneBased < 1 || oneBased > v)
                            throw new ArgumentException(
                                $"Bounds table row {i + 1}: variable index {oneBased} is out of range [1,{v}].");
                        idx = (int)oneBased - 1;
                    }
                    else
                    {
                        string name = InputNormalizer.ToString(key).Trim();
                        idx = -1;
                        for (int c = 0; c < v && idx < 0; c++)
                            if (string.Equals(schema.Headers[schema.Variable[c]], name, StringComparison.OrdinalIgnoreCase))
                                idx = c;
                        if (idx < 0)
                            throw new ArgumentException(
                                $"Bounds table row {i + 1}: variable '{name}' does not match any variable column.");
                    }
                    lo[idx] = InputNormalizer.ToDouble(bounds[i, 1]);
                    hi[idx] = InputNormalizer.ToDouble(bounds[i, 2]);
                    ValidateBoundPair(lo[idx], hi[idx], i + 1);
                }
                return WrapBounds(lo, hi);
            }
            throw new ArgumentException($"Bounds table must have 2 (by variable order) or 3 (name/index, lower, upper) columns; got {cols}.");
        }

        private static void ValidateBoundPair(double lo, double hi, int row)
        {
            if (double.IsNaN(lo) || double.IsInfinity(lo) || double.IsNaN(hi) || double.IsInfinity(hi))
                throw new ArgumentException($"Bounds table row {row} contains a non-numeric bound.");
            if (lo > hi)
                throw new ArgumentException($"Bounds table row {row} has lower > upper ({lo} > {hi}).");
        }

        private static double[][] WrapBounds(double[] lo, double[] hi)
        {
            var bounds = new double[lo.Length][];
            for (int c = 0; c < lo.Length; c++) bounds[c] = new[] { lo[c], hi[c] };
            return bounds;
        }

        /// <summary>Forward prediction for one or more complete parameter rows → n×m double matrix.</summary>
        internal static double[,] PredictTable(object[,] data, object[,] values, string model, bool hasHeaders = true)
        {
            var schema = ParseSchema(data, hasHeaders);
            if (schema.HistoryRows.Count == 0)
                throw new ArgumentException("Data table contains no history rows.");
            var (X, Y) = BuildHistory(schema, data);
            var medians = BuildHistoryMedians(schema, X);
            string mdl = NormalizeModel(model);
            bool rateMode = mdl == ModelRate;
            var ratePlan = rateMode || mdl == ModelAuto ? BuildRatePlan(schema, rateMode) : null;
            int outCount = schema.Output.Length;
            var models = new SolveModel[outCount];
            for (int j = 0; j < outCount; j++)
            {
                var yj = new double[X.Length];
                for (int i = 0; i < X.Length; i++) yj[i] = Y[i][j];
                int rateIncoming = ratePlan?.IncomingPositions[j] ?? -1;
                int rateTime = ratePlan?.TimePosition ?? -1;
                models[j] = mdl == ModelAuto
                    ? FitAuto(X, yj, 42L, rateIncoming, rateTime).Model
                    : FitModel(X, yj, mdl, rateIncoming, rateTime);
            }
            int k = schema.Features.Length;
            int rows = values.GetLength(0);
            if (values.GetLength(1) != k)
                throw new ArgumentException(
                    $"Values table must have {k} columns (Incoming + Variable + Fixed, in data order); got {values.GetLength(1)}.");
            var result = new double[rows, outCount];
            var feature = new double[k];
            for (int i = 0; i < rows; i++)
            {
                for (int c = 0; c < k; c++)
                {
                    int dataCol = schema.Features[c];
                    object cell = values[i, c];
                    if (IsBlank(cell))
                    {
                        if (schema.Fixed.Contains(dataCol)) { feature[c] = medians[dataCol]; continue; }
                        throw new ArgumentException(
                            $"Values row {i + 1} is missing a value in column '{schema.Headers[dataCol]}'.");
                    }
                    double v = InputNormalizer.ToDouble(cell);
                    if (double.IsNaN(v) || double.IsInfinity(v))
                        throw new ArgumentException(
                            $"Values row {i + 1} has a non-numeric value in column '{schema.Headers[dataCol]}'.");
                    feature[c] = v;
                }
                for (int j = 0; j < outCount; j++)
                {
                    double p = Predict(models[j], feature);
                    if (double.IsNaN(p) || double.IsInfinity(p))
                        throw new ArgumentException("Prediction produced a non-finite value; model is numerically unstable.");
                    result[i, j] = p;
                }
            }
            return result;
        }

        /// <summary>
        /// Cross-validated model quality per output / candidate.
        /// Table [输出, 候选, CV方案, CV_R2, CV_MAE, 选用].
        /// </summary>
        internal static object[,] Quality(object[,] data, string model, long seed, bool hasHeaders = true)
        {
            var schema = ParseSchema(data, hasHeaders);
            if (schema.HistoryRows.Count == 0)
                throw new ArgumentException("Data table contains no history rows.");
            var (X, Y) = BuildHistory(schema, data);
            string mdl = NormalizeModel(model);
            bool rateMode = mdl == ModelRate;
            var ratePlan = rateMode || mdl == ModelAuto ? BuildRatePlan(schema, rateMode) : null;
            int k = schema.Features.Length;
            int outCount = schema.Output.Length;
            var rows = new List<object[]>();
            for (int j = 0; j < outCount; j++)
            {
                var yj = new double[X.Length];
                for (int i = 0; i < X.Length; i++) yj[i] = Y[i][j];
                string outputName = schema.Headers[schema.Output[j]];
                int rateIncoming = ratePlan?.IncomingPositions[j] ?? -1;
                int rateTime = ratePlan?.TimePosition ?? -1;
                if (mdl == ModelAuto)
                {
                    var lin = CrossValidate(X, yj, ModelLinear, seed);
                    bool polySkipped = PolyExcludedBySample(X.Length, k);
                    (string Scheme, double R2, double Mae) poly = default;
                    if (!polySkipped)
                    {
                        try { poly = CrossValidate(X, yj, ModelPoly, seed); }
                        catch (ArgumentException) { polySkipped = true; }
                    }
                    bool rateSkipped = RateExcludedBySample(X.Length, k, rateIncoming, rateTime);
                    (string Scheme, double R2, double Mae) rate = default;
                    if (!rateSkipped)
                    {
                        try { rate = CrossValidate(X, yj, ModelRate, seed, rateIncoming, rateTime); }
                        catch (ArgumentException) { rateSkipped = true; }
                    }
                    string best = ModelLinear;
                    double bestR2 = lin.R2;
                    if (!polySkipped && poly.R2 > bestR2 + R2TieTolerance) { best = ModelPoly; bestR2 = poly.R2; }
                    if (!rateSkipped && rate.R2 > bestR2 + R2TieTolerance) { best = ModelRate; bestR2 = rate.R2; }
                    rows.Add(new object[] { outputName, ModelLinear, lin.Scheme, lin.R2, lin.Mae, best == ModelLinear ? "是" : "否" });
                    if (polySkipped)
                        rows.Add(new object[] { outputName, ModelPoly, SchemeSkipped, null!, null!, "否" });
                    else
                        rows.Add(new object[] { outputName, ModelPoly, poly.Scheme ?? string.Empty, poly.R2, poly.Mae, best == ModelPoly ? "是" : "否" });
                    if (rateSkipped)
                        rows.Add(new object[] { outputName, ModelRate, SchemeSkipped, null!, null!, "否" });
                    else
                        rows.Add(new object[] { outputName, ModelRate, rate.Scheme ?? string.Empty, rate.R2, rate.Mae, best == ModelRate ? "是" : "否" });
                }
                else
                {
                    var cv = CrossValidate(X, yj, mdl, seed, rateIncoming, rateTime);
                    rows.Add(new object[] { outputName, mdl, cv.Scheme, cv.R2, cv.Mae, "是" });
                }
            }
            return Report(rows, new[] { "输出", "候选", "CV方案", "CV_R2", "CV_MAE", "选用" });
        }

        private static object[,] Report(List<object[]> rows, string[] headers)
        {
            var table = new object[rows.Count + 1, headers.Length];
            for (int c = 0; c < headers.Length; c++) table[0, c] = headers[c];
            for (int r = 0; r < rows.Count; r++)
                for (int c = 0; c < headers.Length; c++) table[r + 1, c] = rows[r][c];
            return table;
        }

        /// <summary>
        /// Equation text per output. Table [输出, 类型, 表达式]; the inverse formula is emitted only
        /// for a single-variable linear model (closed-form solvable).
        /// </summary>
        internal static object[,] Equation(object[,] data, string model, bool hasHeaders = true)
        {
            var schema = ParseSchema(data, hasHeaders);
            if (schema.HistoryRows.Count == 0)
                throw new ArgumentException("Data table contains no history rows.");
            var (X, Y) = BuildHistory(schema, data);
            string mdl = NormalizeModel(model);
            bool rateMode = mdl == ModelRate;
            var ratePlan = rateMode || mdl == ModelAuto ? BuildRatePlan(schema, rateMode) : null;
            int outCount = schema.Output.Length;
            var rows = new List<object[]>();
            var names = schema.Features.Select(c => schema.Headers[c]).ToArray();
            for (int j = 0; j < outCount; j++)
            {
                var yj = new double[X.Length];
                for (int i = 0; i < X.Length; i++) yj[i] = Y[i][j];
                int rateIncoming = ratePlan?.IncomingPositions[j] ?? -1;
                int rateTime = ratePlan?.TimePosition ?? -1;
                var modelJ = mdl == ModelAuto
                    ? FitAuto(X, yj, 42L, rateIncoming, rateTime).Model
                    : FitModel(X, yj, mdl, rateIncoming, rateTime);
                string outputName = schema.Headers[schema.Output[j]];
                if (modelJ.Kind == ModelRate)
                {
                    string incomingName = names[modelJ.RateIncomingIndex];
                    string timeName = names[modelJ.RateTimeIndex];
                    rows.Add(new object[] { outputName, TypeForward, ForwardRateEquation(modelJ, names, outputName, incomingName, timeName) });
                    rows.Add(new object[] { outputName, TypeRate, outputName + RateColumnSuffix + " = " + Expression(modelJ, names) });
                }
                else
                {
                    rows.Add(new object[] { outputName, TypeForward, ForwardEquation(modelJ, names, outputName) });
                    if (modelJ.Kind == ModelLinear && schema.Variable.Length == 1)
                    {
                        int varIdx = schema.Features.ToList().IndexOf(schema.Variable[0]);
                        string? inverse = InverseFormula(modelJ, names, varIdx, outputName);
                        if (inverse != null)
                            rows.Add(new object[] { outputName, TypeInverse, inverse });
                    }
                }
            }
            return Report(rows, new[] { "输出", "类型", "表达式" });
        }

        private static string Num(double value) => value.ToString("G6", CultureInfo.InvariantCulture);

        private static string TermFactor(SolveModel model, int term, string[] names)
        {
            var parts = new List<string>();
            var powers = model.Powers[term];
            for (int j = 0; j < powers.Length; j++)
            {
                if (powers[j] == 0) continue;
                if (powers[j] == 1) parts.Add(names[j]);
                else parts.Add(names[j] + "^" + powers[j].ToString(CultureInfo.InvariantCulture));
            }
            return string.Join("*", parts);
        }

        /// <summary>Right-hand expression g/y of a fitted model (without the LHS).</summary>
        private static string Expression(SolveModel model, string[] names)
        {
            var sb = new StringBuilder();
            sb.Append(Num(model.Intercept));
            for (int t = 0; t < model.Coef.Length; t++)
            {
                double c = model.Coef[t];
                if (c == 0.0) continue;
                sb.Append(c > 0 ? " + " : " - ");
                double abs = Math.Abs(c);
                string factor = TermFactor(model, t, names);
                if (factor.Length == 0)
                    sb.Append(Num(abs));
                else if (abs == 1.0)
                    sb.Append(factor);
                else
                    sb.Append(Num(abs)).Append('*').Append(factor);
            }
            return sb.ToString();
        }

        private static string ForwardEquation(SolveModel model, string[] names, string outputName)
            => outputName + " = " + Expression(model, names);

        /// <summary>Rate forward equation: Output(t) = Incoming − t·g(·).</summary>
        private static string ForwardRateEquation(SolveModel model, string[] names, string outputName,
            string incomingName, string timeName)
            => outputName + "(" + timeName + ") = " + incomingName + " - " + timeName + "*(" + Expression(model, names) + ")";

        private static string? InverseFormula(SolveModel model, string[] names, int varIdx, string outputName)
        {
            double cVar = 0;
            for (int t = 0; t < model.Coef.Length; t++)
            {
                var powers = model.Powers[t];
                if (powers[varIdx] == 1 && powers.Sum() == 1) { cVar = model.Coef[t]; break; }
            }
            if (cVar == 0.0) return null; // variable has no linear effect → no closed-form inversion
            var sb = new StringBuilder();
            sb.Append(names[varIdx]).Append(" = (").Append(outputName);
            if (model.Intercept > 0) sb.Append(" - ").Append(Num(model.Intercept));
            else if (model.Intercept < 0) sb.Append(" + ").Append(Num(-model.Intercept));
            for (int t = 0; t < model.Coef.Length; t++)
            {
                var powers = model.Powers[t];
                if (powers.Sum() != 1 || powers[varIdx] == 1) continue;
                int j = Array.IndexOf(powers, 1);
                if (j < 0) continue;
                double c = model.Coef[t];
                if (c == 0.0) continue;
                sb.Append(c > 0 ? " - " : " + ");
                double abs = Math.Abs(c);
                if (abs == 1.0) sb.Append(names[j]);
                else sb.Append(Num(abs)).Append('*').Append(names[j]);
            }
            sb.Append(") / ").Append(Num(cVar));
            return sb.ToString();
        }
    }
}
