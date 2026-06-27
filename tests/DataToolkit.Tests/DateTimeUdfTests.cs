using System;
using FormulaLabs.DataToolkit;
using FormulaLabs.Foundation;
using FluentAssertions;
using Xunit;

namespace FormulaLabs.DataToolkit.Tests
{
    public class DateTimeUdfTests
    {
        private static double OA(int y, int m, int d) => new DateTime(y, m, d).ToOADate();

        // ══════════════════════════════════════════════════════════════════
        //  DT.ISOWEEK  (MapOver<double,long>)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void IsoWeek_jan1_mon() => ((long)DateTimeUdf.UDF_DT_ISOW(OA(2024, 1, 1))).Should().Be(1);
        [Fact] public void IsoWeek_jun15() => ((long)DateTimeUdf.UDF_DT_ISOW(OA(2024, 6, 15))).Should().Be(24);
        [Fact] public void IsoWeek_dec31_tue() => ((long)DateTimeUdf.UDF_DT_ISOW(OA(2024, 12, 31))).Should().BeInRange(1, 53);
        [Fact] public void IsoWeek_null() => DateTimeUdf.UDF_DT_ISOW(null!).Should().BeNull();
        [Fact] public void IsoWeek_error() => DateTimeUdf.UDF_DT_ISOW(ExcelError.NA).Should().Be(ExcelError.NA);
        [Fact] public void IsoWeek_array() { var r=(object[])DateTimeUdf.UDF_DT_ISOW(new object[]{OA(2024,1,1),OA(2024,6,15)}); ((long)r[0]).Should().Be(1); ((long)r[1]).Should().Be(24); }

        // ══════════════════════════════════════════════════════════════════
        //  DT.WEEKDAY  (MapOver<double,long> — Sun=1, Mon=2, …, Sat=7)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Weekday_sunday() => ((long)DateTimeUdf.UDF_DT_WKD(OA(2024, 6, 16))).Should().Be(1);
        [Fact] public void Weekday_monday() => ((long)DateTimeUdf.UDF_DT_WKD(OA(2024, 6, 17))).Should().Be(2);
        [Fact] public void Weekday_tuesday() => ((long)DateTimeUdf.UDF_DT_WKD(OA(2024, 6, 18))).Should().Be(3);
        [Fact] public void Weekday_saturday() => ((long)DateTimeUdf.UDF_DT_WKD(OA(2024, 6, 15))).Should().Be(7);
        [Fact] public void Weekday_null() => DateTimeUdf.UDF_DT_WKD(null!).Should().BeNull();
        [Fact] public void Weekday_array() { var r=(object[])DateTimeUdf.UDF_DT_WKD(new object[]{OA(2024,6,16),OA(2024,6,17)}); ((long)r[0]).Should().Be(1); ((long)r[1]).Should().Be(2); }

        // ══════════════════════════════════════════════════════════════════
        //  DT.WEEKDAYISO  (MapOver<double,long> — Mon=1, Sun=7)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void WeekdayIso_monday() => ((long)DateTimeUdf.UDF_DT_WKDISO(OA(2024, 6, 17))).Should().Be(1);
        [Fact] public void WeekdayIso_sunday() => ((long)DateTimeUdf.UDF_DT_WKDISO(OA(2024, 6, 16))).Should().Be(7);
        [Fact] public void WeekdayIso_saturday() => ((long)DateTimeUdf.UDF_DT_WKDISO(OA(2024, 6, 15))).Should().Be(6);
        [Fact] public void WeekdayIso_wednesday() => ((long)DateTimeUdf.UDF_DT_WKDISO(OA(2024, 6, 19))).Should().Be(3);
        [Fact] public void WeekdayIso_null() => DateTimeUdf.UDF_DT_WKDISO(null!).Should().BeNull();
        [Fact] public void WeekdayIso_error() => DateTimeUdf.UDF_DT_WKDISO(ExcelError.NA).Should().Be(ExcelError.NA);
        [Fact] public void WeekdayIso_array() { var r=(object[])DateTimeUdf.UDF_DT_WKDISO(new object[]{OA(2024,6,17),OA(2024,6,16)}); ((long)r[0]).Should().Be(1); ((long)r[1]).Should().Be(7); }

        // ══════════════════════════════════════════════════════════════════
        //  DT.WEEKDAYNAME  (MapOver<double,string>)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void WeekdayName_monday() => ((string)DateTimeUdf.UDF_DT_WKDNAM(OA(2024, 6, 17))).Should().Be("Monday");
        [Fact] public void WeekdayName_sunday() => ((string)DateTimeUdf.UDF_DT_WKDNAM(OA(2024, 6, 16))).Should().Be("Sunday");
        [Fact] public void WeekdayName_friday() => ((string)DateTimeUdf.UDF_DT_WKDNAM(OA(2024, 6, 14))).Should().Be("Friday");
        [Fact] public void WeekdayName_null() => DateTimeUdf.UDF_DT_WKDNAM(null!).Should().BeNull();
        [Fact] public void WeekdayName_error() => DateTimeUdf.UDF_DT_WKDNAM(ExcelError.NA).Should().Be(ExcelError.NA);
        [Fact] public void WeekdayName_array() { var r=(object[])DateTimeUdf.UDF_DT_WKDNAM(new object[]{OA(2024,6,17),OA(2024,6,16)}); ((string)r[0]).Should().Be("Monday"); ((string)r[1]).Should().Be("Sunday"); }

        // ══════════════════════════════════════════════════════════════════
        //  DT.SOW  (MapOver<double,double> — start-of-week OLE date)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void SOW_monday_start() => ((double)DateTimeUdf.UDF_DT_SOW(OA(2024, 6, 15), 1)).Should().Be(OA(2024, 6, 10));
        [Fact] public void SOW_sunday_start() => ((double)DateTimeUdf.UDF_DT_SOW(OA(2024, 6, 15), 0)).Should().Be(OA(2024, 6, 9));
        [Fact] public void SOW_already_start_monday() => ((double)DateTimeUdf.UDF_DT_SOW(OA(2024, 6, 10), 1)).Should().Be(OA(2024, 6, 10));
        [Fact] public void SOW_already_start_sunday() => ((double)DateTimeUdf.UDF_DT_SOW(OA(2024, 6, 9), 0)).Should().Be(OA(2024, 6, 9));
        [Fact] public void SOW_null() => DateTimeUdf.UDF_DT_SOW(null!, 1).Should().BeNull();
        [Fact] public void SOW_error() => DateTimeUdf.UDF_DT_SOW(ExcelError.NA, 1).Should().Be(ExcelError.NA);
        [Fact] public void SOW_array() { var r=(object[])DateTimeUdf.UDF_DT_SOW(new object[]{OA(2024,6,15),OA(2024,6,16)}, 1); ((double)r[0]).Should().Be(OA(2024,6,10)); }

        // ══════════════════════════════════════════════════════════════════
        //  DT.EOW  (MapOver<double,double> — end-of-week OLE date)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void EOW_monday_start() => ((double)DateTimeUdf.UDF_DT_EOW(OA(2024, 6, 15), 1)).Should().Be(OA(2024, 6, 16));
        [Fact] public void EOW_sunday_start() => ((double)DateTimeUdf.UDF_DT_EOW(OA(2024, 6, 15), 0)).Should().Be(OA(2024, 6, 15));
        [Fact] public void EOW_already_end_monday() => ((double)DateTimeUdf.UDF_DT_EOW(OA(2024, 6, 16), 1)).Should().Be(OA(2024, 6, 16));
        [Fact] public void EOW_weekday_midweek() => ((double)DateTimeUdf.UDF_DT_EOW(OA(2024, 6, 19), 0)).Should().Be(OA(2024, 6, 22));
        [Fact] public void EOW_null() => DateTimeUdf.UDF_DT_EOW(null!, 1).Should().BeNull();
        [Fact] public void EOW_error() => DateTimeUdf.UDF_DT_EOW(ExcelError.NA, 1).Should().Be(ExcelError.NA);

        // ══════════════════════════════════════════════════════════════════
        //  DT.SOM  (MapOver<double,double> — start-of-month OLE date)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void SOM_mid_month() => ((double)DateTimeUdf.UDF_DT_SOM(OA(2024, 6, 15))).Should().Be(OA(2024, 6, 1));
        [Fact] public void SOM_jan1() => ((double)DateTimeUdf.UDF_DT_SOM(OA(2024, 1, 1))).Should().Be(OA(2024, 1, 1));
        [Fact] public void SOM_last_day() => ((double)DateTimeUdf.UDF_DT_SOM(OA(2024, 12, 31))).Should().Be(OA(2024, 12, 1));
        [Fact] public void SOM_null() => DateTimeUdf.UDF_DT_SOM(null!).Should().BeNull();
        [Fact] public void SOM_error() => DateTimeUdf.UDF_DT_SOM(ExcelError.NA).Should().Be(ExcelError.NA);
        [Fact] public void SOM_array() { var r=(object[])DateTimeUdf.UDF_DT_SOM(new object[]{OA(2024,6,15),OA(2024,1,15)}); ((double)r[0]).Should().Be(OA(2024,6,1)); ((double)r[1]).Should().Be(OA(2024,1,1)); }

        // ══════════════════════════════════════════════════════════════════
        //  DT.EOM  (MapOver<double,double> — end-of-month OLE date)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void EOM_june() => ((double)DateTimeUdf.UDF_DT_EOM(OA(2024, 6, 15))).Should().Be(OA(2024, 6, 30));
        [Fact] public void EOM_feb_leap() => ((double)DateTimeUdf.UDF_DT_EOM(OA(2024, 2, 10))).Should().Be(OA(2024, 2, 29));
        [Fact] public void EOM_feb_common() => ((double)DateTimeUdf.UDF_DT_EOM(OA(2023, 2, 10))).Should().Be(OA(2023, 2, 28));
        [Fact] public void EOM_december() => ((double)DateTimeUdf.UDF_DT_EOM(OA(2024, 12, 1))).Should().Be(OA(2024, 12, 31));
        [Fact] public void EOM_already_last() => ((double)DateTimeUdf.UDF_DT_EOM(OA(2024, 6, 30))).Should().Be(OA(2024, 6, 30));
        [Fact] public void EOM_null() => DateTimeUdf.UDF_DT_EOM(null!).Should().BeNull();
        [Fact] public void EOM_error() => DateTimeUdf.UDF_DT_EOM(ExcelError.NA).Should().Be(ExcelError.NA);
        [Fact] public void EOM_array() { var r=(object[])DateTimeUdf.UDF_DT_EOM(new object[]{OA(2024,2,1),OA(2023,2,1)}); ((double)r[0]).Should().Be(OA(2024,2,29)); ((double)r[1]).Should().Be(OA(2023,2,28)); }

        // ══════════════════════════════════════════════════════════════════
        //  DT.WOM  (manual — long, week-of-month)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void WOM_monday_anchor() => ((long)DateTimeUdf.UDF_DT_WOM(OA(2024, 6, 15), 1)).Should().Be(2);
        [Fact] public void WOM_sunday_anchor() => ((long)DateTimeUdf.UDF_DT_WOM(OA(2024, 6, 15), 0)).Should().Be(2);
        [Fact] public void WOM_first_monday() => ((long)DateTimeUdf.UDF_DT_WOM(OA(2024, 6, 3), 1)).Should().Be(1);
        [Fact] public void WOM_null_date() => DateTimeUdf.UDF_DT_WOM(null!, 1).Should().BeNull();

        // ══════════════════════════════════════════════════════════════════
        //  DT.DIM  (manual — long, days-in-month)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void DIM_june() => ((long)DateTimeUdf.UDF_DT_DIM(2024, 6)).Should().Be(30);
        [Fact] public void DIM_feb_leap() => ((long)DateTimeUdf.UDF_DT_DIM(2024, 2)).Should().Be(29);
        [Fact] public void DIM_feb_common() => ((long)DateTimeUdf.UDF_DT_DIM(2023, 2)).Should().Be(28);
        [Fact] public void DIM_january() => ((long)DateTimeUdf.UDF_DT_DIM(2024, 1)).Should().Be(31);
        [Fact] public void DIM_july() => ((long)DateTimeUdf.UDF_DT_DIM(2024, 7)).Should().Be(31);
        [Fact] public void DIM_null_year() => DateTimeUdf.UDF_DT_DIM(null!, 2).Should().Be(ExcelError.Value);

        // ══════════════════════════════════════════════════════════════════
        //  DT.AGEYEARS  (manual — long)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void AgeYears_exact_birthday() => ((long)DateTimeUdf.UDF_DT_AGEY(OA(2000, 1, 1), OA(2024, 1, 1))).Should().Be(24);
        [Fact] public void AgeYears_before_birthday() => ((long)DateTimeUdf.UDF_DT_AGEY(OA(2000, 6, 15), OA(2024, 1, 1))).Should().Be(23);
        [Fact] public void AgeYears_after_birthday() => ((long)DateTimeUdf.UDF_DT_AGEY(OA(2000, 1, 1), OA(2024, 6, 15))).Should().Be(24);
        [Fact] public void AgeYears_leap_year_birth() => ((long)DateTimeUdf.UDF_DT_AGEY(OA(2000, 2, 29), OA(2024, 3, 1))).Should().Be(24);
        [Fact] public void AgeYears_same_year() => ((long)DateTimeUdf.UDF_DT_AGEY(OA(2024, 1, 1), OA(2024, 12, 31))).Should().Be(0);
        [Fact] public void AgeYears_null_birth() => DateTimeUdf.UDF_DT_AGEY(null!, OA(2024, 1, 1)).Should().Be(ExcelError.Value);
        [Fact] public void AgeYears_null_ref() { var r=DateTimeUdf.UDF_DT_AGEY(OA(2000,1,1), null!); r.Should().BeOfType<long>().Which.Should().BeGreaterOrEqualTo(0); }

        // ══════════════════════════════════════════════════════════════════
        //  DT.AGEMONTHS  (manual — long)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void AgeMonths_5_months() => ((long)DateTimeUdf.UDF_DT_AGEM(OA(2024, 1, 1), OA(2024, 6, 1))).Should().Be(5);
        [Fact] public void AgeMonths_exact_years() => ((long)DateTimeUdf.UDF_DT_AGEM(OA(2020, 1, 1), OA(2024, 1, 1))).Should().Be(48);
        [Fact] public void AgeMonths_partial_month() => ((long)DateTimeUdf.UDF_DT_AGEM(OA(2024, 1, 15), OA(2024, 2, 1))).Should().Be(0);
        [Fact] public void AgeMonths_null_birth() => DateTimeUdf.UDF_DT_AGEM(null!, OA(2024, 1, 1)).Should().Be(ExcelError.Value);
        [Fact] public void AgeMonths_null_ref() { var r=DateTimeUdf.UDF_DT_AGEM(OA(2024,1,1), null!); r.Should().BeOfType<long>().Which.Should().BeGreaterOrEqualTo(0); }

        // ══════════════════════════════════════════════════════════════════
        //  DT.AGEDAYS  (manual — long)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void AgeDays_nine_days() => ((long)DateTimeUdf.UDF_DT_AGED(OA(2024, 1, 1), OA(2024, 1, 10))).Should().Be(9);
        [Fact] public void AgeDays_same_day() => ((long)DateTimeUdf.UDF_DT_AGED(OA(2024, 6, 15), OA(2024, 6, 15))).Should().Be(0);
        [Fact] public void AgeDays_across_year() => ((long)DateTimeUdf.UDF_DT_AGED(OA(2023, 12, 31), OA(2024, 1, 1))).Should().Be(1);
        [Fact] public void AgeDays_null_birth() => DateTimeUdf.UDF_DT_AGED(null!, OA(2024, 1, 1)).Should().Be(ExcelError.Value);
        [Fact] public void AgeDays_null_ref() { var r=DateTimeUdf.UDF_DT_AGED(OA(2024,1,1), null!); r.Should().BeOfType<long>().Which.Should().BeGreaterOrEqualTo(0); }

        // ══════════════════════════════════════════════════════════════════
        //  DT.ISWE  (MapOver<double,bool> — is-weekend)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void IsWE_saturday() => ((bool)DateTimeUdf.UDF_DT_ISWE(OA(2024, 6, 15))).Should().BeTrue();
        [Fact] public void IsWE_sunday() => ((bool)DateTimeUdf.UDF_DT_ISWE(OA(2024, 6, 16))).Should().BeTrue();
        [Fact] public void IsWE_monday() => ((bool)DateTimeUdf.UDF_DT_ISWE(OA(2024, 6, 17))).Should().BeFalse();
        [Fact] public void IsWE_friday() => ((bool)DateTimeUdf.UDF_DT_ISWE(OA(2024, 6, 14))).Should().BeFalse();
        [Fact] public void IsWE_null() => DateTimeUdf.UDF_DT_ISWE(null!).Should().BeNull();
        [Fact] public void IsWE_array() { var r=(object[])DateTimeUdf.UDF_DT_ISWE(new object[]{OA(2024,6,15),OA(2024,6,17)}); ((bool)r[0]).Should().BeTrue(); ((bool)r[1]).Should().BeFalse(); }

        // ══════════════════════════════════════════════════════════════════
        //  DT.ADDWKD  (manual — double, add-workdays returning OLE date)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void AddWkd_forward_1() => ((double)DateTimeUdf.UDF_DT_AWKD(OA(2024, 6, 14), 1)).Should().Be(OA(2024, 6, 17));
        [Fact] public void AddWkd_backward_1() => ((double)DateTimeUdf.UDF_DT_AWKD(OA(2024, 6, 17), -1)).Should().Be(OA(2024, 6, 14));
        [Fact] public void AddWkd_forward_5() => ((double)DateTimeUdf.UDF_DT_AWKD(OA(2024, 6, 17), 5)).Should().Be(OA(2024, 6, 24));
        [Fact] public void AddWkd_from_sunday() => ((double)DateTimeUdf.UDF_DT_AWKD(OA(2024, 6, 16), 1)).Should().Be(OA(2024, 6, 17));
        [Fact] public void AddWkd_null_start() => DateTimeUdf.UDF_DT_AWKD(null!, 1).Should().Be(ExcelError.Value);

        // ══════════════════════════════════════════════════════════════════
        //  DT.WKDBTWN  (manual — long, workdays-between)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void WkdBtwn_fri_to_mon() => ((long)DateTimeUdf.UDF_DT_WKDB(OA(2024, 6, 14), OA(2024, 6, 17))).Should().Be(1);
        [Fact] public void WkdBtwn_mon_to_fri() => ((long)DateTimeUdf.UDF_DT_WKDB(OA(2024, 6, 17), OA(2024, 6, 21))).Should().Be(4);
        [Fact] public void WkdBtwn_same_day() => ((long)DateTimeUdf.UDF_DT_WKDB(OA(2024, 6, 17), OA(2024, 6, 17))).Should().Be(0);
        [Fact] public void WkdBtwn_across_weekend() => ((long)DateTimeUdf.UDF_DT_WKDB(OA(2024, 6, 14), OA(2024, 6, 24))).Should().Be(6);
        [Fact] public void WkdBtwn_null_start() => DateTimeUdf.UDF_DT_WKDB(null!, OA(2024, 6, 17)).Should().Be(ExcelError.Value);

        // ══════════════════════════════════════════════════════════════════
        //  DT.NEXTWKD  (manual — double, next-workday OLE date)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void NextWkd_friday() => ((double)DateTimeUdf.UDF_DT_NWKD(OA(2024, 6, 14))).Should().Be(OA(2024, 6, 17));
        [Fact] public void NextWkd_saturday() => ((double)DateTimeUdf.UDF_DT_NWKD(OA(2024, 6, 15))).Should().Be(OA(2024, 6, 17));
        [Fact] public void NextWkd_sunday() => ((double)DateTimeUdf.UDF_DT_NWKD(OA(2024, 6, 16))).Should().Be(OA(2024, 6, 17));
        [Fact] public void NextWkd_null() => DateTimeUdf.UDF_DT_NWKD(null!).Should().BeNull();

        // ══════════════════════════════════════════════════════════════════
        //  DT.EASTER  (manual — double, Easter Sunday OLE date)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Easter_2024() => ((double)DateTimeUdf.UDF_DT_EASTER(2024)).Should().Be(OA(2024, 3, 31));
        [Fact] public void Easter_2025() => ((double)DateTimeUdf.UDF_DT_EASTER(2025)).Should().Be(OA(2025, 4, 20));
        [Fact] public void Easter_2023() => ((double)DateTimeUdf.UDF_DT_EASTER(2023)).Should().Be(OA(2023, 4, 9));
        [Fact] public void Easter_returns_date() => ((double)DateTimeUdf.UDF_DT_EASTER(2024)).Should().BeGreaterThan(0);
        [Fact] public void Easter_null_year() => DateTimeUdf.UDF_DT_EASTER(null!).Should().BeNull();

        // ══════════════════════════════════════════════════════════════════
        //  DT.QUARTER  (MapOver<double,long>)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Quarter_Q1() => ((long)DateTimeUdf.UDF_DT_Q(OA(2024, 3, 15))).Should().Be(1);
        [Fact] public void Quarter_Q2() => ((long)DateTimeUdf.UDF_DT_Q(OA(2024, 6, 15))).Should().Be(2);
        [Fact] public void Quarter_Q3() => ((long)DateTimeUdf.UDF_DT_Q(OA(2024, 9, 1))).Should().Be(3);
        [Fact] public void Quarter_Q4() => ((long)DateTimeUdf.UDF_DT_Q(OA(2024, 12, 1))).Should().Be(4);
        [Fact] public void Quarter_null() => DateTimeUdf.UDF_DT_Q(null!).Should().BeNull();
        [Fact] public void Quarter_array() { var r=(object[])DateTimeUdf.UDF_DT_Q(new object[]{OA(2024,1,1),OA(2024,7,1)}); ((long)r[0]).Should().Be(1); ((long)r[1]).Should().Be(3); }

        // ══════════════════════════════════════════════════════════════════
        //  DT.SEMESTER  (MapOver<double,long>)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Semester_H1() => ((long)DateTimeUdf.UDF_DT_SEM(OA(2024, 6, 15))).Should().Be(1);
        [Fact] public void Semester_H2() => ((long)DateTimeUdf.UDF_DT_SEM(OA(2024, 7, 1))).Should().Be(2);
        [Fact] public void Semester_jan1() => ((long)DateTimeUdf.UDF_DT_SEM(OA(2024, 1, 1))).Should().Be(1);
        [Fact] public void Semester_dec31() => ((long)DateTimeUdf.UDF_DT_SEM(OA(2024, 12, 31))).Should().Be(2);
        [Fact] public void Semester_null() => DateTimeUdf.UDF_DT_SEM(null!).Should().BeNull();
        [Fact] public void Semester_array() { var r=(object[])DateTimeUdf.UDF_DT_SEM(new object[]{OA(2024,3,1),OA(2024,8,1)}); ((long)r[0]).Should().Be(1); ((long)r[1]).Should().Be(2); }

        // ══════════════════════════════════════════════════════════════════
        //  DT.DOY  (MapOver<double,long> — day-of-year)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void DOY_jun15_leap() => ((long)DateTimeUdf.UDF_DT_DOY(OA(2024, 6, 15))).Should().Be(167);
        [Fact] public void DOY_jan1() => ((long)DateTimeUdf.UDF_DT_DOY(OA(2024, 1, 1))).Should().Be(1);
        [Fact] public void DOY_dec31() => ((long)DateTimeUdf.UDF_DT_DOY(OA(2024, 12, 31))).Should().Be(366);
        [Fact] public void DOY_mar1_non_leap() => ((long)DateTimeUdf.UDF_DT_DOY(OA(2023, 3, 1))).Should().Be(60);
        [Fact] public void DOY_null() => DateTimeUdf.UDF_DT_DOY(null!).Should().BeNull();
        [Fact] public void DOY_error() => DateTimeUdf.UDF_DT_DOY(ExcelError.NA).Should().Be(ExcelError.NA);
        [Fact] public void DOY_array() { var r=(object[])DateTimeUdf.UDF_DT_DOY(new object[]{OA(2024,1,1),OA(2024,12,31)}); ((long)r[0]).Should().Be(1); ((long)r[1]).Should().Be(366); }

        // ══════════════════════════════════════════════════════════════════
        //  DT.ISLEAP  (manual — object/bool)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void IsLeap_2024() => ((bool)DateTimeUdf.UDF_DT_ILEAP(2024)).Should().BeTrue();
        [Fact] public void IsLeap_2023() => ((bool)DateTimeUdf.UDF_DT_ILEAP(2023)).Should().BeFalse();
        [Fact] public void IsLeap_2000() => ((bool)DateTimeUdf.UDF_DT_ILEAP(2000)).Should().BeTrue();
        [Fact] public void IsLeap_1900() => ((bool)DateTimeUdf.UDF_DT_ILEAP(1900)).Should().BeFalse();
        [Fact] public void IsLeap_null() => DateTimeUdf.UDF_DT_ILEAP(null!).Should().BeNull();

        // ══════════════════════════════════════════════════════════════════
        //  DT.UNIXTS  (manual — double, Unix timestamp)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void UnixTs_2024() => ((double)DateTimeUdf.UDF_DT_UXTS(OA(2024, 6, 15))).Should().BeGreaterThan(0);
        [Fact] public void UnixTs_before_epoch() => ((double)DateTimeUdf.UDF_DT_UXTS(OA(1960, 1, 1))).Should().BeNegative();
        [Fact] public void UnixTs_null() => DateTimeUdf.UDF_DT_UXTS(null!).Should().BeNull();

        // ══════════════════════════════════════════════════════════════════
        //  DT.FROMUNIX  (manual — double, from-Unix-timestamp OLE date)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void FromUnix_zero() => ((double)DateTimeUdf.UDF_DT_FUXTS(0)).Should().BeGreaterThan(0);
        [Fact] public void FromUnix_large() => ((double)DateTimeUdf.UDF_DT_FUXTS(1000000000)).Should().BeGreaterThan(0);
        [Fact] public void FromUnix_roundtrip() { var ts=DateTimeUdf.UDF_DT_UXTS(OA(2024,6,15)); var back=DateTimeUdf.UDF_DT_FUXTS(ts); ((double)back).Should().Be(OA(2024,6,15)); }
        [Fact] public void FromUnix_null() => DateTimeUdf.UDF_DT_FUXTS(null!).Should().BeNull();

        // ══════════════════════════════════════════════════════════════════
        //  DT.DATEDIFF  (manual — long, date-diff)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void DateDiff_days() => ((long)DateTimeUdf.UDF_DT_DDIFF("D", OA(2024, 1, 1), OA(2024, 1, 10))).Should().Be(9);
        [Fact] public void DateDiff_months() => ((long)DateTimeUdf.UDF_DT_DDIFF("M", OA(2024, 1, 1), OA(2024, 6, 1))).Should().Be(5);
        [Fact] public void DateDiff_years() => ((long)DateTimeUdf.UDF_DT_DDIFF("Y", OA(2020, 1, 1), OA(2024, 1, 1))).Should().Be(4);
        [Fact] public void DateDiff_same_date() => ((long)DateTimeUdf.UDF_DT_DDIFF("D", OA(2024, 6, 15), OA(2024, 6, 15))).Should().Be(0);
        [Fact] public void DateDiff_invalid_unit() => DateTimeUdf.UDF_DT_DDIFF("INVALID", OA(2024, 1, 1), OA(2024, 1, 10)).Should().Be(ExcelError.Value);

        // ══════════════════════════════════════════════════════════════════
        // Edge-case / corrected tests
        // ══════════════════════════════════════════════════════════════════

        [Fact] public void UnixTs_epoch() { var ts=(double)DateTimeUdf.UDF_DT_UXTS(OA(1970,1,1)); Math.Abs(ts).Should().BeLessThan(86400); }
        [Fact] public void DateDiff_null_unit() => DateTimeUdf.UDF_DT_DDIFF(null!, OA(2024,1,1), OA(2024,1,10)).Should().Be(ExcelError.Value);
        [Fact] public void AddWkd_zero() => ((double)DateTimeUdf.UDF_DT_AWKD(OA(2024,6,17), 0)).Should().Be(OA(2024,6,17));
    }
}
