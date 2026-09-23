using System;
using System.Collections.Generic;
using ExcelFormulaLabs.Foundation;
using FluentAssertions;
using Xunit;

namespace ExcelFormulaLabs.Foundation.Tests;

public class DictCreateTests
{
    [Fact] public void Case_insensitive_default()
    {
        var dict = DictOperations.Create();
        dict["Key"] = 1;
        dict["key"] = 2;
        dict.Count.Should().Be(1);
    }

    [Fact] public void Case_sensitive_when_requested()
    {
        var dict = DictOperations.Create(StringComparison.Ordinal);
        dict["Key"] = 1;
        dict["key"] = 2;
        dict.Count.Should().Be(2);
    }
}

public class FromKeysTests
{
    [Fact] public void Basic_from_keys()
    {
        var dict = DictOperations.FromKeys(new object[] { "a", "b", "c" }, 0);
        dict.Count.Should().Be(3);
        dict["a"].Should().Be(0);
    }

    [Fact] public void Skips_invalid_keys()
    {
        var dict = DictOperations.FromKeys(new object[] { "a", ExcelError.Value, null!, "b" });
        dict.Count.Should().Be(2);
    }

    [Fact] public void Duplicate_first_wins()
    {
        DictOperations.FromKeys(new object[] { "dup", "dup" }, "first")["dup"].Should().Be("first");
    }

    [Fact] public void Empty_array_returns_empty()
        => DictOperations.FromKeys(Array.Empty<object>()).Count.Should().Be(0);

    [Fact] public void All_invalid_keys_returns_empty()
        => DictOperations.FromKeys(new object[] { ExcelError.Value, null!, ExcelError.NA }).Count.Should().Be(0);

    // 文档契约「Object 键跳过」——两个无意义对象经 Convert.ToString 会塌缩为同一
    // "System.Object" 键（静默合并），必须跳过。
    [Fact] public void Custom_objects_are_skipped_per_doc_contract()
    {
        var dict = DictOperations.FromKeys(new object[] { new object(), new object(), "keep" });
        dict.Count.Should().Be(1);
        dict.ContainsKey("keep").Should().BeTrue();
    }
}

public class ToArrayTests
{
    [Fact] public void Exports_2D()
    {
        var dict = new Dictionary<string, object> { ["k1"] = "v1", ["k2"] = 42 };
        var result = DictOperations.ToArray(dict);
        result.Should().NotBeNull();
        result!.GetLength(0).Should().Be(2);
        result.GetLength(1).Should().Be(2);
    }

    [Fact] public void Null_returns_null() => DictOperations.ToArray(null).Should().BeNull();
    [Fact] public void Empty_returns_null() => DictOperations.ToArray(new Dictionary<string, object>()).Should().BeNull();

    [Fact] public void Single_entry_returns_1x2_array()
    {
        var dict = new Dictionary<string, object> { ["key"] = "value" };
        var result = DictOperations.ToArray(dict);
        result.Should().NotBeNull();
        result!.GetLength(0).Should().Be(1);
        result.GetLength(1).Should().Be(2);
    }
}

public class MergeTests
{
    [Fact] public void No_overwrite_first_wins()
    {
        var a = new Dictionary<string, object> { ["key"] = "A" };
        var b = new Dictionary<string, object> { ["key"] = "B", ["extra"] = "B2" };
        var merged = DictOperations.Merge(a, b);
        merged["key"].Should().Be("A");
        merged["extra"].Should().Be("B2");
    }

    [Fact] public void Overwrite_second_wins()
    {
        var a = new Dictionary<string, object> { ["key"] = "A" };
        var b = new Dictionary<string, object> { ["key"] = "B" };
        DictOperations.Merge(a, b, overwrite: true)["key"].Should().Be("B");
    }

    [Fact] public void Both_null_returns_empty()
        => DictOperations.Merge(null, null).Count.Should().Be(0);

    [Fact] public void Merge_explicit_no_overwrite_first_wins()
    {
        var a = new Dictionary<string, object> { ["key"] = "first" };
        var b = new Dictionary<string, object> { ["key"] = "second" };
        DictOperations.Merge(a, b, overwrite: false)["key"].Should().Be("first");
    }

    [Fact] public void Merge_one_null_dict_preserves_other()
    {
        var dict = new Dictionary<string, object> { ["a"] = 1, ["b"] = 2 };
        var merged = DictOperations.Merge(dict, null);
        merged.Count.Should().Be(2);
        merged["a"].Should().Be(1);
        merged["b"].Should().Be(2);
    }
}

public class DictKeyTypeTests
{
    [Fact] public void FromKeys_distinguishes_typed_keys()
    {
        var dt = new DateTime(2026, 1, 2, 3, 4, 5, 678);
        var dict = DictOperations.FromKeys(new object[] { 1, 1.5f, 2.5m, 3L, (short)4, (byte)5, true, dt });
        dict.Count.Should().Be(8);
        dict.ContainsKey("1").Should().BeTrue();
        dict.ContainsKey("1.5").Should().BeTrue();
        dict.ContainsKey("2.5").Should().BeTrue();
        dict.ContainsKey("3").Should().BeTrue();
        dict.ContainsKey("4").Should().BeTrue();
        dict.ContainsKey("5").Should().BeTrue();
        dict.ContainsKey("TRUE").Should().BeTrue();
        dict.ContainsKey("2026-01-02 03:04:05.6780000").Should().BeTrue();
    }

    [Fact] public void Create_supports_every_comparison_mode()
    {
        foreach (StringComparison mode in new[]
        {
            StringComparison.Ordinal, StringComparison.OrdinalIgnoreCase,
            StringComparison.CurrentCulture, StringComparison.CurrentCultureIgnoreCase,
            StringComparison.InvariantCulture, StringComparison.InvariantCultureIgnoreCase,
            (StringComparison)999,
        })
        {
            DictOperations.FromKeys(new object[] { "Key" }, 1, mode)
                .ContainsKey("Key").Should().BeTrue($"mode={mode}");
        }
        DictOperations.FromKeys(new object[] { "Key" }, 1, StringComparison.Ordinal)
            .ContainsKey("key").Should().BeFalse();
        DictOperations.FromKeys(new object[] { "Key" }, 1, StringComparison.OrdinalIgnoreCase)
            .ContainsKey("key").Should().BeTrue();
        DictOperations.FromKeys(new object[] { "Key" }, 1, (StringComparison)999)
            .ContainsKey("key").Should().BeTrue();
    }
}
