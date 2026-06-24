using ExcelVbaLibraries.Analytics;
using FluentAssertions;
using Xunit;

namespace ExcelVbaLibraries.Analytics.Tests
{
    public class PhyChemCoreTests
    {
        [Fact] public void H2O() => PhyChemCore.MolecularWeight("H2O").Should().BeApproximately(18.016,0.01);
        [Fact] public void CO2() => PhyChemCore.MolecularWeight("CO2").Should().BeApproximately(44.01,0.01);
        [Fact] public void CaOH2_paren() => PhyChemCore.MolecularWeight("Ca(OH)2").Should().BeApproximately(74.094,0.01);
        [Fact] public void Fe4FeCN6_3_bracket() => PhyChemCore.MolecularWeight("Fe4[Fe(CN)6]3").Should().BeGreaterThan(500);
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
            // t=0K → division by zero → now guarded to NaN (P1 fix)
            PhyChemCore.GasToSTP(22.4, 0, 1, "K", "atm").Should().Be(double.NaN);
        }
    }
}
