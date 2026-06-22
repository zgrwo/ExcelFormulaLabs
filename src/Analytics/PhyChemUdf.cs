using ExcelDna.Integration;
using ExcelVbaLibraries.Foundation;

namespace ExcelVbaLibraries.Analytics
{
    public static class PhyChemUdf
    {
        [ExcelFunction(Name="PHYCHEM.MOLWT", Description="Molecular weight from chemical formula (e.g. 'H2SO4')")] public static object UDF_PC_MOLWT(object f)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,double>(f,PhyChemCore.MolecularWeight));
        [ExcelFunction(Name="PHYCHEM.TEMP", Description="Convert temperature between units: C, F, K")] public static object UDF_PC_TEMP(object v,object from,object to)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(v,d=>PhyChemCore.ConvertTemperature(d,S(from),S(to))));
        [ExcelFunction(Name="PHYCHEM.PRESS", Description="Convert pressure between units: ATM, PSI, PA, KPA, BAR, MMHG, TORR")] public static object UDF_PC_PRESS(object v,object from,object to)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(v,d=>PhyChemCore.ConvertPressure(d,S(from),S(to))));
        [ExcelFunction(Name="PHYCHEM.VOL", Description="Convert volume between units: L, GAL, ML, M3, CC, QT, PT, CUP, FLOZ")] public static object UDF_PC_VOL(object v,object from,object to)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(v,d=>PhyChemCore.ConvertVolume(d,S(from),S(to))));
        [ExcelFunction(Name="PHYCHEM.MASS", Description="Convert mass between units: KG, G, LB, OZ, TONNE, MG")] public static object UDF_PC_MASS(object v,object from,object to)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(v,d=>PhyChemCore.ConvertMass(d,S(from),S(to))));
        [ExcelFunction(Name="PHYCHEM.C_TO_F", Description="Celsius to Fahrenheit")] public static object UDF_PC_CTOF(object v)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(v,d=>PhyChemCore.ConvertTemperature(d,"C","F")));
        [ExcelFunction(Name="PHYCHEM.F_TO_C", Description="Fahrenheit to Celsius")] public static object UDF_PC_FTOC(object v)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(v,d=>PhyChemCore.ConvertTemperature(d,"F","C")));
        [ExcelFunction(Name="PHYCHEM.KG_TO_LB", Description="Kilograms to pounds")] public static object UDF_PC_KG2LB(object v)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(v,d=>PhyChemCore.ConvertMass(d,"KG","LB")));
        [ExcelFunction(Name="PHYCHEM.LB_TO_KG", Description="Pounds to kilograms")] public static object UDF_PC_LB2KG(object v)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(v,d=>PhyChemCore.ConvertMass(d,"LB","KG")));
        [ExcelFunction(Name="PHYCHEM.L_TO_GAL", Description="Liters to US gallons")] public static object UDF_PC_L2GAL(object v)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(v,d=>PhyChemCore.ConvertVolume(d,"L","GAL")));
        [ExcelFunction(Name="PHYCHEM.GAL_TO_L", Description="US gallons to liters")] public static object UDF_PC_GAL2L(object v)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(v,d=>PhyChemCore.ConvertVolume(d,"GAL","L")));
        [ExcelFunction(Name="PHYCHEM.ATM_TO_PSI", Description="Atmospheres to PSI")] public static object UDF_PC_ATM2PSI(object v)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(v,d=>PhyChemCore.ConvertPressure(d,"ATM","PSI")));
        [ExcelFunction(Name="PHYCHEM.PSI_TO_ATM", Description="PSI to atmospheres")] public static object UDF_PC_PSI2ATM(object v)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(v,d=>PhyChemCore.ConvertPressure(d,"PSI","ATM")));
        [ExcelFunction(Name="PHYCHEM.IDEALGAS", Description="Ideal gas law: PV = nRT. Solve for any unknown (pass * for variable to solve)")] public static object UDF_PC_GAS(object p,object v,object n,object t)=>OutputWrapper.WrapError(()=>PhyChemCore.IdealGasLaw(V(p),V(v),V(n),V(t)));
        [ExcelFunction(Name="PHYCHEM.GASSTP", Description="Convert gas volume to standard temperature and pressure (STP)")] public static object UDF_PC_STP(object vol,object temp,object press)=>OutputWrapper.WrapError(()=>PhyChemCore.GasToSTP(InputNormalizer.ToDouble(vol),InputNormalizer.ToDouble(temp),InputNormalizer.ToDouble(press)));
        [ExcelFunction(Name="PHYCHEM.DENSITY", Description="Compute density: mass / volume")] public static object UDF_PC_DEN(object mass,object vol)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOverMulti<double,double,double>(mass,vol,(m,v)=>m/v));
        private static string S(object o)=>InputNormalizer.ToString(o);
        private static double? V(object o){if(o is Foundation.ExcelEmpty||o is ExcelDna.Integration.ExcelEmpty||o==null||o is string s&&s=="*")return null;var d=InputNormalizer.ToDouble(o);return double.IsNaN(d)?null:d;}
    }
}
