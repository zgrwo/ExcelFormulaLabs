using System.Collections.Generic;
using System.Linq;
using ExcelDna.Integration;
using ExcelVbaLibraries.Foundation;

namespace ExcelVbaLibraries.DataToolkit
{
    public static class ArrayUdf
    {
        private static object[] A(object a)=>InputNormalizer.NormalizeTo1D(a);
        [ExcelFunction(Name="ARR.SORT", Description="Sort array. asc=true/false, mode=auto/text/numeric")] public static object UDF_ARR_SORT(object a,object asc,object mode)=>OutputWrapper.WrapError(()=>ArrayCore.Sort(A(a),InputNormalizer.ToBool(asc),InputNormalizer.ToString(mode)));
        [ExcelFunction(Name="ARR.SORTASC", Description="Sort array ascending (auto-detect type)")] public static object UDF_ARR_SORTASC(object a)=>OutputWrapper.WrapError(()=>ArrayCore.Sort(A(a),true,"auto"));
        [ExcelFunction(Name="ARR.SORTDESC", Description="Sort array descending (auto-detect type)")] public static object UDF_ARR_SORTDESC(object a)=>OutputWrapper.WrapError(()=>ArrayCore.Sort(A(a),false,"auto"));
        [ExcelFunction(Name="ARR.SORTNUM", Description="Sort array ascending by numeric value")] public static object UDF_ARR_SORTNUM(object a)=>OutputWrapper.WrapError(()=>ArrayCore.Sort(A(a),true,"numeric"));
        [ExcelFunction(Name="ARR.SORTTEXT", Description="Sort array ascending by text (case-insensitive)")] public static object UDF_ARR_SORTTEXT(object a)=>OutputWrapper.WrapError(()=>ArrayCore.Sort(A(a),true,"text"));
        [ExcelFunction(Name="ARR.UNIQUE", Description="Return unique values from an array")] public static object UDF_ARR_UNIQUE(object a)=>OutputWrapper.WrapError(()=>ArrayCore.Unique(A(a)));
        [ExcelFunction(Name="ARR.INDEXOF", Description="Zero-based index of first occurrence, or -1 if not found")] public static object UDF_ARR_INDEXOF(object a,object v)=>OutputWrapper.WrapError(()=>(long)ArrayCore.IndexOf(A(a),v));
        [ExcelFunction(Name="ARR.SLICE", Description="Extract l elements from an array starting at index s")] public static object UDF_ARR_SLICE(object a,object s,object l)=>OutputWrapper.WrapError(()=>ArrayCore.Slice(A(a),InputNormalizer.ToLong(s),InputNormalizer.ToLong(l)));
        [ExcelFunction(Name="ARR.FLATTEN", Description="Flatten a 2D range into 1D, row-major order")] public static object UDF_ARR_FLATTEN(object a)=>OutputWrapper.WrapError(()=>ArrayCore.Flatten2D(InputNormalizer.NormalizeTo2D(a)!));
        [ExcelFunction(Name="ARR.FILTER", Description="Filter array by comparison operator: =, <>, >, <, >=, <=")] public static object UDF_ARR_FILTER(object a,object c,object op)=>OutputWrapper.WrapError(()=>ArrayCore.Filter(A(a),c,InputNormalizer.ToString(op)));
        [ExcelFunction(Name="ARR.FILTER_EQ", Description="Filter array for elements equal to c")] public static object UDF_ARR_FEQ(object a,object c)=>OutputWrapper.WrapError(()=>ArrayCore.Filter(A(a),c,"="));
        [ExcelFunction(Name="ARR.FILTER_NE", Description="Filter array for elements not equal to c")] public static object UDF_ARR_FNE(object a,object c)=>OutputWrapper.WrapError(()=>ArrayCore.Filter(A(a),c,"<>"));
        [ExcelFunction(Name="ARR.FILTER_GT", Description="Filter array for elements greater than c")] public static object UDF_ARR_FGT(object a,object c)=>OutputWrapper.WrapError(()=>ArrayCore.Filter(A(a),c,">"));
        [ExcelFunction(Name="ARR.FILTER_LT", Description="Filter array for elements less than c")] public static object UDF_ARR_FLT(object a,object c)=>OutputWrapper.WrapError(()=>ArrayCore.Filter(A(a),c,"<"));
        [ExcelFunction(Name="ARR.CONCAT", Description="Concatenate two arrays")] public static object UDF_ARR_CONCAT(object a,object b)=>OutputWrapper.WrapError(()=>ArrayCore.Concat(A(a),A(b)));
        [ExcelFunction(Name="ARR.REVERSE", Description="Reverse array order")] public static object UDF_ARR_REVERSE(object a)=>OutputWrapper.WrapError(()=>ArrayCore.Reverse(A(a)));
        [ExcelFunction(Name="ARR.COUNT", Description="Count of elements in an array")] public static object UDF_ARR_COUNT(object a)=>OutputWrapper.WrapError(()=>(long)ArrayCore.Count(A(a)));
        [ExcelFunction(Name="ARR.CONTAINS", Description="Returns TRUE if value exists in array")] public static object UDF_ARR_CONTAINS(object a,object v)=>OutputWrapper.WrapError(()=>(object)ArrayCore.Contains(A(a),v));
        [ExcelFunction(Name="ARR.TOSET", Description="Return unique values (alias for ARR.UNIQUE)")] public static object UDF_ARR_TOSET(object a)=>OutputWrapper.WrapError(()=>ArrayCore.Unique(A(a)));
        [ExcelFunction(Name="ARR.FILL", Description="Create array of length n filled with value v")] public static object UDF_ARR_FILL(object v,object n)=>OutputWrapper.WrapError(()=>{long c=InputNormalizer.ToLong(n);var r=new object[c];for(int i=0;i<c;i++)r[i]=v;return r;});
        [ExcelFunction(Name="ARR.RANGE", Description="Generate numeric sequence from s to e by step")] public static object UDF_ARR_RANGE(object s,object e,object step)=>OutputWrapper.WrapError(()=>{double st=InputNormalizer.ToDouble(s),en=InputNormalizer.ToDouble(e),sp=InputNormalizer.ToDouble(step);if(sp<=0)sp=1;var r=new List<object>();for(double x=st;x<=en;x+=sp)r.Add(x);return r.ToArray();});
        [ExcelFunction(Name="ARR.SHUFFLE", Description="Randomly shuffle array elements (Fisher-Yates)")] public static object UDF_ARR_SHUFFLE(object a)=>OutputWrapper.WrapError(()=>{var r=A(a);
#if NET8_0_OR_GREATER
var rng=System.Random.Shared;
#else
var rng=ThreadLocalRng.Value;
#endif
for(int i=r.Length-1;i>0;i--){int j=rng.Next(i+1);var t=r[i];r[i]=r[j];r[j]=t;}return r;});
#if !NET8_0_OR_GREATER
private static readonly System.Threading.ThreadLocal<System.Random> ThreadLocalRng = new(()=>new System.Random());
#endif
    }
}
