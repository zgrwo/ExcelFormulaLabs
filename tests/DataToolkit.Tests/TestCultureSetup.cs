// R5-P3-13 (review 2026-09-06)：测试宿主线程 culture 固定为 Invariant（与
// Foundation.Tests/TestCultureSetup.cs 同款）。net48 用同名 shim 属性。
using System.Globalization;
using System.Runtime.CompilerServices;

namespace ExcelFormulaLabs.DataToolkit.Tests
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
