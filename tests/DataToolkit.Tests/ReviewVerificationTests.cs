// 评审复核复现集（DataToolkit 侧）。命名规则与 Analytics 侧一致。
using System;
using ExcelFormulaLabs.DataToolkit;
using FluentAssertions;
using Xunit;

namespace ExcelFormulaLabs.DataToolkit.Tests
{
    public class ReviewVerificationTests
    {
        // ─────────────────────────────────────────────────────────────
        // D1 — DT.UNIX 的时区依赖
        // 链路：DateTimeUdf.D() → DateTime.FromOADate(v) → Kind=Unspecified
        //       → DateTimeCore.UnixTimestamp 调 d.ToUniversalTime()
        // .NET 对 Kind=Unspecified 的 ToUniversalTime() 按**本机时区**当作 Local 解释。
        // 期望：同一墙上时刻（Excel 序列号本身无时区语义）必须给出同一 Unix 时间戳。
        // 本机为 UTC+8，未修复时两者相差 28800 秒。
        // ─────────────────────────────────────────────────────────────

        [Fact]
        public void D1_UnixTimestamp_mustNotDependOnDateTimeKind()
        {
            var unspecified = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var utc = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            double fromUnspecified = DateTimeCore.UnixTimestamp(unspecified);
            double fromUtc = DateTimeCore.UnixTimestamp(utc);
            fromUnspecified.Should().Be(fromUtc,
                "Excel 序列号无时区语义；同一墙上时刻不应因 DateTimeKind 不同而得到不同时间戳");
        }

        [Fact] // oracle: 2024-01-01T00:00:00Z == 1704067200（公开 Unix 时间戳）
        public void D1b_UnixTimestamp_ofEpochAlignedDate_shouldMatchKnownValue()
            => DateTimeCore.UnixTimestamp(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                .Should().Be(1704067200.0);

        // UDF 入口实际得到的 Kind 证据（FromOADate 的返回值）
        [Fact]
        public void D1c_FromOADate_yields_Unspecified_kind()
            => DateTime.FromOADate(45292.0).Kind.Should().Be(DateTimeKind.Unspecified,
                "这是 D1 的根因所在：UDF 传入的 DateTime 无时区标记");

        // Kind=Local 必须被**尊重**（不得被 SpecifyKind 重贴标签）：该回归由 CrossVal
        // 反向抓出——manifest 入参 "2024-01-01T00:00:00Z" 经 Dispatcher 转 Local 后
        // 曾被误当 UTC，UnixTimestamp 偏移 +8h。
        [Fact]
        public void D1d_UnixTimestamp_respectsLocalKind()
        {
            var localNoon = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Local);
            var utcEquivalent = localNoon.ToUniversalTime();
            DateTimeCore.UnixTimestamp(localNoon)
                .Should().Be(DateTimeCore.UnixTimestamp(utcEquivalent),
                    "Kind=Local 表示一个确定的时间点，换算结果必须与等价的 Utc 时刻一致");
        }
    }
}
