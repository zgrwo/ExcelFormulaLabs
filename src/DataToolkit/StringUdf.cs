using System.Linq;
using ExcelDna.Integration;
using ExcelVbaLibraries.Foundation;

namespace ExcelVbaLibraries.DataToolkit
{
    public static class StringUdf
    {
        [ExcelFunction(Name="STR.REVERSE")] public static object UDF_STR_REV(object t)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,string>(t,StringCore.ReverseString));
        [ExcelFunction(Name="STR.NORMWS")] public static object UDF_STR_NWS(object t)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,string>(t,StringCore.NormalizeWhitespace));
        [ExcelFunction(Name="STR.TITLE")] public static object UDF_STR_TITLE(object t)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,string>(t,StringCore.ToTitleCase));
        [ExcelFunction(Name="STR.REMOVE")] public static object UDF_STR_REM(object t,object ch)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,string>(t,s=>StringCore.RemoveChars(s,InputNormalizer.ToString(ch))));
        [ExcelFunction(Name="STR.KEEP")] public static object UDF_STR_KEEP(object t,object ch)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,string>(t,s=>StringCore.KeepChars(s,InputNormalizer.ToString(ch))));
        [ExcelFunction(Name="STR.PADLEFT")] public static object UDF_STR_PADL(object t,object len,object pad)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,string>(t,s=>StringCore.PadLeft(s,(int)InputNormalizer.ToLong(len),InputNormalizer.ToString(pad)[0])));
        [ExcelFunction(Name="STR.PADRIGHT")] public static object UDF_STR_PADR(object t,object len,object pad)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,string>(t,s=>StringCore.PadRight(s,(int)InputNormalizer.ToLong(len),InputNormalizer.ToString(pad)[0])));
        [ExcelFunction(Name="STR.TRUNCATE")] public static object UDF_STR_TRUNC(object t,object len,object sfx)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,string>(t,s=>StringCore.Truncate(s,(int)InputNormalizer.ToLong(len),InputNormalizer.ToString(sfx))));
        [ExcelFunction(Name="STR.COUNTSUB")] public static object UDF_STR_CNT(object t,object s,object cs)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOverMulti<string,string,long>(t,s,(a,b)=>StringCore.CountSubstring(a,b,InputNormalizer.ToBool(cs))));
        [ExcelFunction(Name="STR.STARTSWITH")] public static object UDF_STR_SW(object t,object p,object cs)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOverMulti<string,string,bool>(t,p,(a,b)=>StringCore.StartsWithStr(a,b,InputNormalizer.ToBool(cs))));
        [ExcelFunction(Name="STR.ENDSWITH")] public static object UDF_STR_EW(object t,object s,object cs)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOverMulti<string,string,bool>(t,s,(a,b)=>StringCore.EndsWithStr(a,b,InputNormalizer.ToBool(cs))));
        [ExcelFunction(Name="STR.LEFTOF")] public static object UDF_STR_LOF(object t,object d,object n)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,string>(t,s=>StringCore.LeftOf(s,InputNormalizer.ToString(d),InputNormalizer.ToLong(n))));
        [ExcelFunction(Name="STR.RIGHTOF")] public static object UDF_STR_ROF(object t,object d,object n)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,string>(t,s=>StringCore.RightOf(s,InputNormalizer.ToString(d),InputNormalizer.ToLong(n))));
        [ExcelFunction(Name="STR.EXTRACT")] public static object UDF_STR_EXT(object t,object l,object r,object n,object inc)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,string>(t,s=>StringCore.ExtractBetween(s,InputNormalizer.ToString(l),InputNormalizer.ToString(r),InputNormalizer.ToLong(n),InputNormalizer.ToBool(inc))));
        [ExcelFunction(Name="STR.NTHWORD")] public static object UDF_STR_NTHW(object t,object n)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,string>(t,s=>StringCore.NthWord(s,InputNormalizer.ToLong(n))));
        [ExcelFunction(Name="STR.COMMONPFX")] public static object UDF_STR_CPFX(object a,object b,object cs)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOverMulti<string,string,string>(a,b,(x,y)=>StringCore.CommonPrefix(x,y,InputNormalizer.ToBool(cs))));
        [ExcelFunction(Name="STR.TEXTJOIN")] public static object UDF_STR_JOIN(object d,object skip,object vals)=>OutputWrapper.WrapError(()=>{var a=InputNormalizer.NormalizeTo1D(vals).Select(x=>InputNormalizer.ToString(x)).ToArray();return StringCore.TextJoin(InputNormalizer.ToString(d),InputNormalizer.ToBool(skip),a);});
        [ExcelFunction(Name="STR.LEVENSHTEIN")] public static object UDF_STR_LEV(object a,object b)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOverMulti<string,string,long>(a,b,(x,y)=>StringCore.LevenshteinDistance(x,y)));
        [ExcelFunction(Name="STR.SOUNDEX")] public static object UDF_STR_SDX(object t)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,string>(t,StringCore.Soundex));
        [ExcelFunction(Name="STR.URLENCODE")] public static object UDF_STR_UENC(object t)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,string>(t,StringCore.UrlEncode));
        [ExcelFunction(Name="STR.URLDECODE")] public static object UDF_STR_UDEC(object t)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,string>(t,StringCore.UrlDecode));
        [ExcelFunction(Name="STR.HTMLENCODE")] public static object UDF_STR_HENC(object t)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,string>(t,StringCore.HtmlEncode));
        [ExcelFunction(Name="STR.HTMLDECODE")] public static object UDF_STR_HDEC(object t)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,string>(t,StringCore.HtmlDecode));
        [ExcelFunction(Name="STR.BASE64ENC")] public static object UDF_STR_B64ENC(object t)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,string>(t,StringCore.Base64Encode));
        [ExcelFunction(Name="STR.BASE64DEC")] public static object UDF_STR_B64DEC(object t)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,string>(t,StringCore.Base64Decode));
        [ExcelFunction(Name="STR.UUID")] public static object UDF_STR_UUID()=>OutputWrapper.WrapError(()=>(object)StringCore.UUID());
        [ExcelFunction(Name="STR.RNDSTR")] public static object UDF_STR_RND(object len,object cs)=>OutputWrapper.WrapError(()=>(object)StringCore.RandomString(InputNormalizer.ToLong(len),InputNormalizer.ToString(cs)));
        [ExcelFunction(Name="STR.RNDALPHA")] public static object UDF_STR_RNDA(object len)=>OutputWrapper.WrapError(()=>(object)StringCore.RandomString(InputNormalizer.ToLong(len),"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"));
        [ExcelFunction(Name="STR.RNDNUM")] public static object UDF_STR_RNDN(object len)=>OutputWrapper.WrapError(()=>(object)StringCore.RandomString(InputNormalizer.ToLong(len),"0123456789"));
        [ExcelFunction(Name="STR.ISNULLEMPTY")] public static object UDF_STR_ISNE(object t)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,bool>(t,s=>StringCore.IsNullOrEmptyStr(s)));
        [ExcelFunction(Name="STR.ISNULLWS")] public static object UDF_STR_ISNW(object t)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,bool>(t,s=>StringCore.IsNullOrWhitespaceStr(s)));
        [ExcelFunction(Name="STR.COALESCE")] public static object UDF_STR_COAL(object p,object fb)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOverMulti<string,string,string>(p,fb,(a,b)=>StringCore.Coalesce(a,b)));
        [ExcelFunction(Name="STR.FORMAT")] public static object UDF_STR_FMT(object v,object fmt)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOverMulti<string,string,string>(v,fmt,(a,b)=>{try{return string.Format("{0:"+b+"}",a);}catch{return a;}}));
        [ExcelFunction(Name="STR.STRIPHTML")] public static object UDF_STR_SHTML(object t)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,string>(t,s=>System.Text.RegularExpressions.Regex.Replace(s,"<[^>]+>","")));
    }
}
