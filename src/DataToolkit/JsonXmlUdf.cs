using ExcelDna.Integration;
using ExcelVbaLibraries.Foundation;

namespace ExcelVbaLibraries.DataToolkit
{
    public static class JsonXmlUdf
    {
        [ExcelFunction(Name="JSON.PARSE")] public static object UDF_JSON_PARSE(object j)=>OutputWrapper.WrapError(()=>JsonXmlCore.JsonParse(InputNormalizer.ToString(j)));
        [ExcelFunction(Name="JSON.QUERY")] public static object UDF_JSON_QUERY(object j,object p)=>OutputWrapper.WrapError(()=>JsonXmlCore.JsonQuery(InputNormalizer.ToString(j),InputNormalizer.ToString(p)));
        [ExcelFunction(Name="JSON.VALIDATE")] public static object UDF_JSON_VALIDATE(object j)=>OutputWrapper.WrapError(()=>JsonXmlCore.JsonValidate(InputNormalizer.ToString(j)));
        [ExcelFunction(Name="JSON.PRETTIFY")] public static object UDF_JSON_PRETTIFY(object j)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,string>(j,JsonXmlCore.JsonPrettify));
        [ExcelFunction(Name="JSON.TOTABLE")] public static object UDF_JSON_TOTABLE(object j)=>OutputWrapper.WrapError(()=>(object?)JsonXmlCore.JsonToTable(InputNormalizer.ToString(j))??ExcelDna.Integration.ExcelEmpty.Value);
        [ExcelFunction(Name="XML.XPATH")] public static object UDF_XML_XPATH(object x,object xp)=>OutputWrapper.WrapError(()=>JsonXmlCore.XmlXPath(InputNormalizer.ToString(x),InputNormalizer.ToString(xp)));
        [ExcelFunction(Name="XML.VALIDATE")] public static object UDF_XML_VALIDATE(object x)=>OutputWrapper.WrapError(()=>JsonXmlCore.XmlValidate(InputNormalizer.ToString(x)));
        [ExcelFunction(Name="XML.TOTABLE")] public static object UDF_XML_TOTABLE(object x,object rp)=>OutputWrapper.WrapError(()=>(object?)JsonXmlCore.XmlToTable(InputNormalizer.ToString(x),InputNormalizer.ToString(rp))??ExcelDna.Integration.ExcelEmpty.Value);
    }
}
