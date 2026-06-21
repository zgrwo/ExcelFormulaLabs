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
    }
}
