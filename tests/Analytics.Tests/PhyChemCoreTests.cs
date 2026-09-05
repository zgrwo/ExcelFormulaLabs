using System;
using ExcelFormulaLabs.Analytics;
using FluentAssertions;
using Xunit;

namespace ExcelFormulaLabs.Analytics.Tests
{
    public class PhyChemCoreTests
    {
        [Fact] public void H2O() => PhyChemCore.MolecularWeight("H2O").Should().BeApproximately(18.016,0.01);
        [Fact] public void CO2() => PhyChemCore.MolecularWeight("CO2").Should().BeApproximately(44.01,0.01);
        [Fact] public void CaOH2_paren() => PhyChemCore.MolecularWeight("Ca(OH)2").Should().BeApproximately(74.094,0.01);
        [Fact] public void Fe4FeCN6_3_bracket() => PhyChemCore.MolecularWeight("Fe4[Fe(CN)6]3").Should().BeApproximately(859.239, 1e-3);
        // P3 (review): exact golden values — the bracket/paren parsing correctness currently
        // depends on fragile regex intermediate states; exact assertions guard regressions
        // (a >500 sanity check would let a wrong 679 pass).
        [Fact] public void H2SO4_exact() => PhyChemCore.MolecularWeight("H2SO4").Should().BeApproximately(98.078, 1e-3);
        [Fact] public void FeCN6_bracket_exact() => PhyChemCore.MolecularWeight("[Fe(CN)6]").Should().BeApproximately(211.953, 1e-3);
        [Fact] public void Unknown_element_NaN() => PhyChemCore.MolecularWeight("Xx2").Should().Be(double.NaN);
        [Fact] public void C_to_F() => PhyChemCore.ConvertTemperature(0,"C","F").Should().BeApproximately(32,1e-10);
        [Fact] public void F_to_C() => PhyChemCore.ConvertTemperature(212,"F","C").Should().BeApproximately(100,1e-4);
        [Fact] public void C_to_K() => PhyChemCore.ConvertTemperature(0,"C","K").Should().BeApproximately(273.15,1e-10);
        [Fact] public void Atm_to_kPa() => PhyChemCore.ConvertPressure(1,"atm","kPa").Should().BeApproximately(101.325,1e-6);
        [Fact] public void L_to_mL() => PhyChemCore.ConvertVolume(1,"L","mL").Should().BeApproximately(1000,1e-10);
        [Fact] public void Kg_to_g() => PhyChemCore.ConvertMass(1,"kg","g").Should().BeApproximately(1000,1e-10);
        [Fact] public void IdealGasLaw_P() => PhyChemCore.IdealGasLaw(v:22.4,n:1,t:273.15).Should().BeApproximately(1.0,0.02);
        [Fact] public void IdealGasLaw_NaN_if_2_missing() => PhyChemCore.IdealGasLaw(p:1,v:22.4).Should().Be(double.NaN);
        [Fact] public void GasToSTP() => PhyChemCore.GasToSTP(22.4,0,1,"C","atm").Should().BeApproximately(22.4,0.01);
        [Fact] public void MolecularWeight_hydrate() => PhyChemCore.MolecularWeight("CuSO4.5H2O").Should().BeApproximately(249.69, 0.1);
        [Fact] public void MolecularWeight_overflow_count_throws() { var act = ()=>PhyChemCore.MolecularWeight("C9999999999H2"); act.Should().Throw<ArgumentException>().WithMessage("*9999999999*"); }
        // review 2026-08-29（发行前 max level 复审）：水合物系数原用裸 int 逐位累积，unchecked 下
        // 超 int.MaxValue 静默回绕为负数 → 错误的分子量。现与 ParseCount 对齐显式抛错。
        [Fact] public void MolecularWeight_hydrate_coefficient_overflow_throws() { var act = () => PhyChemCore.MolecularWeight("H2O.10000000000H2O"); act.Should().Throw<ArgumentException>().WithMessage("*coefficient*"); }
        [Fact] public void GasToSTP_no_vUnit() => PhyChemCore.GasToSTP(22.4, 0, 1).Should().BeApproximately(22.4, 0.01);
        [Fact] public void GasToSTP_invalid_unit_returns_NaN() => PhyChemCore.GasToSTP(22.4, 0, 1, "XX").Should().Be(double.NaN);

        // =====================================================================
        // EDGE CASE & INPUT VALIDATION TESTS
        // (systematic coverage — unknown units, empty/null, extreme values)
        // =====================================================================

        [Fact] public void ConvertTemperature_unknown_from_unit() =>
            double.IsNaN(PhyChemCore.ConvertTemperature(100, "XYZ", "C")).Should().BeTrue();

        [Fact] public void ConvertTemperature_unknown_to_unit() =>
            double.IsNaN(PhyChemCore.ConvertTemperature(0, "C", "XYZ")).Should().BeTrue();

        [Fact] public void ConvertPressure_unknown_unit() =>
            double.IsNaN(PhyChemCore.ConvertPressure(1, "PA", "XYZ")).Should().BeTrue();

        [Fact] public void ConvertVolume_unknown_unit() =>
            double.IsNaN(PhyChemCore.ConvertVolume(1, "GAL", "XYZ")).Should().BeTrue();

        [Fact] public void ConvertMass_unknown_unit() =>
            double.IsNaN(PhyChemCore.ConvertMass(1, "LB", "XYZ")).Should().BeTrue();

        [Fact] public void ConvertVolume_FT3_to_L()
        {
            // FT3 → L: 1 ft³ ≈ 28.3168 L
            PhyChemCore.ConvertVolume(1, "FT3", "L").Should().BeApproximately(28.3168, 0.01);
        }

        [Fact] public void ConvertVolume_QT_to_L()
        {
            // QT → L: 1 quart ≈ 0.946353 L
            PhyChemCore.ConvertVolume(1, "QT", "L").Should().BeApproximately(0.946353, 1e-5);
        }

        [Fact] public void ConvertMass_TON_to_KG()
        {
            PhyChemCore.ConvertMass(1, "TON", "KG").Should().BeApproximately(1000, 0.01);
        }

        [Fact] public void MolecularWeight_empty_string() =>
            double.IsNaN(PhyChemCore.MolecularWeight("")).Should().BeTrue();

        [Fact] public void MolecularWeight_null() =>
            double.IsNaN(PhyChemCore.MolecularWeight(null!)).Should().BeTrue();

        [Fact] public void MolecularWeight_whitespace_only() =>
            double.IsNaN(PhyChemCore.MolecularWeight("   ")).Should().BeTrue();

        [Fact] public void MolarMass_hydrate_with_coefficient_only() =>
            // Hydrate dot-syntax with coefficient but no parent compound → NaN
            // ".10H2O" splits into [""] + ["10H2O"]; empty parent → NaN immediately
            double.IsNaN(PhyChemCore.MolecularWeight(".10H2O")).Should().BeTrue();

        [Fact] public void ConvertTemperature_fahrenheit_to_kelvin() =>
            // 32°F = 273.15 K (freezing point of water)
            PhyChemCore.ConvertTemperature(32, "F", "K").Should().BeApproximately(273.15, 1e-10);

        [Fact] public void ConvertPressure_bar_to_pascal() =>
            // 1 bar = 100,000 Pa (exact by definition in this library)
            PhyChemCore.ConvertPressure(1, "BAR", "PA").Should().BeApproximately(100000, 1e-10);

        [Fact] public void ConvertVolume_liter_to_cubic_meter() =>
            // 1000 L = 1 m³
            PhyChemCore.ConvertVolume(1000, "L", "M3").Should().BeApproximately(1, 1e-10);

        [Fact] public void ConvertMass_gram_to_kilogram() =>
            // 1000 g = 1 kg
            PhyChemCore.ConvertMass(1000, "G", "KG").Should().BeApproximately(1, 1e-10);

        [Fact] public void IdealGasLaw_solve_for_T()
        {
            // PV = nRT → T = PV/(nR) = 1*22.4/(1*0.082057) ≈ 272.98 K
            // (close to STP 273.15K; R=0.082057 is approximate)
            PhyChemCore.IdealGasLaw(p: 1, v: 22.4, n: 1)
                .Should().BeApproximately(272.98, 0.05);
        }

        [Fact] public void IdealGasLaw_extra_unknowns() =>
            // Two missing parameters (n and t) → NaN (too many unknowns)
            double.IsNaN(PhyChemCore.IdealGasLaw(p: 1, v: 22.4)).Should().BeTrue();

        [Fact] public void IdealGasLaw_too_few_params()
        {
            double.IsNaN(PhyChemCore.IdealGasLaw(p: 1)).Should().BeTrue();
        }

        [Fact] public void IdealGasLaw_too_many_params()
        {
            double.IsNaN(PhyChemCore.IdealGasLaw(p: 1, v: 22.4, n: 1, t: 273.15)).Should().BeTrue();
        }

        [Fact] public void IdealGasLaw_solve_for_n()
        {
            // PV = nRT → n = PV/(RT) = 1*22.4/(0.082057*273.15) ≈ 1.0
            PhyChemCore.IdealGasLaw(p: 1, v: 22.4, t: 273.15)
                .Should().BeApproximately(1.0, 0.01);
        }

        [Fact] public void GasToSTP_zero_temperature_K()
        {
            PhyChemCore.GasToSTP(22.4, 0, 1, "K", "atm").Should().Be(double.NaN);
        }

        // ═══════════════ NaN/Inf guard tests (防错原则1) ═══════════════

        [Fact] public void ConvertTemperature_NaN_returns_NaN() => PhyChemCore.ConvertTemperature(double.NaN, "C", "F").Should().Be(double.NaN);
        [Fact] public void ConvertTemperature_Inf_returns_NaN() => PhyChemCore.ConvertTemperature(double.PositiveInfinity, "C", "F").Should().Be(double.NaN);
        [Fact] public void ConvertPressure_NaN_returns_NaN() => PhyChemCore.ConvertPressure(double.NaN, "atm", "Pa").Should().Be(double.NaN);
        [Fact] public void ConvertPressure_Inf_returns_NaN() => PhyChemCore.ConvertPressure(double.PositiveInfinity, "atm", "Pa").Should().Be(double.NaN);
        [Fact] public void ConvertVolume_NaN_returns_NaN() => PhyChemCore.ConvertVolume(double.NaN, "L", "mL").Should().Be(double.NaN);
        [Fact] public void ConvertVolume_Inf_returns_NaN() => PhyChemCore.ConvertVolume(double.PositiveInfinity, "L", "mL").Should().Be(double.NaN);
        [Fact] public void ConvertMass_NaN_returns_NaN() => PhyChemCore.ConvertMass(double.NaN, "kg", "g").Should().Be(double.NaN);
        [Fact] public void ConvertMass_Inf_returns_NaN() => PhyChemCore.ConvertMass(double.PositiveInfinity, "kg", "g").Should().Be(double.NaN);
        [Fact] public void IdealGasLaw_NaN_pressure_returns_NaN() => PhyChemCore.IdealGasLaw(p: double.NaN, v: 22.4, n: 1).Should().Be(double.NaN);
        [Fact] public void IdealGasLaw_Infinity_volume_returns_NaN() => PhyChemCore.IdealGasLaw(p: 1, v: double.PositiveInfinity, n: 1).Should().Be(double.NaN);

        // ── Release-review regression guards ────────────────────────────────
        [Fact] public void GasToSTP_negative_pressure_returns_NaN() => PhyChemCore.GasToSTP(10.0, 25.0, -1.0).Should().Be(double.NaN);
        [Fact] public void GasToSTP_zero_pressure_returns_NaN() => PhyChemCore.GasToSTP(10.0, 25.0, 0.0).Should().Be(double.NaN);

        // ── review-2026-09-05（N06）：残留字符拒收一致化 ──
        [Fact] public void MOLWT_trailing_garbage_returns_NaN()
        {
            // "H2Oxyz"：元素匹配后残留 "xyz" 原实现静默按已匹配部分计算（=18.015），
            // 而全小写 "h2o"（零匹配）却返回 NaN——拒收不一致。整串消费校验后 → NaN。
            double.IsNaN(PhyChemCore.MolecularWeight("H2Oxyz")).Should().BeTrue();
        }

        [Fact] public void MOLWT_lowercase_returns_NaN()
        {
            // "h2o"：无大写元素记号 → NaN（既有行为，与残留拒收口径一致化后保持）。
            double.IsNaN(PhyChemCore.MolecularWeight("h2o")).Should().BeTrue();
        }

        [Fact] public void MOLWT_H2O_exact_value()
        {
            // 按现有 AtomicWeights 精度：H=1.008, O=15.999 → 2×1.008+15.999 = 18.015。
            // 拒收规则收紧（N06）后正常式不受影响。
            PhyChemCore.MolecularWeight("H2O").Should().BeApproximately(18.015, 1e-3);
        }

        [Fact] public void MOLWT_CO2_exact_value()
        {
            // C=12.011, O=15.999 → 12.011+2×15.999 = 44.009（N06 收紧后正常式不变）。
            PhyChemCore.MolecularWeight("CO2").Should().BeApproximately(44.009, 1e-3);
        }

        // ── review-2026-09-05（N08）：Convert* 输出封顶 + 绝对零守卫 ──
        [Fact] public void ConvertTemperature_overflow_returns_NaN()
        {
            // 有限大输入 1e308 ℃ → F：×1.8 溢出 ±Inf → NaN 封顶（原 Inf 直通）。
            double.IsNaN(PhyChemCore.ConvertTemperature(1e308, "C", "F")).Should().BeTrue();
        }

        [Fact] public void ConvertTemperature_below_absolute_zero_returns_NaN()
        {
            // -300℃ → -26.85 K：K 温标 ≤ 0 物理无意义（对齐 GASSTP 的 tK<=0 拒收）→ NaN。
            double.IsNaN(PhyChemCore.ConvertTemperature(-300, "C", "K")).Should().BeTrue();
        }

        [Fact] public void ConvertTemperature_zero_kelvin_returns_NaN()
        {
            // 0 K 为绝对零边界（守卫判据 k ≤ 0）→ NaN。
            double.IsNaN(PhyChemCore.ConvertTemperature(0, "K", "C")).Should().BeTrue();
        }

        [Fact] public void ConvertPressure_overflow_returns_NaN()
        {
            // 1e308 atm → Pa：×101325 溢出 Inf → NaN 封顶。
            double.IsNaN(PhyChemCore.ConvertPressure(1e308, "ATM", "PA")).Should().BeTrue();
        }

        [Fact] public void ConvertVolume_overflow_returns_NaN()
        {
            // 1e308 L → mL：×1000 溢出 Inf → NaN 封顶。
            double.IsNaN(PhyChemCore.ConvertVolume(1e308, "L", "ML")).Should().BeTrue();
        }

        [Fact] public void ConvertMass_overflow_returns_NaN()
        {
            // 1e308 kg → g：×1000 溢出 Inf → NaN 封顶。
            double.IsNaN(PhyChemCore.ConvertMass(1e308, "KG", "G")).Should().BeTrue();
        }

        [Fact] public void ConvertTemperature_large_but_finite_K_returns_finite()
        {
            // 1e308 ℃ → K：k=1e308 有限且 > 0 → 结果有限（不得误封顶）。
            PhyChemCore.ConvertTemperature(1e308, "C", "K").Should().BeApproximately(1e308, 1e300);
        }
    }
}