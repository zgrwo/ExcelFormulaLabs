using System;
using System.Linq;
using ExcelFormulaLabs.DataToolkit;
using FluentAssertions;
using Xunit;

namespace ExcelFormulaLabs.DataToolkit.Tests
{
    /// <summary>
    /// 安全回归子集（CI：<c>dotnet test --filter "Category=Security"</c>）。
    /// 本类补此前无显式回归的攻击面：XML XXE/DTD/深度炸弹、SQL 注入（恶意列名/文本值/多语句）。
    /// 既有安全测试以 [Trait] 纳入同一子集：沙箱越界（FileSystemCoreTests）、
    /// ReDoS 与预算（RegexCoreTests/RegexUdfTests）、CSV 公式注入（RangeExportCoreTests）、
    /// SQL 校验（SqlCoreTests）、原生 DLL 完整性（NativeDllStoreTests）。
    /// </summary>
    [Trait("Category", "Security")]
    public class SecurityTests
    {
        // ─────────────────────────────────────────────────────────────
        // XML：XXE / DTD / 深度炸弹
        // ─────────────────────────────────────────────────────────────

        [Fact]
        public void Xml_doctype_is_prohibited()
        {
            var xml = "<!DOCTYPE foo [<!ENTITY xxe \"boom\">]><foo>&xxe;</foo>";
            JsonXmlCore.XmlValidate(xml).Should().BeFalse();
        }

        [Fact]
        public void Xml_external_entity_is_not_resolved()
        {
            var xml = "<!DOCTYPE foo [<!ENTITY xxe SYSTEM \"file:///C:/Windows/win.ini\">]><foo>&xxe;</foo>";
            JsonXmlCore.XmlXPath(xml, "//foo").Should().BeEmpty();
            JsonXmlCore.XmlToTable(xml).Should().BeNull();
        }

        [Fact]
        public void Xml_depth_bomb_is_rejected()
        {
            var deep = string.Concat(Enumerable.Repeat("<a>", 1100))
                     + string.Concat(Enumerable.Repeat("</a>", 1100));
            Action act = () => JsonXmlCore.XmlToTable(deep);
            act.Should().Throw<ArgumentException>().WithMessage("*depth*");
        }

        [Fact]
        public void Xml_benign_input_still_parses()
        {
            JsonXmlCore.XmlValidate("<r><a>1</a></r>").Should().BeTrue();
        }

        // ─────────────────────────────────────────────────────────────
        // SQL：注入
        // ─────────────────────────────────────────────────────────────

        [Fact]
        public void Sql_malicious_column_name_cannot_escape_identifier()
        {
            var data = new object[,]
            {
                { "x\"; DROP TABLE data; --", "v" },
                { "1", "ok" }
            };
            var r = SqlCore.SqlQuery(data, "SELECT * FROM data");
            r.Should().NotBeNull();
            r!.GetLength(0).Should().Be(2);
        }

        [Fact]
        public void Sql_text_value_is_parameterized_not_executed()
        {
            var payload = "'; DROP TABLE data; --";
            var data = new object[,] { { "name", "note" }, { "Alice", payload } };
            var r = SqlCore.SqlQuery(data, "SELECT note FROM data WHERE name = 'Alice'");
            r![1, 0].Should().Be(payload);
        }

        [Fact]
        public void Sql_multi_statement_is_rejected()
        {
            var data = new object[,] { { "a" }, { "1" } };
            // 用双 SELECT 触发分号拦截（含 DROP 的变体会先被禁用关键字黑名单拦截，
            // 消息不同——黑名单路径由 SqlCoreTests 覆盖）。
            Action act = () => SqlCore.SqlQuery(data, "SELECT * FROM data; SELECT 1");
            act.Should().Throw<ArgumentException>().WithMessage("*Semicolon*");
        }
    }
}
