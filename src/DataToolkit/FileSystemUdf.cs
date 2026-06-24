using ExcelDna.Integration;
using ExcelVbaLibraries.Foundation;

namespace ExcelVbaLibraries.DataToolkit
{
    public static class FileSystemUdf
    {
        [ExcelFunction(Name="FS.NORM", Description="Normalize file path (slashes, resolve . and ..)")] public static object UDF_FS_NORM([ExcelArgument("path")] object p)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,string>(p,FileSystemCore.NormalizePath));
        [ExcelFunction(Name="FS.COMBINE", Description="Combine path parts into a full path")] public static object UDF_FS_COMB([ExcelArgument("a")] object a, [ExcelArgument("b")] object b)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOverMulti<string,string,string>(a,b,(x,y)=>FileSystemCore.PathCombine(x,y)));
        [ExcelFunction(Name="FS.FNAME", Description="Get file name from path")] public static object UDF_FS_FNAME([ExcelArgument("path")] object p)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,string>(p,FileSystemCore.GetFileName));
        [ExcelFunction(Name="FS.BNAME", Description="Get file name without extension")] public static object UDF_FS_BNAME([ExcelArgument("path")] object p)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,string>(p,FileSystemCore.GetBaseName));
        [ExcelFunction(Name="FS.EXT", Description="Get file extension (with dot)")] public static object UDF_FS_EXT([ExcelArgument("path")] object p)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,string>(p,FileSystemCore.GetExtension));
        [ExcelFunction(Name="FS.FOLDER", Description="Get parent folder path")] public static object UDF_FS_FDR([ExcelArgument("path")] object p)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,string>(p,FileSystemCore.GetFolderPath));
        [ExcelFunction(Name="FS.FEXISTS", Description="Returns TRUE if file exists")] public static object UDF_FS_FEX([ExcelArgument("path")] object p)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,bool>(p,s=>FileSystemCore.FileExists(s)));
        [ExcelFunction(Name="FS.FSIZE", Description="File size in bytes")] public static object UDF_FS_FSZ([ExcelArgument("path")] object p)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,long>(p,s=>FileSystemCore.GetFileSize(s)));
        [ExcelFunction(Name="FS.FDEXISTS", Description="Returns TRUE if folder exists")] public static object UDF_FS_FDEX([ExcelArgument("path")] object p)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,bool>(p,s=>FileSystemCore.FolderExists(s)));
        [ExcelFunction(Name="FS.MKDIR", Description="Create folder (including parents)")] public static object UDF_FS_MKDIR([ExcelArgument("path")] object p)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,bool>(p,s=>FileSystemCore.EnsureFolder(s)));
        [ExcelFunction(Name="FS.LS", Description="List files in a folder matching a wildcard pattern")] public static object UDF_FS_LS([ExcelArgument("path")] object p, [ExcelArgument("pattern")] object pat)=>OutputWrapper.WrapError(()=>FileSystemCore.ListFiles(InputNormalizer.ToString(p),InputNormalizer.ToString(pat)));
        [ExcelFunction(Name="FS.LSDIR", Description="List sub-folders matching a wildcard pattern")] public static object UDF_FS_LSDIR([ExcelArgument("path")] object p, [ExcelArgument("pattern")] object pat)=>OutputWrapper.WrapError(()=>FileSystemCore.ListFolders(InputNormalizer.ToString(p),InputNormalizer.ToString(pat)));
        [ExcelFunction(Name="FS.READ", Description="Read text file content as string")] public static object UDF_FS_READ([ExcelArgument("path")] object p)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,string>(p,s=>FileSystemCore.ReadTextFile(s)));
        [ExcelFunction(Name="FS.WRITE", Description="Write content to a text file (overwrites)")] public static object UDF_FS_WRITE([ExcelArgument("path")] object p, [ExcelArgument("content")] object c)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOverMulti<string,string,bool>(p,c,(a,b)=>FileSystemCore.WriteTextFile(a,b)));
        [ExcelFunction(Name="FS.APPEND", Description="Append content to a text file")] public static object UDF_FS_APPEND([ExcelArgument("path")] object p, [ExcelArgument("content")] object c)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOverMulti<string,string,bool>(p,c,(a,b)=>FileSystemCore.AppendTextFile(a,b)));
        [ExcelFunction(Name="FS.COPY", Description="Copy a file from source to destination")] public static object UDF_FS_COPY([ExcelArgument("src")] object s, [ExcelArgument("dest")] object d)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOverMulti<string,string,bool>(s,d,(a,b)=>FileSystemCore.CopyFile(a,b)));
        [ExcelFunction(Name="FS.MOVE", Description="Move/rename a file")] public static object UDF_FS_MOVE([ExcelArgument("src")] object s, [ExcelArgument("dest")] object d)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOverMulti<string,string,bool>(s,d,(a,b)=>FileSystemCore.MoveFile(a,b)));
        [ExcelFunction(Name="FS.DELETE", Description="Delete a file permanently")] public static object UDF_FS_DEL([ExcelArgument("path")] object p)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,bool>(p,s=>FileSystemCore.DeleteFile(s)));
        [ExcelFunction(Name="FS.DELDIR", Description="Delete a folder and all its contents")] public static object UDF_FS_DELDIR([ExcelArgument("path")] object p)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<string,bool>(p,s=>FileSystemCore.DeleteFolder(s,true)));
        [ExcelFunction(Name="FS.DRIVES", Description="List all logical drive letters")] public static object UDF_FS_DRVS()=>OutputWrapper.WrapError(()=>FileSystemCore.GetDrives());
        [ExcelFunction(Name="FS.PWD", Description="Current working directory")] public static object UDF_FS_PWD()=>OutputWrapper.WrapError(()=>(object)FileSystemCore.GetCurrentFolder());
        [ExcelFunction(Name="FS.TEMP", Description="Path to the system temporary folder")] public static object UDF_FS_TEMP()=>OutputWrapper.WrapError(()=>(object)FileSystemCore.GetTempPath());
    }
}
