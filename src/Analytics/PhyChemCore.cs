using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ExcelVbaLibraries.Foundation;

namespace ExcelVbaLibraries.Analytics
{
    /// <summary>
    /// Physical chemistry: molecular weight, unit conversions, gas standard state.
    /// Ported from PhyChemUtils.bas.
    /// </summary>
    internal static class PhyChemCore
    {
        private static readonly Dictionary<string, double> AtomicWeights = new(StringComparer.Ordinal)
        {
            ["H"]=1.008,["He"]=4.003,["Li"]=6.941,["Be"]=9.012,["B"]=10.81,["C"]=12.01,
            ["N"]=14.01,["O"]=16.00,["F"]=19.00,["Ne"]=20.18,["Na"]=22.99,["Mg"]=24.31,
            ["Al"]=26.98,["Si"]=28.09,["P"]=30.97,["S"]=32.07,["Cl"]=35.45,["Ar"]=39.95,
            ["K"]=39.10,["Ca"]=40.08,["Sc"]=44.96,["Ti"]=47.87,["V"]=50.94,["Cr"]=52.00,
            ["Mn"]=54.94,["Fe"]=55.85,["Co"]=58.93,["Ni"]=58.69,["Cu"]=63.55,["Zn"]=65.39,
            ["Ga"]=69.72,["Ge"]=72.61,["As"]=74.92,["Se"]=78.96,["Br"]=79.90,["Kr"]=83.80,
            ["Rb"]=85.47,["Sr"]=87.62,["Y"]=88.91,["Zr"]=91.22,["Nb"]=92.91,["Mo"]=95.94,
            ["Tc"]=98.00,["Ru"]=101.10,["Rh"]=102.90,["Pd"]=106.40,["Ag"]=107.90,["Cd"]=112.40,
            ["In"]=114.80,["Sn"]=118.70,["Sb"]=121.80,["Te"]=127.60,["I"]=126.90,["Xe"]=131.30,
            ["Cs"]=132.90,["Ba"]=137.30,["La"]=138.90,["Ce"]=140.10,["Pr"]=140.90,["Nd"]=144.20,
            ["Pm"]=145.00,["Sm"]=150.40,["Eu"]=151.90,["Gd"]=157.30,["Tb"]=158.90,["Dy"]=162.50,
            ["Ho"]=164.90,["Er"]=167.30,["Tm"]=168.90,["Yb"]=173.00,["Lu"]=175.00,["Hf"]=178.49,
            ["Ta"]=180.95,["W"]=183.85,["Re"]=186.21,["Os"]=190.23,["Ir"]=192.22,["Pt"]=195.08,
            ["Au"]=196.97,["Hg"]=201.97,["Tl"]=204.38,["Pb"]=207.20,["Bi"]=208.98,["Po"]=209.00,
            ["At"]=210.00,["Rn"]=222.00,["Fr"]=223.00,["Ra"]=226.03,["Ac"]=227.03,["Th"]=232.04,
            ["Pa"]=231.04,["U"]=238.03,["Np"]=237.05,["Pu"]=244.06,["Am"]=243.06,["Cm"]=247.07,
            ["Bk"]=247.07,["Cf"]=251.08,["Es"]=252.08,["Fm"]=257.10,
        };

        private static readonly Regex ElemRx = new(@"([A-Z][a-z]?)(\d*)", RegexOptions.Compiled, TimeSpan.FromSeconds(5));
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
                    int coeff = 0, j = 0;
                    while (j < p.Length && char.IsDigit(p[j])) { coeff = coeff * 10 + (p[j] - '0'); j++; }
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
            double mw = 0;
            foreach (Match m in ElemRx.Matches(formula))
            {
                string elem = m.Groups[1].Value;
                int cnt = ParseCount(m.Groups[2].Value);
                if (AtomicWeights.TryGetValue(elem, out double w))
                    mw += w * cnt;
                else return double.NaN;
            }
            return mw;
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
            if (long.TryParse(s, out long big))
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
            if (double.IsNaN(k)) return double.NaN;
            return to.ToUpperInvariant() switch
            {
                "C" or "CELSIUS" => k - 273.15,
                "F" or "FAHRENHEIT" => (k - 273.15) * 9.0 / 5.0 + 32,
                "K" or "KELVIN" => k,
                _ => double.NaN,
            };
        }

        internal static double ConvertPressure(double v, string from, string to)
        {
            if (double.IsNaN(v) || double.IsInfinity(v)) return double.NaN;
            double pa = from.ToUpperInvariant() switch
            {
                "ATM" => v * 101325, "PA" or "PASCAL" => v, "KPA" => v * 1000,
                "BAR" => v * 100000, "PSI" => v * 6894.76, "MMHG" or "TORR" => v * 133.322,
                _ => double.NaN,
            };
            if (double.IsNaN(pa)) return double.NaN;
            return to.ToUpperInvariant() switch
            {
                "ATM" => pa / 101325, "PA" or "PASCAL" => pa, "KPA" => pa / 1000,
                "BAR" => pa / 100000, "PSI" => pa / 6894.76, "MMHG" or "TORR" => pa / 133.322,
                _ => double.NaN,
            };
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
            return to.ToUpperInvariant() switch
            {
                "L" or "LITER" => l, "ML" => l * 1000, "M3" => l / 1000,
                "GAL" or "GALLON" => l / 3.78541, "QT" or "QUART" => l / 0.946353,
                "FT3" => l / 28.3168, _ => double.NaN,
            };
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
            return to.ToUpperInvariant() switch
            {
                "KG" => g / 1000, "G" or "GRAM" => g, "MG" => g * 1000,
                "LB" or "LBS" => g / 453.59237, "OZ" => g / 28.3495, "TON" => g / 1e6,
                _ => double.NaN,
            };
        }

        internal static double IdealGasLaw(double? p = null, double? v = null,
            double? n = null, double? t = null, double r = 0.082057)
        {
            // Reject NaN/Inf in supplied parameters (防错原则1)
            if (p.HasValue && (double.IsNaN(p.Value) || double.IsInfinity(p.Value))) return double.NaN;
            if (v.HasValue && (double.IsNaN(v.Value) || double.IsInfinity(v.Value))) return double.NaN;
            if (n.HasValue && (double.IsNaN(n.Value) || double.IsInfinity(n.Value))) return double.NaN;
            if (t.HasValue && (double.IsNaN(t.Value) || double.IsInfinity(t.Value))) return double.NaN;
            if (double.IsNaN(r) || double.IsInfinity(r)) return double.NaN;
            int missing = (p.HasValue?0:1)+(v.HasValue?0:1)+(n.HasValue?0:1)+(t.HasValue?0:1);
            if (missing != 1) return double.NaN;
            // Each branch handles the case where exactly one parameter is missing.
            // The missing count above guarantees the other three have values,
            // but we use explicit .HasValue checks (not !) for defence-in-depth (防错原则1).
            if (!p.HasValue)
            {
                if (!v.HasValue || !n.HasValue || !t.HasValue) return double.NaN;
                return v.Value == 0 ? double.NaN : n.Value * r * t.Value / v.Value;
            }
            if (!v.HasValue)
            {
                if (!p.HasValue || !n.HasValue || !t.HasValue) return double.NaN;
                return p.Value == 0 ? double.NaN : n.Value * r * t.Value / p.Value;
            }
            if (!n.HasValue)
            {
                if (!p.HasValue || !v.HasValue || !t.HasValue) return double.NaN;
                return t.Value == 0 ? double.NaN : p.Value * v.Value / (r * t.Value);
            }
            if (!p.HasValue || !v.HasValue || !n.HasValue) return double.NaN;
            return n.Value == 0 ? double.NaN : p.Value * v.Value / (n.Value * r);
        }

        internal static double GasToSTP(double vol, double temp, double press,
            string tUnit = "C", string pUnit = "atm")
        {
            double tK = ConvertTemperature(temp, tUnit, "K");
            double pAtm = ConvertPressure(press, pUnit, "atm");
            if (double.IsNaN(tK) || double.IsNaN(pAtm) || tK == 0)
                return double.NaN;
            return vol * pAtm * (273.15 / tK);
        }
    }
}
