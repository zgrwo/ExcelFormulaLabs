# scaffold-udf.ps1 — UDF 模块脚手架（ADR-0011 流程）
#
# 用法：.\scripts\scaffold-udf.ps1 -Module Analytics -Name Weather -Prefix WEATHER
#
# 生成 3 个文件，然后调用 udfgen.py 产出 UDF 声明：
#   src/<Module>/<Name>Core.cs              纯逻辑（哨兵契约 + 异常过滤器）
#   udf-metadata/<Name>Udf.json             **UDF 声明的单一真源**
#   tests/<Module>.Tests/<Name>CoreTests.cs 边界/NaN/空值测试
#   → python tools/udfgen.py generate --only <Name>Udf
#   src/<Module>/<Name>Udf.g.cs             生成物（勿手改）
#
# 不再生成手写的 *Udf.cs：属性与签名由元数据生成（见 ADR-0011）。

param(
    [Parameter(Mandatory=$true)]
    [string]$Module,       # Target module folder under src/ (e.g. Analytics, DataToolkit)

    [Parameter(Mandatory=$true)]
    [string]$Name,         # PascalCase class name (e.g. Weather, Finance)

    [Parameter(Mandatory=$true)]
    [string]$Prefix        # UDF prefix in Excel (e.g. WEATHER, FIN)
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot  # project root

# Validate module exists
$modulePath = Join-Path (Join-Path $root "src") $Module
if (-not (Test-Path $modulePath)) {
    Write-Host "[FAIL] Module directory not found: $modulePath"
    Write-Host "       Available modules:"
    Get-ChildItem (Join-Path $root "src") -Directory | ForEach-Object { Write-Host "         - $($_.Name)" }
    exit 1
}

# R5-N1 (review 2026-09-06 修复轮发现)：三参数 Join-Path（-AdditionalChildPath）为 pwsh6+ 专有——本脚本自 v2.0.0 起在 Windows PowerShell 5.1 下必然绑定失败（此前零自测未暴露），
# 已全部改为嵌套 Join-Path 双宿主兼容。
# N-E (review 2026-09-06)：$Name/$Prefix 进入文件路径与生成代码——无格式校验时
# 可路径穿越（..\..\x）或产生非法 C# 标识符。仅允许字母开头的字母数字组合。
if ($Name -notmatch '^[A-Za-z][A-Za-z0-9]*$') {
    Write-Host "[FAIL] -Name must be a PascalCase identifier (letters/digits, starting with a letter): '$Name'"
    exit 1
}
if ($Prefix -notmatch '^[A-Za-z][A-Za-z0-9]*$') {
    Write-Host "[FAIL] -Prefix must be alphanumeric (UDF prefix, e.g. WEATHER, FIN): '$Prefix'"
    exit 1
}

# 元数据类名含 $Name，若同名元数据已存在则拒绝——避免覆盖既有单一真源
$metaPath = Join-Path (Join-Path $root "udf-metadata") "$Name`Udf.json"
if (Test-Path $metaPath) {
    Write-Host "[FAIL] 元数据已存在：$metaPath"
    Write-Host "       新增函数请直接编辑该文件后运行：python tools/udfgen.py generate --only $Name`Udf"
    exit 1
}

# Template directory
$tplDir = Join-Path (Join-Path $root "templates") "NewModule"
if (-not (Test-Path $tplDir)) {
    Write-Host "[FAIL] Template directory not found: $tplDir"
    exit 1
}

# Replacement map
$replacements = @{
    '{Name}'   = $Name
    '{Module}' = $Module
    '{PREFIX}' = $Prefix.ToUpper()
}

function Expand-Template {
    param([string]$TemplateFile, [string]$OutputFile)

    if (Test-Path $OutputFile) {
        Write-Host "[SKIP] Already exists: $OutputFile"
        return
    }

    $content = Get-Content $TemplateFile -Raw -Encoding UTF8
    foreach ($key in $replacements.Keys) {
        $content = $content.Replace($key, $replacements[$key])
    }

    $outDir = Split-Path -Parent $OutputFile
    if (-not (Test-Path $outDir)) {
        New-Item -ItemType Directory -Path $outDir -Force | Out-Null
    }

    [System.IO.File]::WriteAllText($OutputFile, $content, [System.Text.UTF8Encoding]::new($false))
    Write-Host "[OK]   $OutputFile"
}

Write-Host ""
Write-Host "=== UDF Scaffold: $Name (Module=$Module, Prefix=$Prefix) ==="
Write-Host ""

Expand-Template (Join-Path $tplDir '{Name}Core.cs.template') `
                (Join-Path $modulePath "$Name`Core.cs")

Expand-Template (Join-Path $tplDir '{Name}Udf.json.template') `
                $metaPath

$testProject = Join-Path (Join-Path $root "tests") "$Module.Tests"
Expand-Template (Join-Path $tplDir '{Name}Core.Tests.cs.template') `
                (Join-Path $testProject "$Name`CoreTests.cs")

# 生成 UDF 声明（.g.cs）——属性/签名来自刚写出的元数据。
# tools/udfgen.py 不存在时**只告警不失败**：模板落盘是本脚本的核心职责，生成是便利步骤
# （治理自测的临时夹具只复制 scripts/ 与 templates/，不含 tools/）。
$udfgen = Join-Path (Join-Path $root "tools") "udfgen.py"
if (Test-Path $udfgen) {
    Write-Host ""
    Write-Host "-> python tools/udfgen.py generate --only $Name`Udf"
    Push-Location $root
    try {
        & python $udfgen generate --only "$Name`Udf"
        if ($LASTEXITCODE -ne 0) {
            Write-Host "[WARN] 生成失败——请检查 $metaPath 的 JSON 结构后重跑 generate" -ForegroundColor Yellow
        }
    } finally {
        Pop-Location
    }
} else {
    Write-Host ""
    Write-Host "[SKIP] 未找到 tools/udfgen.py，跳过 UDF 声明生成（元数据已写出：$metaPath）" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== Done. Next steps: ==="
Write-Host "  1. 实现纯逻辑：src/$Module/$Name`Core.cs（零 Excel 依赖）"
Write-Host "  2. 调整 UDF 声明：编辑 udf-metadata/$Name`Udf.json（函数名/描述/参数/分类/expr）"
Write-Host "     改完运行：python tools/udfgen.py generate"
Write-Host "  3. 补测试：tests/$Module.Tests/$Name`CoreTests.cs（期望值必须硬编码，禁自校验）"
Write-Host "  4. 数值类 UDF 补交叉验证：tests/CrossValRunner/test_manifest.json + scripts/verify-manual.py"
Write-Host "     （写法见 templates/README.md；禁止 check(name, X, X)）"
Write-Host "  5. 同步文档：api-reference.md / user-manual / project-structure.md 目录树"
Write-Host "  6. 验证：python tools/udfgen.py verify; dotnet build; dotnet test --filter $Name"
Write-Host ""
