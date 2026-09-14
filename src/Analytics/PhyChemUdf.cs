using ExcelDna.Integration;
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.Analytics
{
    public static class PhyChemUdf
    {
        [ExcelFunction(Name="PHYCHEM.MOLWT", Description="Molecular weight from chemical formula (e.g. 'H2SO4')")] public static object UDF_PC_MOLWT([ExcelArgument(Name="formula_text", Description="A chemical formula, e.g. H2SO4 or Fe4[Fe(CN)6]3")] object f)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,double>(f,PhyChemCore.MolecularWeight));
        [ExcelFunction(Name="PHYCHEM.TEMP", Description="Convert temperature between units: C, F, K")] public static object UDF_PC_TEMP([ExcelArgument(Name="number", Description="A numeric value or array of numbers")] object v, [ExcelArgument(Name="from_unit", Description="Source temperature unit: C, F, or K")] object from, [ExcelArgument(Name="to_unit", Description="Target temperature unit: C, F, or K")] object to)=>OutputWrapper.WrapError(()=>{var f=S(from);var t=S(to);return ElementWiseMapper.MapOver<double,double>(v,d=>PhyChemCore.ConvertTemperature(d,f,t));});
        [ExcelFunction(Name="PHYCHEM.PRESS", Description="Convert pressure between units: ATM, PSI, PA, KPA, BAR, MMHG, TORR")] public static object UDF_PC_PRESS([ExcelArgument(Name="number", Description="A numeric value or array of numbers")] object v, [ExcelArgument(Name="from_unit", Description="Source pressure unit, e.g. ATM, PSI, KPA")] object from, [ExcelArgument(Name="to_unit", Description="Target pressure unit, e.g. PA, BAR, MMHG")] object to)=>OutputWrapper.WrapError(()=>{var f=S(from);var t=S(to);return ElementWiseMapper.MapOver<double,double>(v,d=>PhyChemCore.ConvertPressure(d,f,t));});
        [ExcelFunction(Name="PHYCHEM.VOL", Description="Convert volume between units: L, ML, M3, GAL, QT, FT3")] public static object UDF_PC_VOL([ExcelArgument(Name="number", Description="A numeric value or array of numbers")] object v, [ExcelArgument(Name="from_unit", Description="Source volume unit, e.g. L, GAL, M3")] object from, [ExcelArgument(Name="to_unit", Description="Target volume unit, e.g. ML, QT, FT3")] object to)=>OutputWrapper.WrapError(()=>{var f=S(from);var t=S(to);return ElementWiseMapper.MapOver<double,double>(v,d=>PhyChemCore.ConvertVolume(d,f,t));});
        [ExcelFunction(Name="PHYCHEM.MASS", Description="Convert mass between units: KG, G, MG, LB, OZ, TON")] public static object UDF_PC_MASS([ExcelArgument(Name="number", Description="A numeric value or array of numbers")] object v, [ExcelArgument(Name="from_unit", Description="Source mass unit, e.g. KG, LB, OZ")] object from, [ExcelArgument(Name="to_unit", Description="Target mass unit, e.g. G, MG, TON")] object to)=>OutputWrapper.WrapError(()=>{var f=S(from);var t=S(to);return ElementWiseMapper.MapOver<double,double>(v,d=>PhyChemCore.ConvertMass(d,f,t));});
        [ExcelFunction(Name="PHYCHEM.C_TO_F", Description="Celsius to Fahrenheit")] public static object UDF_PC_CTOF([ExcelArgument(Name="celsius", Description="Temperature in degrees Celsius")] object v)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(v,d=>PhyChemCore.ConvertTemperature(d,"C","F")));
        [ExcelFunction(Name="PHYCHEM.F_TO_C", Description="Fahrenheit to Celsius")] public static object UDF_PC_FTOC([ExcelArgument(Name="fahrenheit", Description="Temperature in degrees Fahrenheit")] object v)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(v,d=>PhyChemCore.ConvertTemperature(d,"F","C")));
        [ExcelFunction(Name="PHYCHEM.KG_TO_LB", Description="Kilograms to pounds")] public static object UDF_PC_KG2LB([ExcelArgument(Name="kg", Description="Mass in kilograms")] object v)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(v,d=>PhyChemCore.ConvertMass(d,"KG","LB")));
        [ExcelFunction(Name="PHYCHEM.LB_TO_KG", Description="Pounds to kilograms")] public static object UDF_PC_LB2KG([ExcelArgument(Name="lb", Description="Mass in pounds")] object v)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(v,d=>PhyChemCore.ConvertMass(d,"LB","KG")));
        [ExcelFunction(Name="PHYCHEM.L_TO_GAL", Description="Liters to US gallons")] public static object UDF_PC_L2GAL([ExcelArgument(Name="liters", Description="Volume in liters")] object v)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(v,d=>PhyChemCore.ConvertVolume(d,"L","GAL")));
        [ExcelFunction(Name="PHYCHEM.GAL_TO_L", Description="US gallons to liters")] public static object UDF_PC_GAL2L([ExcelArgument(Name="gallons", Description="Volume in US gallons")] object v)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(v,d=>PhyChemCore.ConvertVolume(d,"GAL","L")));
        [ExcelFunction(Name="PHYCHEM.ATM_TO_PSI", Description="Atmospheres to PSI")] public static object UDF_PC_ATM2PSI([ExcelArgument(Name="atm", Description="Pressure in atmospheres")] object v)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(v,d=>PhyChemCore.ConvertPressure(d,"ATM","PSI")));
        [ExcelFunction(Name="PHYCHEM.PSI_TO_ATM", Description="PSI to atmospheres")] public static object UDF_PC_PSI2ATM([ExcelArgument(Name="psi", Description="Pressure in pounds per square inch")] object v)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(v,d=>PhyChemCore.ConvertPressure(d,"PSI","ATM")));
        [ExcelFunction(Name="PHYCHEM.IDEALGAS", Description="Ideal gas law: PV = nRT. Solve for any unknown (pass * for variable to solve)")] public static object UDF_PC_GAS([ExcelArgument(Name="pressure", Description="Pressure (P) in the ideal gas law PV=nRT")] object p, [ExcelArgument(Name="volume", Description="Volume (V) in ideal gas law or gas volume to convert")] object v, [ExcelArgument(Name="moles", Description="Number of moles (n) in the ideal gas law PV=nRT")] object n, [ExcelArgument(Name="temperature", Description="Temperature (T) in Kelvin for ideal gas law")] object t)=>OutputWrapper.WrapError(()=>{if(InputNormalizer.IsExcelErrorValue(p))return p;if(InputNormalizer.IsExcelErrorValue(v))return v;if(InputNormalizer.IsExcelErrorValue(n))return n;if(InputNormalizer.IsExcelErrorValue(t))return t;return PhyChemCore.IdealGasLaw(V(p),V(v),V(n),V(t));});
        // review 2026-09-14（P3 PHY-03）：错误输入统一原样传播（同 MapOver 系列），
        // 数组/标量口径一致；tUnit/pUnit 更名 t_unit/p_unit（snake_case 参数规范，UDF-05）。
        [ExcelFunction(Name="PHYCHEM.GASSTP", Description="Convert gas volume to standard temperature and pressure (STP)")] public static object UDF_PC_STP([ExcelArgument(Name="volume", Description="Volume (V) in ideal gas law or gas volume to convert")] object vol, [ExcelArgument(Name="temperature", Description="Temperature in Celsius by default; use t_unit=\"K\" for Kelvin")] object temp, [ExcelArgument(Name="pressure", Description="Pressure in atm by default; use p_unit=\"PSI\",\"KPA\", etc.")] object press, [ExcelArgument(Name="[t_unit]", Description="Temperature unit: C (default), K, F")] object tUnit=null, [ExcelArgument(Name="[p_unit]", Description="Pressure unit: atm (default), PSI, KPA, PA, BAR, MMHG, TORR")] object pUnit=null)=>OutputWrapper.WrapError(()=>{if(InputNormalizer.IsExcelErrorValue(vol))return vol;if(InputNormalizer.IsExcelErrorValue(temp))return temp;if(InputNormalizer.IsExcelErrorValue(press))return press;return PhyChemCore.GasToSTP(InputNormalizer.ToDouble(vol),InputNormalizer.ToDouble(temp),InputNormalizer.ToDouble(press),InputNormalizer.IsOmitted(tUnit)?"C":InputNormalizer.ToString(tUnit),InputNormalizer.IsOmitted(pUnit)?"atm":InputNormalizer.ToString(pUnit));});
        [ExcelFunction(Name="PHYCHEM.DENSITY", Description="Compute density: mass / volume")] public static object UDF_PC_DEN([ExcelArgument(Name="mass", Description="Mass value for density calculation")] object mass, [ExcelArgument(Name="volume", Description="Volume value for density calculation")] object vol)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOverMulti<double,double,double>(mass,vol,PhyChemCore.Density));
        private static string S(object o)=>InputNormalizer.ToString(o);
        // review 2026-09-14（模块审查 P2 PHY-01）：Excel 错误/非 "*" 文本原被静默当作
        // "待求量"（#REF! 输入返回貌似合理的解）。错误值与非占位文本必须显式失败
        // （→ #VALUE!），只有数值、空白与 "*" 占位参与求解。
        private static double? V(object o)
        {
            if (o == null || InputNormalizer.IsExcelEmptyValue(o)) return null;
            if (InputNormalizer.IsExcelErrorValue(o))
                throw new System.ArgumentException(
                    "Ideal gas parameter is an Excel error value; fix the reference before solving.");
            if (o is string s)
            {
                if (s == "*") return null;
                throw new System.ArgumentException(
                    "Ideal gas parameter must be numeric or \"*\" to mark the unknown quantity.");
            }
            double d = InputNormalizer.ToDouble(o);
            if (double.IsNaN(d))
                throw new System.ArgumentException(
                    "Ideal gas parameter is not numeric; pass a number or \"*\".");
            return d;
        }
    }
}
