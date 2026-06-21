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
        [Fact] public void DayOfYear() => DateTimeCore.DayOfYear(D).Should().Be(166);
        [Fact] public void DateDiff_days() => DateTimeCore.DateDiff("D",new(2025,1,1),new(2025,1,10)).Should().Be(9);
        [Fact] public void DateDiff_months() => DateTimeCore.DateDiff("M",new(2025,1,15),new(2025,6,15)).Should().Be(5);
        [Fact] public void IsoYear_2024_01_01() => DateTimeCore.IsoYear(new(2024,1,1)).Should().Be(2024);
        [Fact] public void IsoYear_2024_12_30() => DateTimeCore.IsoYear(new(2024,12,30)).Should().Be(2025);
        [Fact] public void WeekdayName_saturday() => DateTimeCore.WeekdayName(new(2024,6,15)).Should().Be("Saturday");
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
    }
}
