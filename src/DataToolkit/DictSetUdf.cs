using ExcelDna.Integration;
using ExcelVbaLibraries.Foundation;

namespace ExcelVbaLibraries.DataToolkit
{
    public static class DictSetUdf
    {
        private static object[] A(object a)=>InputNormalizer.NormalizeTo1D(a);
        [ExcelFunction(Name="DICT.FREQUENCY", Description="Frequency count of each unique value. Returns 2 columns: value, count")] public static object UDF_DICT_FREQ([ExcelArgument(Name="key_array", Description="Array of key values")] object k)=>OutputWrapper.WrapError(()=>DictSetCore.Frequency(A(k)));
        [ExcelFunction(Name="DICT.INTERSECT", Description="Set intersection: values present in both arrays")] public static object UDF_DICT_INTER([ExcelArgument(Name="array1", Description="First range or array")] object a, [ExcelArgument(Name="array2", Description="Second range or array")] object b)=>OutputWrapper.WrapError(()=>DictSetCore.Intersect(A(a),A(b)));
        [ExcelFunction(Name="DICT.UNION", Description="Set union: all unique values from both arrays")] public static object UDF_DICT_UNION([ExcelArgument(Name="array1", Description="First range or array")] object a, [ExcelArgument(Name="array2", Description="Second range or array")] object b)=>OutputWrapper.WrapError(()=>DictSetCore.Union(A(a),A(b)));
        [ExcelFunction(Name="DICT.EXCEPT", Description="Set difference: values in first array but not in second")] public static object UDF_DICT_EXCEPT([ExcelArgument(Name="array1", Description="First range or array")] object a, [ExcelArgument(Name="array2", Description="Second range or array")] object b)=>OutputWrapper.WrapError(()=>DictSetCore.Except(A(a),A(b)));
        [ExcelFunction(Name="DICT.DICT", Description="Build a 2-column dictionary from parallel key and value arrays")] public static object UDF_DICT_DICT([ExcelArgument(Name="key_array", Description="Array of key values")] object k, [ExcelArgument(Name="value_array", Description="Array of values corresponding to each key")] object v)=>OutputWrapper.WrapError(()=>DictSetCore.Dict(A(k),A(v)));
        [ExcelFunction(Name="DICT.COUNT", Description="Number of rows in a 2-column dictionary table")] public static object UDF_DICT_COUNT([ExcelArgument(Name="dict_table", Description="A 2-column table with keys in column 0 and values in column 1")] object d)=>OutputWrapper.WrapError(()=>(long)DictSetCore.Count(InputNormalizer.NormalizeTo2D(d)!));
        [ExcelFunction(Name="DICT.KEYS", Description="Extract keys (column 0) from a 2-column dictionary table")] public static object UDF_DICT_KEYS([ExcelArgument(Name="dict_table", Description="A 2-column table with keys in column 0 and values in column 1")] object d)=>OutputWrapper.WrapError(()=>{var m=InputNormalizer.NormalizeTo2D(d)!;var r=new object[m.GetLength(0)];for(int i=0;i<r.Length;i++)r[i]=m[i,0];return r;});
        [ExcelFunction(Name="DICT.VALUES", Description="Extract values (column 1) from a 2-column dictionary table")] public static object UDF_DICT_VALS([ExcelArgument(Name="dict_table", Description="A 2-column table with keys in column 0 and values in column 1")] object d)=>OutputWrapper.WrapError(()=>{var m=InputNormalizer.NormalizeTo2D(d)!;var r=new object[m.GetLength(0)];for(int i=0;i<r.Length;i++)r[i]=m[i,1];return r;});
    }
}
