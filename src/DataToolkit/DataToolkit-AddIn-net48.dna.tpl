<DnaLibrary Name="DataToolkit - STR REGEX JSON SQL FS DT RANGE (.NET Framework)" RuntimeVersion="v4.0" ExplicitExports="true">
  <ExternalLibrary Path="DataToolkit.dll" Pack="true" ExplicitExports="true" />
  <Reference Path="Foundation.dll" Pack="true" />
  <Reference Path="System.Data.SQLite.dll" Pack="true" />
  <Reference Path="ExcelDna.IntelliSense.dll" Pack="true" />

  <!-- JSON.* / XML.* 依赖链（2026-10-07 修复）：
       ExcelDnaPack **不会**自动发现 CLR 引用闭包——只在 .dna 里显式列出的程序集才被打包。
       此前缺 System.Text.Json 及以下传递依赖 → net48 的 5 个 JSON.* 在真实 Excel 中
       全部抛 FileNotFoundException 被 WrapError 兜成 #VALUE!（net8 因 STJ 在共享框架内而正常）。
       真机隔离实验：同一公式 =JSON.QUERY("{""a"":{""b"":7}}","a.b") → net48 #VALUE! / net8 7。
       漏哪个都会在运行时炸，故整条闭包一并登记；缺失将由 verify-pack.ps1 的
       "声明→实际打包" 检查拦下（不再依赖人工发现）。 -->
  <Reference Path="System.Text.Json.dll" Pack="true" />
  <Reference Path="System.Text.Encodings.Web.dll" Pack="true" />
  <Reference Path="System.Memory.dll" Pack="true" />
  <Reference Path="System.Buffers.dll" Pack="true" />
  <Reference Path="System.Numerics.Vectors.dll" Pack="true" />
  <Reference Path="System.Runtime.CompilerServices.Unsafe.dll" Pack="true" />
  <Reference Path="System.Threading.Tasks.Extensions.dll" Pack="true" />
  <Reference Path="Microsoft.Bcl.AsyncInterfaces.dll" Pack="true" />

  <!-- DataToolkit (.NET Framework 4.8) — String, Regex, JSON, XML, DateTime, FileSystem, Pivot, Range Export, SQL.
       Zero runtime install required on Win10/11.
       Install: drag DataToolkit-AddIn-net48-packed.xll into Excel (asset name as in GitHub Release;
       64-bit variant: DataToolkit-AddIn-net48-64-packed.xll).
       Category prefixes: STR.*, REGEX.*, JSON.*, XML.*, DT.*, FS.*, PIVOT.*, RANGE.*, SQL.* -->
</DnaLibrary>
