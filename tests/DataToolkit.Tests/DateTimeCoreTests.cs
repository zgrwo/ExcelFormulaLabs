using System;
using ExcelVbaLibraries.DataToolkit;
using FluentAssertions;
using Xunit;

namespace ExcelVbaLibraries.DataToolkit.Tests
{
    public class DateTimeCoreTests
    {
        private static readonly DateTime D = new(2025,6,15);
        [Fact] public void IsoWeekNum() => DateTimeCore.IsoWeekNum(D).Should().Be(24);
        [Fact] public void Weekday_sunday_is_1() => DateTimeCore.Weekday(D).Should().Be(1);
        [Fact] public void WeekdayISO_sunday_is_7() => DateTimeCore.WeekdayISO(D).Should().Be(7);
        [Fact] public void StartOfWeek() => DateTimeCore.StartOfWeek(D).DayOfWeek.Should().Be(DayOfWeek.Monday);
        [Fact] public void StartOfMonth() => DateTimeCore.StartOfMonth(D).Day.Should().Be(1);
        [Fact] public void EndOfMonth() => DateTimeCore.EndOfMonth(D).Day.Should().Be(30);
        [Fact] public void Quarter() => DateTimeCore.Quarter(D).Should().Be(2);
        [Fact] public void AgeYears() => DateTimeCore.AgeYears(new DateTime(2000,6,15),D).Should().Be(25);
        [Fact] public void IsWeekend_sunday() => DateTimeCore.IsWeekend(D).Should().BeTrue();
        [Fact] public void AddWorkdays_fri_to_wed() => DateTimeCore.AddWorkdays(new(2025,6,13),3).Day.Should().Be(18);
        [Fact] public void WorkdaysBetween() => DateTimeCore.WorkdaysBetween(new(2025,6,13),new(2025,6,17)).Should().Be(2);
        [Fact] public void Easter_2025() { var e=DateTimeCore.Easter(2025); e.Month.Should().Be(4); e.Day.Should().Be(20); }
        [Fact] public void IsLeapYear() { DateTimeCore.IsLeapYear(2024).Should().BeTrue(); DateTimeCore.IsLeapYear(2025).Should().BeFalse(); }
        [Fact] public void IsLeapYear_century() { DateTimeCore.IsLeapYear(1900).Should().BeFalse(); DateTimeCore.IsLeapYear(2000).Should().BeTrue(); }
        [Fact] public void Easter_2100() { var e=DateTimeCore.Easter(2100); e.Month.Should().Be(3); e.Day.Should().Be(28); }
        [Fact] public void DayOfYear() => DateTimeCore.DayOfYear(D).Should().Be(166);
        [Fact] public void DateDiff_days() => DateTimeCore.DateDiff("D",new(2025,1,1),new(2025,1,10)).Should().Be(9);
        [Fact] public void DateDiff_months() => DateTimeCore.DateDiff("M",new(2025,1,15),new(2025,6,15)).Should().Be(5);
        [Fact] public void IsoYear_2024_01_01() => DateTimeCore.IsoYear(new(2024,1,1)).Should().Be(2024);
        [Fact] public void IsoYear_2024_12_30() => DateTimeCore.IsoYear(new(2024,12,30)).Should().Be(2025);
        [Fact] public void WeekdayName_saturday() => DateTimeCore.WeekdayName(new(2024,6,15)).Should().Be("Saturday");
        [Fact] public void WeekdayName_monday() => DateTimeCore.WeekdayName(new(2024,6,17)).Should().Be("Monday");
        [Fact] public void WeekdayName_sunday() => DateTimeCore.WeekdayName(new(2024,6,16)).Should().Be("Sunday");
        [Fact] public void EndOfWeek_saturday_to_sunday() => DateTimeCore.EndOfWeek(new(2024,6,15)).DayOfWeek.Should().Be(DayOfWeek.Sunday);
        [Fact] public void WeekOfMonth() => DateTimeCore.WeekOfMonth(new(2024,6,15)).Should().Be(2);
        [Fact] public void AgeMonths() => DateTimeCore.AgeMonths(new(2020,1,15),new(2024,6,15)).Should().Be(53);
        [Fact] public void AgeDays() => DateTimeCore.AgeDays(new(2020,1,15),new(2024,6,15)).Should().Be(1613);
        [Fact] public void NextWorkday_friday() => DateTimeCore.NextWorkday(new(2024,6,14)).DayOfWeek.Should().Be(DayOfWeek.Monday);
        [Fact] public void NextWorkday_saturday() => DateTimeCore.NextWorkday(new(2024,6,15)).DayOfWeek.Should().Be(DayOfWeek.Monday);
        [Fact] public void Semester_january() => DateTimeCore.Semester(new(2024,1,1)).Should().Be(1);
        [Fact] public void Semester_july() => DateTimeCore.Semester(new(2024,7,1)).Should().Be(2);
        [Fact] public void DaysInMonth_2024_02() => DateTimeCore.DaysInMonth(2024,2).Should().Be(29);
        [Fact] public void DaysInMonth_2023_02() => DateTimeCore.DaysInMonth(2023,2).Should().Be(28);
        [Fact] public void UnixTimestamp() => DateTimeCore.UnixTimestamp(new DateTime(2024,1,1,0,0,0,DateTimeKind.Utc)).Should().Be(1704067200.0);
        [Fact] public void FromUnixTimestamp_roundtrip() { var ts=1704067200.0; var r=DateTimeCore.FromUnixTimestamp(ts); DateTimeCore.UnixTimestamp(r).Should().BeApproximately(ts,1.0); }
        [Fact] public void IsoWeekNum_edge_2024_12_31() => DateTimeCore.IsoWeekNum(new(2024,12,31)).Should().Be(1);
        // Easter dates cross-validated with US Naval Observatory / Python easter lib
        [Fact] public void Easter_2024() => DateTimeCore.Easter(2024).Should().Be(new DateTime(2024,3,31));
        [Fact] public void Easter_2023() => DateTimeCore.Easter(2023).Should().Be(new DateTime(2023,4,9));
        [Fact] public void Easter_2000() => DateTimeCore.Easter(2000).Should().Be(new DateTime(2000,4,23));

        // =====================================================================
        // EDGE CASE & DAY-OF-WEEK VARIATIONS
        // (systematic coverage for DayOfWeek parameterized methods)
        // =====================================================================

        [Fact] public void StartOfWeek_sunday_start()
        {
            // Wednesday with Sunday as start → previous Sunday
            var d = new DateTime(2025, 6, 18);  // Wednesday
            var r = DateTimeCore.StartOfWeek(d, DayOfWeek.Sunday);
            r.DayOfWeek.Should().Be(DayOfWeek.Sunday);
            r.Day.Should().Be(15);
        }

        [Fact] public void StartOfWeek_already_start_day()
        {
            // Monday is already the start (default) → same day
            var d = new DateTime(2025, 6, 16);  // Monday
            DateTimeCore.StartOfWeek(d).Should().Be(d.Date);
        }

        [Fact] public void EndOfWeek_sunday_start()
        {
            var d = new DateTime(2025, 6, 18);  // Wednesday
            var r = DateTimeCore.EndOfWeek(d, DayOfWeek.Sunday);
            r.DayOfWeek.Should().Be(DayOfWeek.Saturday);
            r.Day.Should().Be(21);
        }

        [Fact] public void EndOfWeek_friday_start()
        {
            // Wednesday with Friday as start → end = Thursday
            var d = new DateTime(2025, 6, 18);  // Wednesday
            var r = DateTimeCore.EndOfWeek(d, DayOfWeek.Friday);
            r.DayOfWeek.Should().Be(DayOfWeek.Thursday);
            r.Day.Should().Be(19);
        }

        [Fact] public void WeekOfMonth_sunday_start()
        {
            var d = new DateTime(2025, 6, 15);  // Sunday
            var r = DateTimeCore.WeekOfMonth(d, DayOfWeek.Sunday);
            r.Should().Be(3);
        }

        [Fact] public void WeekOfMonth_first_day_is_start()
        {
            // June 1, 2025 = Sunday. With Sunday start, it's week 1
            var d = new DateTime(2025, 6, 1);
            DateTimeCore.WeekOfMonth(d, DayOfWeek.Sunday).Should().Be(1);
        }

        [Fact] public void AddWorkdays_negative()
        {
            // -3 workdays before Monday June 16 → previous Wednesday June 11
            var r = DateTimeCore.AddWorkdays(new DateTime(2025, 6, 16), -3);
            r.DayOfWeek.Should().Be(DayOfWeek.Wednesday);
            r.Day.Should().Be(11);
        }

        [Fact] public void AddWorkdays_zero()
        {
            var d = new DateTime(2025, 6, 15);  // Sunday
            DateTimeCore.AddWorkdays(d, 0).Should().Be(d);
        }

        [Fact] public void IsWeekend_weekday()
        {
            DateTimeCore.IsWeekend(new DateTime(2025, 6, 18)).Should().BeFalse();  // Wednesday
            DateTimeCore.IsWeekend(new DateTime(2025, 6, 16)).Should().BeFalse();  // Monday
        }

        [Fact] public void IsWeekend_saturday()
        {
            DateTimeCore.IsWeekend(new DateTime(2025, 6, 21)).Should().BeTrue();
        }

        [Fact] public void WorkdaysBetween_same_day()
        {
            var d = new DateTime(2025, 6, 15);
            DateTimeCore.WorkdaysBetween(d, d).Should().Be(0);
        }

        [Fact] public void WorkdaysBetween_reversed()
        {
            // Start after end → negative result
            var a = new DateTime(2025, 6, 20);
            var b = new DateTime(2025, 6, 15);
            DateTimeCore.WorkdaysBetween(a, b).Should().BeLessOrEqualTo(0);
        }

        [Fact] public void DateDiff_year()
        {
            DateTimeCore.DateDiff("Y", new DateTime(2020, 6, 15), new DateTime(2025, 6, 15)).Should().Be(5);
        }

        [Fact] public void DateDiff_year_birthday_not_yet()
        {
            // Same year diff but day-of-year not yet reached
            DateTimeCore.DateDiff("Y", new DateTime(2020, 12, 31), new DateTime(2025, 1, 1)).Should().Be(4);
        }

        [Fact] public void DateDiff_weeks()
        {
            DateTimeCore.DateDiff("W", new DateTime(2025, 1, 1), new DateTime(2025, 1, 15)).Should().Be(2);  // 14 days = 2 weeks
        }
        [Fact] public void DateDiff_unknown_unit()
        {
            var act = () => DateTimeCore.DateDiff("HOUR", new DateTime(2025, 1, 1), new DateTime(2025, 1, 10));
            act.Should().Throw<ArgumentException>().WithMessage("*HOUR*");
        }

        [Fact] public void NextWorkday_thursday()
        {
            var r = DateTimeCore.NextWorkday(new DateTime(2025, 6, 19));  // Thursday → Friday
            r.DayOfWeek.Should().Be(DayOfWeek.Friday);
        }

        [Fact] public void AgeYears_birthday_today()
        {
            var birth = new DateTime(2000, 6, 15);
            var refDate = new DateTime(2025, 6, 15);
            DateTimeCore.AgeYears(birth, refDate).Should().Be(25);
        }

        [Fact] public void AgeYears_birthday_not_yet()
        {
            var birth = new DateTime(2000, 12, 31);
            var refDate = new DateTime(2025, 1, 1);
            DateTimeCore.AgeYears(birth, refDate).Should().Be(24);
        }

        [Fact] public void Quarter_december()
        {
            DateTimeCore.Quarter(new DateTime(2024, 12, 1)).Should().Be(4);
        }

        [Fact] public void Semester_june()
        {
            DateTimeCore.Semester(new DateTime(2024, 6, 30)).Should().Be(1);
        }

        // =====================================================================
        // AssertValidDate — MinValue rejection on ALL date methods
        // =====================================================================
        private static void AssertMinValueThrows(Action act)
            => act.Should().Throw<ArgumentException>().WithMessage("*Invalid date*");

        [Fact] public void IsoWeekNum_MinValue_throws() => AssertMinValueThrows(() => DateTimeCore.IsoWeekNum(DateTime.MinValue));
        [Fact] public void IsoYear_MinValue_throws() => AssertMinValueThrows(() => DateTimeCore.IsoYear(DateTime.MinValue));
        [Fact] public void Weekday_MinValue_throws() => AssertMinValueThrows(() => DateTimeCore.Weekday(DateTime.MinValue));
        [Fact] public void WeekdayISO_MinValue_throws() => AssertMinValueThrows(() => DateTimeCore.WeekdayISO(DateTime.MinValue));
        [Fact] public void WeekdayName_MinValue_throws() => AssertMinValueThrows(() => DateTimeCore.WeekdayName(DateTime.MinValue));
        [Fact] public void IsWeekend_MinValue_throws() => AssertMinValueThrows(() => DateTimeCore.IsWeekend(DateTime.MinValue));
        [Fact] public void Quarter_MinValue_throws() => AssertMinValueThrows(() => DateTimeCore.Quarter(DateTime.MinValue));
        [Fact] public void Semester_MinValue_throws() => AssertMinValueThrows(() => DateTimeCore.Semester(DateTime.MinValue));
        [Fact] public void DayOfYear_MinValue_throws() => AssertMinValueThrows(() => DateTimeCore.DayOfYear(DateTime.MinValue));
        [Fact] public void UnixTimestamp_MinValue_throws() => AssertMinValueThrows(() => DateTimeCore.UnixTimestamp(DateTime.MinValue));
        // Regression: methods that already had guards before this change
        [Fact] public void StartOfWeek_MinValue_throws() => AssertMinValueThrows(() => DateTimeCore.StartOfWeek(DateTime.MinValue));
        [Fact] public void EndOfMonth_MinValue_throws() => AssertMinValueThrows(() => DateTimeCore.EndOfMonth(DateTime.MinValue));
    }
}
