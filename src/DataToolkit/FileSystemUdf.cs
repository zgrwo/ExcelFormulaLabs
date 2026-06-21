using ExcelDna.Integration;
using ExcelVbaLibraries.Foundation;

namespace ExcelVbaLibraries.DataToolkit
{
    public static class FileSystemUdf
    {
        [ExcelFunction(Name="FS.NORM")] public static object UDF_FS_NORM(object p)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,string>(p,FileSystemCore.NormalizePath));
        [ExcelFunction(Name="FS.COMBINE")] public static object UDF_FS_COMB(object a,object b)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOverMulti<string,string,string>(a,b,(x,y)=>FileSystemCore.PathCombine(x,y)));
        [ExcelFunction(Name="FS.FNAME")] public static object UDF_FS_FNAME(object p)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,string>(p,FileSystemCore.GetFileName));
        [ExcelFunction(Name="FS.BNAME")] public static object UDF_FS_BNAME(object p)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,string>(p,FileSystemCore.GetBaseName));
        [ExcelFunction(Name="FS.EXT")] public static object UDF_FS_EXT(object p)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,string>(p,FileSystemCore.GetExtension));
        [ExcelFunction(Name="FS.FOLDER")] public static object UDF_FS_FDR(object p)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,string>(p,FileSystemCore.GetFolderPath));
        [ExcelFunction(Name="FS.FEXISTS")] public static object UDF_FS_FEX(object p)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,bool>(p,s=>FileSystemCore.FileExists(s)));
        [ExcelFunction(Name="FS.FSIZE")] public static object UDF_FS_FSZ(object p)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,long>(p,s=>FileSystemCore.GetFileSize(s)));
        [ExcelFunction(Name="FS.FDEXISTS")] public static object UDF_FS_FDEX(object p)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,bool>(p,s=>FileSystemCore.FolderExists(s)));
        [ExcelFunction(Name="FS.MKDIR")] public static object UDF_FS_MKDIR(object p)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,bool>(p,s=>FileSystemCore.EnsureFolder(s)));
        [ExcelFunction(Name="FS.LS")] public static object UDF_FS_LS(object p,object pat)=>OutputWrapper.WrapError(()=>FileSystemCore.ListFiles(InputNormalizer.ToString(p),InputNormalizer.ToString(pat)));
        [ExcelFunction(Name="FS.LSDIR")] public static object UDF_FS_LSDIR(object p,object pat)=>OutputWrapper.WrapError(()=>FileSystemCore.ListFolders(InputNormalizer.ToString(p),InputNormalizer.ToString(pat)));
        [ExcelFunction(Name="FS.READ")] public static object UDF_FS_READ(object p)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,string>(p,s=>FileSystemCore.ReadTextFile(s)));
        [ExcelFunction(Name="FS.WRITE")] public static object UDF_FS_WRITE(object p,object c)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOverMulti<string,string,bool>(p,c,(a,b)=>FileSystemCore.WriteTextFile(a,b)));
        [ExcelFunction(Name="FS.APPEND")] public static object UDF_FS_APPEND(object p,object c)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOverMulti<string,string,bool>(p,c,(a,b)=>FileSystemCore.AppendTextFile(a,b)));
        [ExcelFunction(Name="FS.COPY")] public static object UDF_FS_COPY(object s,object d)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOverMulti<string,string,bool>(s,d,(a,b)=>FileSystemCore.CopyFile(a,b)));
        [ExcelFunction(Name="FS.MOVE")] public static object UDF_FS_MOVE(object s,object d)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOverMulti<string,string,bool>(s,d,(a,b)=>FileSystemCore.MoveFile(a,b)));
        [ExcelFunction(Name="FS.DELETE")] public static object UDF_FS_DEL(object p)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,bool>(p,s=>FileSystemCore.DeleteFile(s)));
        [ExcelFunction(Name="FS.DELDIR")] public static object UDF_FS_DELDIR(object p)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,bool>(p,s=>FileSystemCore.DeleteFolder(s,false)));
        [ExcelFunction(Name="FS.DRIVES")] public static object UDF_FS_DRVS()=>OutputWrapper.WrapError(()=>FileSystemCore.GetDrives());
        [ExcelFunction(Name="FS.PWD")] public static object UDF_FS_PWD()=>OutputWrapper.WrapError(()=>(object)FileSystemCore.GetCurrentFolder());
        [ExcelFunction(Name="FS.TEMP")] public static object UDF_FS_TEMP()=>OutputWrapper.WrapError(()=>(object)FileSystemCore.GetTempPath());
    }
}
