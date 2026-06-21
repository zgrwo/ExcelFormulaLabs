using System.Collections.Generic;
using ExcelVbaLibraries.DataToolkit;
using ExcelVbaLibraries.Foundation;
using FluentAssertions;
using Xunit;

namespace ExcelVbaLibraries.DataToolkit.Tests
{
    public class JsonXmlUdfTests
    {
        [Fact] public void JsonParse_object() => JsonXmlUdf.UDF_JSON_PARSE("{\"a\":1}").Should().BeAssignableTo<Dictionary<string,object?>>()
            .Which["a"].Should().Be(1L);
        [Fact] public void JsonParse_array() { var r=(object[])JsonXmlUdf.UDF_JSON_PARSE("[1,2,3]"); r.Should().Equal(1L,2L,3L); }
        [Fact] public void JsonParse_string() => ((string)JsonXmlUdf.UDF_JSON_PARSE("\"hello\"")).Should().Be("hello");
        [Fact] public void JsonParse_number() => JsonXmlUdf.UDF_JSON_PARSE("42").Should().Be(42L);
        [Fact] public void JsonParse_bool_true() => ((bool)JsonXmlUdf.UDF_JSON_PARSE("true")).Should().Be(true);
        [Fact] public void JsonParse_bool_false() => ((bool)JsonXmlUdf.UDF_JSON_PARSE("false")).Should().Be(false);
        [Fact] public void JsonParse_null_literal() => JsonXmlUdf.UDF_JSON_PARSE("null").Should().BeNull();
        [Fact] public void JsonParse_invalid() => JsonXmlUdf.UDF_JSON_PARSE("{bad}").Should().Be(ExcelError.Value);
        [Fact] public void JsonParse_empty_string() => JsonXmlUdf.UDF_JSON_PARSE("").Should().Be(ExcelError.Value);

        [Fact] public void JsonQuery_simple() => JsonXmlUdf.UDF_JSON_QUERY("{\"a\":1}","a").Should().Be(1L);
        [Fact] public void JsonQuery_nested() => JsonXmlUdf.UDF_JSON_QUERY("{\"a\":{\"b\":2}}","a.b").Should().Be(2L);
        [Fact] public void JsonQuery_array_index() => JsonXmlUdf.UDF_JSON_QUERY("{\"a\":[10,20]}","a[0]").Should().Be(10L);
        [Fact] public void JsonQuery_string_value() => ((string)JsonXmlUdf.UDF_JSON_QUERY("{\"name\":\"Alice\"}","name")).Should().Be("Alice");
        [Fact] public void JsonQuery_bool_value() => ((bool)JsonXmlUdf.UDF_JSON_QUERY("{\"ok\":true}","ok")).Should().Be(true);
        [Fact] public void JsonQuery_missing_path() => JsonXmlUdf.UDF_JSON_QUERY("{\"a\":1}","b").Should().BeNull();
        [Fact] public void JsonQuery_invalid_json() => JsonXmlUdf.UDF_JSON_QUERY("{bad}","a").Should().Be(ExcelError.Value);
        [Fact] public void JsonQuery_empty_string() => JsonXmlUdf.UDF_JSON_QUERY("","a").Should().Be(ExcelError.Value);

        [Fact] public void JsonValidate_valid_object() => ((bool)JsonXmlUdf.UDF_JSON_VALIDATE("{\"a\":1}")).Should().BeTrue();
        [Fact] public void JsonValidate_valid_array() => ((bool)JsonXmlUdf.UDF_JSON_VALIDATE("[1,2,3]")).Should().BeTrue();
        [Fact] public void JsonValidate_valid_string() => ((bool)JsonXmlUdf.UDF_JSON_VALIDATE("\"hello\"")).Should().BeTrue();
        [Fact] public void JsonValidate_valid_number() => ((bool)JsonXmlUdf.UDF_JSON_VALIDATE("42")).Should().BeTrue();
        [Fact] public void JsonValidate_invalid() => ((bool)JsonXmlUdf.UDF_JSON_VALIDATE("{bad}")).Should().BeFalse();
        [Fact] public void JsonValidate_empty() => ((bool)JsonXmlUdf.UDF_JSON_VALIDATE("")).Should().BeFalse();
        [Fact] public void JsonValidate_null() => ((bool)JsonXmlUdf.UDF_JSON_VALIDATE(null!)).Should().BeFalse();

        [Fact] public void JsonPrettify_object() => ((string)JsonXmlUdf.UDF_JSON_PRETTIFY("{\"a\":1}")).Should().Contain("\"a\"");
        [Fact] public void JsonPrettify_array() => ((string)JsonXmlUdf.UDF_JSON_PRETTIFY("[1,2]")).Should().Contain("\n");
        [Fact] public void JsonPrettify_nested() { var s=(string)JsonXmlUdf.UDF_JSON_PRETTIFY("{\"a\":{\"b\":2}}"); s.Should().Contain("\"b\"").And.Contain("\n"); }
        [Fact] public void JsonPrettify_invalid() => JsonXmlUdf.UDF_JSON_PRETTIFY("{bad}").Should().Be(ExcelError.Value);
        [Fact] public void JsonPrettify_empty_string() => JsonXmlUdf.UDF_JSON_PRETTIFY("").Should().Be(ExcelError.Value);

        [Fact] public void JsonToTable_basic() { var r=(object[,])JsonXmlUdf.UDF_JSON_TOTABLE("[{\"a\":1,\"b\":2},{\"a\":3,\"b\":4}]"); r.GetLength(0).Should().Be(3); r.GetLength(1).Should().Be(2); r[0,0].Should().Be("a"); r[1,0].Should().Be(1L); }
        [Fact] public void JsonToTable_single_row() { var r=(object[,])JsonXmlUdf.UDF_JSON_TOTABLE("[{\"x\":10}]"); r.GetLength(0).Should().Be(2); r[0,0].Should().Be("x"); r[1,0].Should().Be(10L); }
        [Fact] public void JsonToTable_non_array() => JsonXmlUdf.UDF_JSON_TOTABLE("{\"a\":1}").Should().NotBeOfType<object[,]>();
        [Fact] public void JsonToTable_empty_array() => JsonXmlUdf.UDF_JSON_TOTABLE("[]").Should().NotBeOfType<object[,]>();
        [Fact] public void JsonToTable_invalid() => JsonXmlUdf.UDF_JSON_TOTABLE("{bad}").Should().Be(ExcelError.Value);

        [Fact] public void XmlXPath_single() { var r=(object[])JsonXmlUdf.UDF_XML_XPATH("<root><item>a</item></root>","//item"); r.Should().Equal("a"); }
        [Fact] public void XmlXPath_multiple() { var r=(object[])JsonXmlUdf.UDF_XML_XPATH("<r><i>a</i><i>b</i></r>","//i"); r.Should().Equal("a","b"); }
        [Fact] public void XmlXPath_element_value() { var r=(object[])JsonXmlUdf.UDF_XML_XPATH("<r><e>val</e></r>","//e"); r.Should().Equal("val"); }
        [Fact] public void XmlXPath_no_match() { var r=(object[])JsonXmlUdf.UDF_XML_XPATH("<r><e/></r>","//x"); r.Should().BeEmpty(); }
        [Fact] public void XmlXPath_invalid_xml() { var r=(object[])JsonXmlUdf.UDF_XML_XPATH("<bad>","//x"); r.Should().BeEmpty(); }
        [Fact] public void XmlXPath_empty() { var r=(object[])JsonXmlUdf.UDF_XML_XPATH("","//x"); r.Should().BeEmpty(); }

        [Fact] public void XmlValidate_valid() => ((bool)JsonXmlUdf.UDF_XML_VALIDATE("<root/>")).Should().BeTrue();
        [Fact] public void XmlValidate_valid_with_content() => ((bool)JsonXmlUdf.UDF_XML_VALIDATE("<a><b>c</b></a>")).Should().BeTrue();
        [Fact] public void XmlValidate_invalid() => ((bool)JsonXmlUdf.UDF_XML_VALIDATE("<root>")).Should().BeFalse();
        [Fact] public void XmlValidate_plain_text() => ((bool)JsonXmlUdf.UDF_XML_VALIDATE("not xml")).Should().BeFalse();
        [Fact] public void XmlValidate_empty() => ((bool)JsonXmlUdf.UDF_XML_VALIDATE("")).Should().BeFalse();

        [Fact] public void XmlToTable_basic() { var r=(object[,])JsonXmlUdf.UDF_XML_TOTABLE("<rows><r><c1>a</c1><c2>1</c2></r></rows>","//r"); r.GetLength(0).Should().Be(2); r[0,0].Should().Be("c1"); r[1,0].Should().Be("a"); }
        [Fact] public void XmlToTable_with_rowpath() { var r=(object[,])JsonXmlUdf.UDF_XML_TOTABLE("<root><a><x>1</x><y>2</y></a><a><x>3</x></a></root>","//a"); r.GetLength(0).Should().Be(3); }
        [Fact] public void XmlToTable_invalid() => JsonXmlUdf.UDF_XML_TOTABLE("<bad>","//r").Should().NotBeOfType<object[,]>();
        [Fact] public void XmlToTable_empty() => JsonXmlUdf.UDF_XML_TOTABLE("","//r").Should().NotBeOfType<object[,]>();
    }
}
