using System;
using ExcelVbaLibraries.Analytics;
using ExcelVbaLibraries.Foundation;
using FluentAssertions;
using Xunit;

namespace ExcelVbaLibraries.Analytics.Tests
{
    public class PhyChemUdfTests
    {
        // ── MOLWT ────────────────────────────────────────────────────────────
        [Fact] public void MolWt_H2O()
        {
            var r = PhyChemUdf.UDF_PC_MOLWT("H2O");
            ((double)r).Should().BeApproximately(18.016, 0.01);
        }

        [Fact] public void MolWt_CO2()
        {
            var r = PhyChemUdf.UDF_PC_MOLWT("CO2");
            ((double)r).Should().BeApproximately(44.01, 0.01);
        }

        [Fact] public void MolWt_CaOH2_paren()
        {
            var r = PhyChemUdf.UDF_PC_MOLWT("Ca(OH)2");
            ((double)r).Should().BeApproximately(74.094, 0.01);
        }

        [Fact] public void MolWt_invalid()
        {
            var r = PhyChemUdf.UDF_PC_MOLWT("Xx2");
            ((double)r).Should().Be(double.NaN);
        }

        // ── TEMP ─────────────────────────────────────────────────────────────
        [Fact] public void Temp_C_to_F()
        {
            var r = PhyChemUdf.UDF_PC_TEMP(0.0, "C", "F");
            ((double)r).Should().BeApproximately(32.0, 0.01);
        }

        [Fact] public void Temp_F_to_C()
        {
            var r = PhyChemUdf.UDF_PC_TEMP(212.0, "F", "C");
            ((double)r).Should().BeApproximately(100.0, 0.01);
        }

        [Fact] public void Temp_C_to_K()
        {
            var r = PhyChemUdf.UDF_PC_TEMP(0.0, "C", "K");
            ((double)r).Should().BeApproximately(273.15, 0.01);
        }

        [Fact] public void Temp_bad_unit()
        {
            var r = PhyChemUdf.UDF_PC_TEMP(0.0, "X", "C");
            ((double)r).Should().Be(double.NaN);
        }

        // ── PRESS ────────────────────────────────────────────────────────────
        [Fact] public void Press_atm_to_kPa()
        {
            var r = PhyChemUdf.UDF_PC_PRESS(1.0, "atm", "kPa");
            ((double)r).Should().BeApproximately(101.325, 0.01);
        }

        [Fact] public void Press_atm_to_Pa()
        {
            var r = PhyChemUdf.UDF_PC_PRESS(1.0, "atm", "Pa");
            ((double)r).Should().BeApproximately(101325.0, 1.0);
        }

        [Fact] public void Press_psi_to_atm()
        {
            var r = PhyChemUdf.UDF_PC_PRESS(14.6959, "psi", "atm");
            ((double)r).Should().BeApproximately(1.0, 0.01);
        }

        [Fact] public void Press_bad_unit()
        {
            var r = PhyChemUdf.UDF_PC_PRESS(1.0, "X", "atm");
            ((double)r).Should().Be(double.NaN);
        }

        // ── VOL ──────────────────────────────────────────────────────────────
        [Fact] public void Vol_L_to_mL()
        {
            var r = PhyChemUdf.UDF_PC_VOL(1.0, "L", "mL");
            ((double)r).Should().BeApproximately(1000.0, 1e-10);
        }

        [Fact] public void Vol_gal_to_L()
        {
            var r = PhyChemUdf.UDF_PC_VOL(1.0, "gal", "L");
            ((double)r).Should().BeApproximately(3.78541, 0.01);
        }

        [Fact] public void Vol_L_to_gal()
        {
            var r = PhyChemUdf.UDF_PC_VOL(3.78541, "L", "gal");
            ((double)r).Should().BeApproximately(1.0, 0.01);
        }

        [Fact] public void Vol_bad_unit()
        {
            var r = PhyChemUdf.UDF_PC_VOL(1.0, "X", "L");
            ((double)r).Should().Be(double.NaN);
        }

        // ── MASS ─────────────────────────────────────────────────────────────
        [Fact] public void Mass_kg_to_g()
        {
            var r = PhyChemUdf.UDF_PC_MASS(1.0, "kg", "g");
            ((double)r).Should().BeApproximately(1000.0, 1e-10);
        }

        [Fact] public void Mass_lb_to_kg()
        {
            var r = PhyChemUdf.UDF_PC_MASS(1.0, "lb", "kg");
            ((double)r).Should().BeApproximately(0.453592, 0.01);
        }

        [Fact] public void Mass_kg_to_lb()
        {
            var r = PhyChemUdf.UDF_PC_MASS(1.0, "kg", "lb");
            ((double)r).Should().BeApproximately(2.20462, 0.01);
        }

        [Fact] public void Mass_bad_unit()
        {
            var r = PhyChemUdf.UDF_PC_MASS(1.0, "X", "kg");
            ((double)r).Should().Be(double.NaN);
        }

        // ── C_TO_F ───────────────────────────────────────────────────────────
        [Fact] public void CtoF_freezing()
        {
            var r = PhyChemUdf.UDF_PC_CTOF(0.0);
            ((double)r).Should().BeApproximately(32.0, 1e-10);
        }

        [Fact] public void CtoF_boiling()
        {
            var r = PhyChemUdf.UDF_PC_CTOF(100.0);
            ((double)r).Should().BeApproximately(212.0, 1e-10);
        }

        [Fact] public void CtoF_negative()
        {
            var r = PhyChemUdf.UDF_PC_CTOF(-40.0);
            ((double)r).Should().BeApproximately(-40.0, 1e-10);
        }

        // ── F_TO_C ───────────────────────────────────────────────────────────
        [Fact] public void FtoC_freezing()
        {
            var r = PhyChemUdf.UDF_PC_FTOC(32.0);
            ((double)r).Should().BeApproximately(0.0, 1e-10);
        }

        [Fact] public void FtoC_boiling()
        {
            var r = PhyChemUdf.UDF_PC_FTOC(212.0);
            ((double)r).Should().BeApproximately(100.0, 1e-10);
        }

        [Fact] public void FtoC_body_temp()
        {
            var r = PhyChemUdf.UDF_PC_FTOC(98.6);
            ((double)r).Should().BeApproximately(37.0, 0.1);
        }

        // ── KG_TO_LB ─────────────────────────────────────────────────────────
        [Fact] public void KgToLb_one()
        {
            var r = PhyChemUdf.UDF_PC_KG2LB(1.0);
            ((double)r).Should().BeApproximately(2.20462, 1e-6);
        }

        [Fact] public void KgToLb_zero()
        {
            var r = PhyChemUdf.UDF_PC_KG2LB(0.0);
            ((double)r).Should().Be(0.0);
        }

        // ── LB_TO_KG ─────────────────────────────────────────────────────────
        [Fact] public void LbToKg_one()
        {
            var r = PhyChemUdf.UDF_PC_LB2KG(2.20462);
            ((double)r).Should().BeApproximately(1.0, 1e-6);
        }

        [Fact] public void LbToKg_zero()
        {
            var r = PhyChemUdf.UDF_PC_LB2KG(0.0);
            ((double)r).Should().Be(0.0);
        }

        // ── L_TO_GAL ─────────────────────────────────────────────────────────
        [Fact] public void LToGal_one()
        {
            var r = PhyChemUdf.UDF_PC_L2GAL(1.0);
            ((double)r).Should().BeApproximately(0.264172, 1e-6);
        }

        [Fact] public void LToGal_one_gal_equivalent()
        {
            var r = PhyChemUdf.UDF_PC_L2GAL(3.78541);
            ((double)r).Should().BeApproximately(1.0, 1e-4);
        }

        [Fact] public void LToGal_zero()
        {
            var r = PhyChemUdf.UDF_PC_L2GAL(0.0);
            ((double)r).Should().Be(0.0);
        }

        // ── GAL_TO_L ─────────────────────────────────────────────────────────
        [Fact] public void GalToL_one()
        {
            var r = PhyChemUdf.UDF_PC_GAL2L(1.0);
            ((double)r).Should().BeApproximately(3.78541, 0.01);
        }

        [Fact] public void GalToL_zero()
        {
            var r = PhyChemUdf.UDF_PC_GAL2L(0.0);
            ((double)r).Should().Be(0.0);
        }

        // ── ATM_TO_PSI ───────────────────────────────────────────────────────
        [Fact] public void AtmToPsi_one()
        {
            var r = PhyChemUdf.UDF_PC_ATM2PSI(1.0);
            ((double)r).Should().BeApproximately(14.6959, 1e-4);
        }

        [Fact] public void AtmToPsi_zero()
        {
            var r = PhyChemUdf.UDF_PC_ATM2PSI(0.0);
            ((double)r).Should().Be(0.0);
        }

        // ── PSI_TO_ATM ───────────────────────────────────────────────────────
        [Fact] public void PsiToAtm_one()
        {
            var r = PhyChemUdf.UDF_PC_PSI2ATM(14.6959);
            ((double)r).Should().BeApproximately(1.0, 1e-4);
        }

        [Fact] public void PsiToAtm_zero()
        {
            var r = PhyChemUdf.UDF_PC_PSI2ATM(0.0);
            ((double)r).Should().Be(0.0);
        }

        // ── IDEALGAS ─────────────────────────────────────────────────────────
        [Fact] public void IdealGas_solve_P()
        {
            var r = PhyChemUdf.UDF_PC_GAS(null!, 22.4, 1.0, 273.15);
            ((double)r).Should().BeApproximately(1.0, 0.2);
        }

        [Fact] public void IdealGas_solve_V()
        {
            var r = PhyChemUdf.UDF_PC_GAS(1.0, null!, 1.0, 273.15);
            ((double)r).Should().BeApproximately(22.414, 0.5);
        }

        [Fact] public void IdealGas_solve_n()
        {
            var r = PhyChemUdf.UDF_PC_GAS(1.0, 22.4, null!, 273.15);
            ((double)r).Should().BeApproximately(1.0, 0.1);
        }

        [Fact] public void IdealGas_solve_T()
        {
            var r = PhyChemUdf.UDF_PC_GAS(1.0, 22.414, 1.0, null!);
            ((double)r).Should().BeApproximately(273.15, 1.0);
        }

        [Fact] public void IdealGas_all_null()
        {
            var r = PhyChemUdf.UDF_PC_GAS(null!, null!, null!, null!);
            ((double)r).Should().Be(double.NaN);
        }

        [Fact] public void IdealGas_two_missing()
        {
            var r = PhyChemUdf.UDF_PC_GAS(null!, null!, 1.0, 273.15);
            ((double)r).Should().Be(double.NaN);
        }

        // ── GASSTP ───────────────────────────────────────────────────────────
        [Fact] public void GasSTP_at_standard_conditions()
        {
            var r = PhyChemUdf.UDF_PC_STP(22.4, 0.0, 1.0);
            ((double)r).Should().BeApproximately(22.4, 0.1);
        }

        [Fact] public void GasSTP_double_volume()
        {
            var r = PhyChemUdf.UDF_PC_STP(44.8, 0.0, 1.0);
            ((double)r).Should().BeApproximately(44.8, 0.1);
        }

        [Fact] public void GasSTP_at_room_temp()
        {
            var r = PhyChemUdf.UDF_PC_STP(22.4, 25.0, 1.0);
            double expected = 22.4 * (273.15 / 298.15);
            ((double)r).Should().BeApproximately(expected, 0.1);
        }

        // ── DENSITY ──────────────────────────────────────────────────────────
        [Fact] public void Density_scalar()
        {
            var r = PhyChemUdf.UDF_PC_DEN(10.0, 2.0);
            ((double)r).Should().BeApproximately(5.0, 1e-10);
        }

        [Fact] public void Density_zero_mass()
        {
            var r = PhyChemUdf.UDF_PC_DEN(0.0, 5.0);
            ((double)r).Should().Be(0.0);
        }

        [Fact] public void Density_equal_mass_vol()
        {
            var r = PhyChemUdf.UDF_PC_DEN(7.5, 7.5);
            ((double)r).Should().BeApproximately(1.0, 1e-10);
        }
    }
}
