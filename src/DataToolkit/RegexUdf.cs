using ExcelDna.Integration;
using ExcelVbaLibraries.Foundation;

namespace ExcelVbaLibraries.DataToolkit
{
    public static class RegexUdf
    {
        [ExcelFunction(Name="REGEX.TEST")] public static object UDF_RX_TEST(object i,object p,object ic)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOverMulti<string,string,bool>(i,p,(x,y)=>RegexCore.RegexTest(x,y,InputNormalizer.ToBool(ic))));
        [ExcelFunction(Name="REGEX.COUNT")] public static object UDF_RX_COUNT(object i,object p,object ic)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOverMulti<string,string,long>(i,p,(x,y)=>RegexCore.RegexCount(x,y,InputNormalizer.ToBool(ic))));
        [ExcelFunction(Name="REGEX.MATCH")] public static object UDF_RX_MATCH(object i,object p,object ic)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOverMulti<string,string,string>(i,p,(x,y)=>RegexCore.RegexMatch(x,y,InputNormalizer.ToBool(ic))));
        [ExcelFunction(Name="REGEX.MATCHALL")] public static object UDF_RX_MALL(object i,object p,object ic)=>OutputWrapper.WrapError(()=>RegexCore.RegexMatchAll(InputNormalizer.ToString(i),InputNormalizer.ToString(p),InputNormalizer.ToBool(ic)));
        [ExcelFunction(Name="REGEX.REPLACE")] public static object UDF_RX_REPL(object i,object p,object r,object ic)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,string>(i,s=>RegexCore.RegexReplace(s,InputNormalizer.ToString(p),InputNormalizer.ToString(r),InputNormalizer.ToBool(ic))));
        [ExcelFunction(Name="REGEX.SPLIT")] public static object UDF_RX_SPLIT(object i,object p,object ic)=>OutputWrapper.WrapError(()=>RegexCore.RegexSplit(InputNormalizer.ToString(i),InputNormalizer.ToString(p),InputNormalizer.ToBool(ic)));
        [ExcelFunction(Name="REGEX.GROUPS")] public static object UDF_RX_GRP(object i,object p,object ic)=>OutputWrapper.WrapError(()=>RegexCore.RegexCaptureGroups(InputNormalizer.ToString(i),InputNormalizer.ToString(p),InputNormalizer.ToBool(ic)));
        [ExcelFunction(Name="REGEX.ESCAPE")] public static object UDF_RX_ESC(object l)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,string>(l,RegexCore.RegexEscape));
        [ExcelFunction(Name="REGEX.ISMATCH")] public static object UDF_RX_ISMATCH(object i,object p)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOverMulti<string,string,bool>(i,p,(x,y)=>RegexCore.RegexTest(x,y,true)));
    }
}
