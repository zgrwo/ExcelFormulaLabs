using ExcelDna.Integration;
using ExcelVbaLibraries.Foundation;

namespace ExcelVbaLibraries.Analytics
{
    public static class PhyChemUdf
    {
        [ExcelFunction(Name="PHYCHEM.MOLWT")] public static object UDF_PC_MOLWT(object f)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,double>(f,PhyChemCore.MolecularWeight));
        [ExcelFunction(Name="PHYCHEM.TEMP")] public static object UDF_PC_TEMP(object v,object from,object to)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(v,d=>PhyChemCore.ConvertTemperature(d,S(from),S(to))));
        [ExcelFunction(Name="PHYCHEM.PRESS")] public static object UDF_PC_PRESS(object v,object from,object to)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(v,d=>PhyChemCore.ConvertPressure(d,S(from),S(to))));
        [ExcelFunction(Name="PHYCHEM.VOL")] public static object UDF_PC_VOL(object v,object from,object to)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(v,d=>PhyChemCore.ConvertVolume(d,S(from),S(to))));
        [ExcelFunction(Name="PHYCHEM.MASS")] public static object UDF_PC_MASS(object v,object from,object to)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(v,d=>PhyChemCore.ConvertMass(d,S(from),S(to))));
        [ExcelFunction(Name="PHYCHEM.C_TO_F")] public static object UDF_PC_CTOF(object v)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(v,d=>PhyChemCore.ConvertTemperature(d,"C","F")));
        [ExcelFunction(Name="PHYCHEM.F_TO_C")] public static object UDF_PC_FTOC(object v)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(v,d=>PhyChemCore.ConvertTemperature(d,"F","C")));
        [ExcelFunction(Name="PHYCHEM.KG_TO_LB")] public static object UDF_PC_KG2LB(object v)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(v,d=>PhyChemCore.ConvertMass(d,"KG","LB")));
        [ExcelFunction(Name="PHYCHEM.LB_TO_KG")] public static object UDF_PC_LB2KG(object v)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(v,d=>PhyChemCore.ConvertMass(d,"LB","KG")));
        [ExcelFunction(Name="PHYCHEM.L_TO_GAL")] public static object UDF_PC_L2GAL(object v)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(v,d=>PhyChemCore.ConvertVolume(d,"L","GAL")));
        [ExcelFunction(Name="PHYCHEM.GAL_TO_L")] public static object UDF_PC_GAL2L(object v)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(v,d=>PhyChemCore.ConvertVolume(d,"GAL","L")));
        [ExcelFunction(Name="PHYCHEM.ATM_TO_PSI")] public static object UDF_PC_ATM2PSI(object v)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(v,d=>PhyChemCore.ConvertPressure(d,"ATM","PSI")));
        [ExcelFunction(Name="PHYCHEM.PSI_TO_ATM")] public static object UDF_PC_PSI2ATM(object v)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(v,d=>PhyChemCore.ConvertPressure(d,"PSI","ATM")));
        [ExcelFunction(Name="PHYCHEM.IDEALGAS")] public static object UDF_PC_GAS(object p,object v,object n,object t)=>OutputWrapper.WrapError(()=>PhyChemCore.IdealGasLaw(V(p),V(v),V(n),V(t)));
        [ExcelFunction(Name="PHYCHEM.GASSTP")] public static object UDF_PC_STP(object vol,object temp,object press)=>OutputWrapper.WrapError(()=>PhyChemCore.GasToSTP(InputNormalizer.ToDouble(vol),InputNormalizer.ToDouble(temp),InputNormalizer.ToDouble(press)));
        [ExcelFunction(Name="PHYCHEM.DENSITY")] public static object UDF_PC_DEN(object mass,object vol)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOverMulti<double,double,double>(mass,vol,(m,v)=>m/v));
        private static string S(object o)=>InputNormalizer.ToString(o);
        private static double? V(object o){if(o is Foundation.ExcelEmpty||o is ExcelDna.Integration.ExcelEmpty||o==null)return null;return InputNormalizer.ToDouble(o);}
    }
}
