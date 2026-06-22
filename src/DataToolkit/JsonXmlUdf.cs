using ExcelDna.Integration;
using ExcelVbaLibraries.Foundation;

namespace ExcelVbaLibraries.DataToolkit
{
    public static class JsonXmlUdf
    {
        [ExcelFunction(Name="JSON.PARSE", Description="Parse a JSON string into a nested table/array structure")] public static object UDF_JSON_PARSE(object j)=>OutputWrapper.WrapError(()=>JsonXmlCore.JsonParse(InputNormalizer.ToString(j)));
        [ExcelFunction(Name="JSON.QUERY", Description="Query JSON with dot-notation path (e.g. 'store.book[0].title')")] public static object UDF_JSON_QUERY(object j,object p)=>OutputWrapper.WrapError(()=>JsonXmlCore.JsonQuery(InputNormalizer.ToString(j),InputNormalizer.ToString(p)));
        [ExcelFunction(Name="JSON.VALIDATE", Description="Returns TRUE if string is valid JSON")] public static object UDF_JSON_VALIDATE(object j)=>OutputWrapper.WrapError(()=>JsonXmlCore.JsonValidate(InputNormalizer.ToString(j)));
        [ExcelFunction(Name="JSON.PRETTIFY", Description="Pretty-print JSON string with indentation")] public static object UDF_JSON_PRETTIFY(object j)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,string>(j,JsonXmlCore.JsonPrettify));
        [ExcelFunction(Name="JSON.TOTABLE", Description="Convert JSON array of objects to a 2D Excel table with headers")] public static object UDF_JSON_TOTABLE(object j)=>OutputWrapper.WrapError(()=>(object?)JsonXmlCore.JsonToTable(InputNormalizer.ToString(j))??ExcelDna.Integration.ExcelEmpty.Value);
        [ExcelFunction(Name="XML.XPATH", Description="Evaluate XPath expression on XML string, returns matching element values")] public static object UDF_XML_XPATH(object x,object xp)=>OutputWrapper.WrapError(()=>JsonXmlCore.XmlXPath(InputNormalizer.ToString(x),InputNormalizer.ToString(xp)));
        [ExcelFunction(Name="XML.VALIDATE", Description="Returns TRUE if string is well-formed XML")] public static object UDF_XML_VALIDATE(object x)=>OutputWrapper.WrapError(()=>JsonXmlCore.XmlValidate(InputNormalizer.ToString(x)));
        [ExcelFunction(Name="XML.TOTABLE", Description="Convert XML to a 2D Excel table using row-path XPath expression")] public static object UDF_XML_TOTABLE(object x,object rp)=>OutputWrapper.WrapError(()=>(object?)JsonXmlCore.XmlToTable(InputNormalizer.ToString(x),InputNormalizer.ToString(rp))??ExcelDna.Integration.ExcelEmpty.Value);
    }
}
