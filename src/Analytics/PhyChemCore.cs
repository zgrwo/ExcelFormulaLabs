using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.Analytics
{
    /// <summary>
    /// Physical chemistry: molecular weight, unit conversions, gas standard state.
    /// Ported from PhyChemUtils.bas.
    /// </summary>
    internal static class PhyChemCore
    {
        private static readonly Dictionary<string, double> AtomicWeights = new(StringComparer.Ordinal)
        {
            ["H"]=1.008,["He"]=4.002602,["Li"]=6.94,["Be"]=9.0121831,["B"]=10.81,["C"]=12.011,
            ["N"]=14.007,["O"]=15.999,["F"]=18.998403163,["Ne"]=20.1797,["Na"]=22.98976928,["Mg"]=24.305,
            ["Al"]=26.9815385,["Si"]=28.085,["P"]=30.973762,["S"]=32.066,["Cl"]=35.453,["Ar"]=39.948,
            ["K"]=39.0983,["Ca"]=40.078,["Sc"]=44.955908,["Ti"]=47.867,["V"]=50.9415,["Cr"]=51.9961,
            ["Mn"]=54.938044,["Fe"]=55.845,["Co"]=58.933194,["Ni"]=58.6934,["Cu"]=63.546,["Zn"]=65.38,
            ["Ga"]=69.723,["Ge"]=72.630,["As"]=74.921595,["Se"]=78.971,["Br"]=79.904,["Kr"]=83.798,
            ["Rb"]=85.4678,["Sr"]=87.62,["Y"]=88.90584,["Zr"]=91.224,["Nb"]=92.90637,["Mo"]=95.95,
            ["Tc"]=98.0,["Ru"]=101.07,["Rh"]=102.90550,["Pd"]=106.42,["Ag"]=107.8682,["Cd"]=112.414,
            ["In"]=114.818,["Sn"]=118.710,["Sb"]=121.760,["Te"]=127.60,["I"]=126.90447,["Xe"]=131.293,
            ["Cs"]=132.90545196,["Ba"]=137.327,["La"]=138.90547,["Ce"]=140.116,["Pr"]=140.90766,["Nd"]=144.242,
            ["Pm"]=145.0,["Sm"]=150.36,["Eu"]=151.964,["Gd"]=157.25,["Tb"]=158.92535,["Dy"]=162.500,
            ["Ho"]=164.93033,["Er"]=167.259,["Tm"]=168.93422,["Yb"]=173.045,["Lu"]=174.9668,["Hf"]=178.49,
            ["Ta"]=180.94788,["W"]=183.84,["Re"]=186.207,["Os"]=190.23,["Ir"]=192.217,["Pt"]=195.084,
            ["Au"]=196.966569,["Hg"]=200.592,["Tl"]=204.38,["Pb"]=207.2,["Bi"]=208.98040,["Po"]=209.0,
            ["At"]=210.0,["Rn"]=222.0,["Fr"]=223.0,["Ra"]=226.0,["Ac"]=227.0,["Th"]=232.0377,
            ["Pa"]=231.03588,["U"]=238.02891,["Np"]=237.0,["Pu"]=244.0,["Am"]=243.0,["Cm"]=247.0,
            ["Bk"]=247.0,["Cf"]=251.0,["Es"]=252.0,["Fm"]=257.0,
        };

        private static readonly Regex ElemRx = new(@"([A-Z][a-z]?)(\d*)", RegexOptions.Compiled, TimeSpan.FromSeconds(5));
        // review 2026-09-05（N06）：整串消费校验——元素记号必须覆盖全部输入（与 ElemRx 同构）。
        private static readonly Regex ElemFullRx = new(@"^([A-Z][a-z]?\d*)+$", RegexOptions.Compiled, TimeSpan.FromSeconds(5));
        private static readonly Regex ParenRx = new(@"\(([^()]+)\)(\d*)", RegexOptions.Compiled, TimeSpan.FromSeconds(5));
        private static readonly Regex BrackRx = new(@"\[([^\[\]]+)\](\d*)", RegexOptions.Compiled, TimeSpan.FromSeconds(5));

        /// <summary>
        /// Compute molecular weight from a chemical formula string.
        /// Supports hydrates (CuSO4.5H2O), parentheses Ca(OH)2, and brackets [Fe(CN)6].
        /// Limitation: the regex-based parser handles one level of bracket/parenthesis
        /// nesting (e.g. Fe4[Fe(CN)6]3 = Prussian blue). Deeper nesting like
        /// Ca[Fe[(CN)6]2]3 may produce incorrect results because bracket/paren
        /// expansion is sequential, not iterative.
        /// </summary>
        internal static double MolecularWeight(string formula) => MolecularWeight(formula, 0);

        private static double MolecularWeight(string formula, int depth)
        {
            const int maxDepth = 100; // Guard against stack overflow from pathological hydrate chains
            if (depth > maxDepth)
                throw new ArgumentException(
                    $"Formula nesting depth exceeds {maxDepth}. The formula may be malformed or contain too many hydrate segments.");
            if (string.IsNullOrWhiteSpace(formula)) return double.NaN;
            if (formula.Contains("."))
            {
                string[] parts = formula.Split('.');
                double total = MolecularWeight(parts[0], depth + 1);
                if (double.IsNaN(total)) return double.NaN;
                for (int i = 1; i < parts.Length; i++)
                {
                    string p = parts[i];
                    // review 2026-08-29（发行前 max level 复审）：水合物系数用裸 int 逐位累积，
                    // 编译未开 CheckForOverflowUnderflow → 超 int.MaxValue（≥10 位）时静默回绕为负数，
                    // 产生错误的（可能负的）分子量。与 ParseCount 的溢出防护对齐，改为显式抛错。
                    int coeff = 0, j = 0;
                    while (j < p.Length && char.IsDigit(p[j]))
                    {
                        int digit = p[j] - '0';
                        if (coeff > (int.MaxValue - digit) / 10)
                            throw new ArgumentException(
                                $"Hydrate coefficient '{p.Substring(0, j + 1)}…' is too large. The maximum supported coefficient is {int.MaxValue}.");
                        coeff = coeff * 10 + digit;
                        j++;
                    }
                    if (coeff == 0) coeff = 1;
                    string sub = p.Substring(j);
                    double pm = MolecularWeight(sub, depth + 1);
                    if (double.IsNaN(pm)) return double.NaN;
                    total += coeff * pm;
                }
                return total;
            }
            formula = BrackRx.Replace(formula, m =>
                ExpandGroup(m.Groups[1].Value, ParseCount(m.Groups[2].Value)));
            formula = ParenRx.Replace(formula, m =>
                ExpandGroup(m.Groups[1].Value, ParseCount(m.Groups[2].Value)));
            // review 2026-09-05（N06）：元素扫描后若残留未消费字符（如 "H2Oxyz"），原实现
            // 静默按已匹配部分计算（= 18.015），而全小写 "h2o"（零匹配）却返回 NaN——拒收
            // 不一致。改为整串必须被元素记号完全消费，残留 → NaN（拒收口径一致化）。
            if (!ElemFullRx.IsMatch(formula)) return double.NaN;
            double mw = 0;
            bool matched = false;
            foreach (Match m in ElemRx.Matches(formula))
            {
                matched = true;
                string elem = m.Groups[1].Value;
                int cnt = ParseCount(m.Groups[2].Value);
                if (AtomicWeights.TryGetValue(elem, out double w))
                    mw += w * cnt;
                else return double.NaN;
            }
            // No element matched → invalid formula (e.g. all-lowercase "h2o")
            return matched ? mw : double.NaN;
        }

        private static string ExpandGroup(string inner, int mult) =>
            ElemRx.Replace(inner, m =>
            {
                string el = m.Groups[1].Value;
                int c = ParseCount(m.Groups[2].Value) * mult;
                return c > 1 ? $"{el}{c}" : el;
            });

        private static int ParseCount(string s)
        {
            if (string.IsNullOrEmpty(s)) return 1;
            // Try int first for the common case; fall back to long for overflow detection
            if (int.TryParse(s, out int n)) return n;
            // review 2026-08-29：long.TryParse 也失败（如超 19 位数字）时原实现静默返回 1
            // （H99999999999999999999 → 1.008 按 H1 解析）——违反防错原则，改为显式抛错。
            if (long.TryParse(s, out long big))
                throw new ArgumentException(
                    $"Subscript '{s}' is too large. The maximum supported subscript is {int.MaxValue}.");
            if (Regex.IsMatch(s, @"^\d+$", RegexOptions.None, TimeSpan.FromSeconds(5))) // 纯数字但超 long 范围（>19 位）
                throw new ArgumentException(
                    $"Subscript '{s}' is too large. The maximum supported subscript is {int.MaxValue}.");
            return 1; // non-numeric → treat as 1 (e.g. malformed token)
        }

        internal static double ConvertTemperature(double v, string from, string to)
        {
            if (double.IsNaN(v) || double.IsInfinity(v)) return double.NaN;
            double k = from.ToUpperInvariant() switch
            {
                "C" or "CELSIUS" => v + 273.15,
                "F" or "FAHRENHEIT" => (v - 32) * 5.0 / 9.0 + 273.15,
                "K" or "KELVIN" => v,
                _ => double.NaN,
            };
            // review 2026-09-05（N08b）：绝对零守卫（对齐 GASSTP 的 tK<=0 拒收）——k 即
            // 换算中间量/结果所在的 K 温标值，≤ 0 物理无意义（-300℃ → -26.85 K 曾静默返回）。
            if (double.IsNaN(k) || k <= 0) return double.NaN;
            // review 2026-09-05（N08a）：有限大输入（1e308）× 换算常数可溢出 ±Inf → NaN 封顶
            // （模块约定，见 CapNaN）。
            return CapNaN(to.ToUpperInvariant() switch
            {
                "C" or "CELSIUS" => k - 273.15,
                "F" or "FAHRENHEIT" => (k - 273.15) * 9.0 / 5.0 + 32,
                "K" or "KELVIN" => k,
                _ => double.NaN,
            });
        }

        internal static double ConvertPressure(double v, string from, string to)
        {
            if (double.IsNaN(v) || double.IsInfinity(v)) return double.NaN;
            double pa = from.ToUpperInvariant() switch
            {
                "ATM" => v * 101325, "PA" or "PASCAL" => v, "KPA" => v * 1000,
                "BAR" => v * 100000, "PSI" => v * 6894.757293168, "MMHG" or "TORR" => v * 133.322387415,
                _ => double.NaN,
            };
            if (double.IsNaN(pa)) return double.NaN;
            // review 2026-09-05（N08a）：有限大输入 × 换算常数（如 1e308 atm → Pa）可溢出
            // ±Inf → NaN 封顶（模块约定）。
            return CapNaN(to.ToUpperInvariant() switch
            {
                "ATM" => pa / 101325, "PA" or "PASCAL" => pa, "KPA" => pa / 1000,
                "BAR" => pa / 100000, "PSI" => pa / 6894.757293168, "MMHG" or "TORR" => pa / 133.322387415,
                _ => double.NaN,
            });
        }

        internal static double ConvertVolume(double v, string from, string to)
        {
            if (double.IsNaN(v) || double.IsInfinity(v)) return double.NaN;
            double l = from.ToUpperInvariant() switch
            {
                "L" or "LITER" => v, "ML" => v / 1000.0, "M3" => v * 1000,
                "GAL" or "GALLON" => v * 3.78541, "QT" or "QUART" => v * 0.946353,
                "FT3" => v * 28.3168, _ => double.NaN,
            };
            // review 2026-09-05（N08a）：有限大输入 × 换算常数可溢出 ±Inf → NaN 封顶（模块约定）。
            return CapNaN(to.ToUpperInvariant() switch
            {
                "L" or "LITER" => l, "ML" => l * 1000, "M3" => l / 1000,
                "GAL" or "GALLON" => l / 3.78541, "QT" or "QUART" => l / 0.946353,
                "FT3" => l / 28.3168, _ => double.NaN,
            });
        }

        internal static double ConvertMass(double v, string from, string to)
        {
            if (double.IsNaN(v) || double.IsInfinity(v)) return double.NaN;
            double g = from.ToUpperInvariant() switch
            {
                "KG" => v * 1000, "G" or "GRAM" => v, "MG" => v / 1000.0,
                "LB" or "LBS" => v * 453.59237, "OZ" => v * 28.3495, "TON" => v * 1e6,
                _ => double.NaN,
            };
            // review 2026-09-05（N08a）：有限大输入 × 换算常数可溢出 ±Inf → NaN 封顶（模块约定）。
            return CapNaN(to.ToUpperInvariant() switch
            {
                "KG" => g / 1000, "G" or "GRAM" => g, "MG" => g * 1000,
                "LB" or "LBS" => g / 453.59237, "OZ" => g / 28.3495, "TON" => g / 1e6,
                _ => double.NaN,
            });
        }

        internal static double IdealGasLaw(double? p = null, double? v = null,
            double? n = null, double? t = null, double r = 0.082057)
        {
            // Reject NaN/Inf in supplied parameters (防错原则1)
            if (p.HasValue && (double.IsNaN(p.Value) || double.IsInfinity(p.Value))) return double.NaN;
            if (v.HasValue && (double.IsNaN(v.Value) || double.IsInfinity(v.Value))) return double.NaN;
            if (n.HasValue && (double.IsNaN(n.Value) || double.IsInfinity(n.Value))) return double.NaN;
            if (t.HasValue && (double.IsNaN(t.Value) || double.IsInfinity(t.Value))) return double.NaN;
            if (r == 0 || double.IsNaN(r) || double.IsInfinity(r)) return double.NaN;
            int missing = (p.HasValue?0:1)+(v.HasValue?0:1)+(n.HasValue?0:1)+(t.HasValue?0:1);
            if (missing != 1) return double.NaN;
            // Each branch handles the case where exactly one parameter is missing.
            // The missing count above guarantees the other three have values,
            // but we use explicit .HasValue checks (not !) for defence-in-depth (防错原则1).
            if (!p.HasValue)
            {
                if (!v.HasValue || !n.HasValue || !t.HasValue) return double.NaN;
                // review 2026-08-31（深度审查 P2-12）：溢出 ±Inf → NaN（模块约定）。
                return v.Value == 0 ? double.NaN : CapNaN(n.Value * r * t.Value / v.Value);
            }
            if (!v.HasValue)
            {
                if (!p.HasValue || !n.HasValue || !t.HasValue) return double.NaN;
                return p.Value == 0 ? double.NaN : CapNaN(n.Value * r * t.Value / p.Value);
            }
            if (!n.HasValue)
            {
                if (!p.HasValue || !v.HasValue || !t.HasValue) return double.NaN;
                return t.Value == 0 ? double.NaN : CapNaN(p.Value * v.Value / (r * t.Value));
            }
            if (!p.HasValue || !v.HasValue || !n.HasValue) return double.NaN;
            return n.Value == 0 ? double.NaN : CapNaN(p.Value * v.Value / (n.Value * r));
        }

        internal static double GasToSTP(double vol, double temp, double press,
            string tUnit = "C", string pUnit = "atm")
        {
            if (double.IsNaN(vol) || double.IsInfinity(vol) || vol < 0)
                return double.NaN;
            double tK = ConvertTemperature(temp, tUnit, "K");
            double pAtm = ConvertPressure(press, pUnit, "atm");
            if (double.IsNaN(tK) || double.IsNaN(pAtm) || tK <= 0 || pAtm <= 0)
                return double.NaN;
            // review 2026-08-31（深度审查 P2-12）：溢出时原样返回 ±Inf，与模块
            // "Infinity capped to NaN" 约定不一致（防错原则①——Inf 会继续传播进下游计算）。
            return CapNaN(vol * pAtm * (273.15 / tK));
        }

        /// <summary>Non-finite result → NaN（模块约定：不向 Excel 泄漏 ±Inf）。</summary>
        private static double CapNaN(double v) => double.IsInfinity(v) ? double.NaN : v;

        // review 2026-08-29：DENSITY 下沉（原 L2 零分母守卫写在 UDF lambda，红线① UDF 仅分发）
        internal static double Density(double m, double v) => v == 0 ? double.NaN : m / v;
    }
}
