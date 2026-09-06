// R5-P3-13 (review 2026-09-06)：测试宿主线程 culture 固定为 Invariant——格式化/解析断言
// 不再依赖运行机 locale（de-DE/FR-FR 上曾为环境敏感失败源）。个别 culture 用例仍显式
// 切换并 try/finally 复位，不受影响。
// net8：BCL 自带 ModuleInitializerAttribute；net48：内联同名 shim 属性（LangVersion=latest
// 下编译器据此生成 <Module>::.cctor，.NET Framework 4.8 运行时支持模块初始值设定项）。
using System.Globalization;
using System.Runtime.CompilerServices;

namespace ExcelFormulaLabs.Foundation.Tests
{
    internal static class TestCultureSetup
    {
        [ModuleInitializer]
        internal static void Init()
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        }
    }
}

#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    internal sealed class ModuleInitializerAttribute : Attribute { }
}
#endif
