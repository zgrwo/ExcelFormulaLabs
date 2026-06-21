using ExcelVbaLibraries.Foundation;

namespace ExcelVbaLibraries.Analytics
{
    internal static class AnalyticsHelpers
    {
        internal static double[,] ToDoubleMatrix(object[,] data)
        { int r=data.GetLength(0),c=data.GetLength(1); var m=new double[r,c]; for(int i=0;i<r;i++)for(int j=0;j<c;j++)m[i,j]=InputNormalizer.ToDouble(data[i,j]); return m; }
        internal static double[,] PrepM(object data)=>ToDoubleMatrix(InputNormalizer.NormalizeTo2D(data)!);
        internal static double[] PrepV(object data)=>InputNormalizer.ToDoubles(data);
    }
}
