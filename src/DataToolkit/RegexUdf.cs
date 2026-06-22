using ExcelDna.Integration;
using ExcelVbaLibraries.Foundation;

namespace ExcelVbaLibraries.DataToolkit
{
    public static class RegexUdf
    {
        [ExcelFunction(Name="REGEX.TEST", Description="Returns TRUE if string matches regex pattern")] public static object UDF_RX_TEST(object i,object p,object ic)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOverMulti<string,string,bool>(i,p,(x,y)=>RegexCore.RegexTest(x,y,InputNormalizer.ToBool(ic))));
        [ExcelFunction(Name="REGEX.COUNT", Description="Count of non-overlapping regex matches in a string")] public static object UDF_RX_COUNT(object i,object p,object ic)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOverMulti<string,string,long>(i,p,(x,y)=>RegexCore.RegexCount(x,y,InputNormalizer.ToBool(ic))));
        [ExcelFunction(Name="REGEX.MATCH", Description="First regex match substring, or empty string if no match")] public static object UDF_RX_MATCH(object i,object p,object ic)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOverMulti<string,string,string>(i,p,(x,y)=>RegexCore.RegexMatch(x,y,InputNormalizer.ToBool(ic))));
        [ExcelFunction(Name="REGEX.MATCHALL", Description="All regex matches as array of strings")] public static object UDF_RX_MALL(object i,object p,object ic)=>OutputWrapper.WrapError(()=>RegexCore.RegexMatchAll(InputNormalizer.ToString(i),InputNormalizer.ToString(p),InputNormalizer.ToBool(ic)));
        [ExcelFunction(Name="REGEX.REPLACE", Description="Replace all regex matches with replacement string")] public static object UDF_RX_REPL(object i,object p,object r,object ic)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,string>(i,s=>RegexCore.RegexReplace(s,InputNormalizer.ToString(p),InputNormalizer.ToString(r),InputNormalizer.ToBool(ic))));
        [ExcelFunction(Name="REGEX.SPLIT", Description="Split string by regex delimiter into array")] public static object UDF_RX_SPLIT(object i,object p,object ic)=>OutputWrapper.WrapError(()=>RegexCore.RegexSplit(InputNormalizer.ToString(i),InputNormalizer.ToString(p),InputNormalizer.ToBool(ic)));
        [ExcelFunction(Name="REGEX.GROUPS", Description="Capture groups from first match. Returns 2xn: group names row0, values row1")] public static object UDF_RX_GRP(object i,object p,object ic)=>OutputWrapper.WrapError(()=>RegexCore.RegexCaptureGroups(InputNormalizer.ToString(i),InputNormalizer.ToString(p),InputNormalizer.ToBool(ic)));
        [ExcelFunction(Name="REGEX.ESCAPE", Description="Escape regex special characters in a literal string")] public static object UDF_RX_ESC(object l)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,string>(l,RegexCore.RegexEscape));
        [ExcelFunction(Name="REGEX.ISMATCH", Description="Case-sensitive regex match test (alias for REGEX.TEST with ic=true)")] public static object UDF_RX_ISMATCH(object i,object p)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOverMulti<string,string,bool>(i,p,(x,y)=>RegexCore.RegexTest(x,y,true)));
    }
}
