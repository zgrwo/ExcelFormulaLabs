# verify-pack.ps1 - Validate ExcelDnaPack output integrity
# Usage: powershell -File verify-pack.ps1 -PublishDir <path> -Module <Analytics|DataToolkit> -Tfm <net48|net8.0-windows>
param(
    [Parameter(Mandatory=$true)] [string] $PublishDir,
    [Parameter(Mandatory=$true)] [string] $Module,
    [Parameter(Mandatory=$true)] [string] $Tfm
)

$ErrorActionPreference = "Stop"
$errors = @()
$warnings = @()

# TFM -> XLL filename suffix mapping
$tfmSuffix = if ($Tfm -eq "net48") { "net48" } else { "net8.0" }

# 1. Check packed XLLs exist and have reasonable size (>= 100 KB)
$minSize = 100 * 1024
$xllFiles = @(
    "$PublishDir\$Module-AddIn-$tfmSuffix-packed.xll",
    "$PublishDir\$Module-AddIn-$tfmSuffix-64-packed.xll"
)

foreach ($xll in $xllFiles) {
    if (-not (Test-Path $xll)) {
        $errors += "Missing packed XLL: $xll"
    } else {
        $size = (Get-Item $xll).Length
        if ($size -lt $minSize) {
            $errors += "$xll size too small: $size bytes (min $minSize)"
        } else {
            $kb = [math]::Round($size / 1024)
            $name = Split-Path $xll -Leaf
            Write-Host "  [OK] $name ($kb KB)"
        }
    }
}

# 2. Check SQLite native DLL
#    net48: embedded in DataToolkit.dll, publish copy is a filesystem fallback
#    net8.0: packed in XLL as NATIVE_LIBRARY_LZMA
if ($Module -eq "DataToolkit") {
    if ($tfmSuffix -eq "net48") {
        $nativeName = "SQLite.Interop.dll"
    } else {
        $nativeName = "e_sqlite3.dll"
    }
    $interopX86 = "$PublishDir\x86\$nativeName"
    $interopX64 = "$PublishDir\x64\$nativeName"
    if (-not (Test-Path $interopX86)) {
        $warnings += "Unpacked fallback missing: $interopX86 (packed mode is unaffected)"
    } else {
        $kb = [math]::Round((Get-Item $interopX86).Length / 1024)
        Write-Host "  [OK] x86\$nativeName ($kb KB)"
    }
    if (-not (Test-Path $interopX64)) {
        $warnings += "Unpacked fallback missing: $interopX64 (packed mode is unaffected)"
    } else {
        $kb = [math]::Round((Get-Item $interopX64).Length / 1024)
        Write-Host "  [OK] x64\$nativeName ($kb KB)"
    }
}

# 3. Check for cross-TFM contamination
#    publish 目录出现另一 TFM 的 packed.xll 说明并行内部构建互相污染（构建已有序，
#    此处兜底）。须判 error 且同时检查 32/64 两个变体。
$otherTfm = if ($tfmSuffix -eq "net48") { "net8.0" } else { "net48" }
$staleXlls = @(
    "$PublishDir\$Module-AddIn-$otherTfm-packed.xll",
    "$PublishDir\$Module-AddIn-$otherTfm-64-packed.xll"
)
foreach ($stale in $staleXlls) {
    if (Test-Path $stale) {
        $errors += "Cross-TFM stale XLL found: $stale (会反向覆盖正确产物，中止)"
    }
}

# 4. 打包依赖完整性（**仅 net48**）
#    背景：ExcelDnaPack 在 net48 下**不会**自动发现 CLR 引用闭包——只有 .dna.tpl 里显式
#    登记的才被打包。2026-10-07 实测缺陷：net48 的 tpl 漏了 System.Text.Json 及其传递依赖，
#    JSON.*（5 个函数）在真实 Excel 中全部 #VALUE!，而单元测试在进程内运行（依赖 DLL 齐备）
#    故一直全绿——只有真机加载 XLL 才暴露。
#    net8 走 deps.json 机制（XLL 内无 ASSEMBLY_LZMA 资源，名称扫描不适用），本检查不覆盖。
#
#    判据（不依赖 tpl，故能发现"需要却没登记"）：模块程序集的**传递引用闭包**中，凡是
#    存在于构建输出目录的程序集，都必须出现在 XLL 内。框架程序集不在输出目录 → 自然跳过。
$skipList = @{
    # net48 起 ValueTuple 由 mscorlib 提供（包内程序集为类型转发 facade），实测无需打包：
    # 修复后 JSON.*/XML.*/SQL.* 与其余 14 个模块函数在两个 TFM 上各 17/17 通过。
    "System.ValueTuple" = "net48 起由 mscorlib 提供（类型转发），无需打包"
}
if ($tfmSuffix -eq "net48") {
    $repoRoot = Split-Path -Parent $PSScriptRoot
    $moduleDll = Join-Path $repoRoot "src\$Module\bin\$(if ($PublishDir -match 'Release') { 'Release' } else { 'Debug' })\net48\$Module.dll"
    if (-not (Test-Path $moduleDll)) {
        $warnings += "找不到 $Module.dll（$moduleDll），跳过打包依赖完整性检查"
    } else {
        $outDir = Split-Path $moduleDll -Parent
        $inOutDir = @(Get-ChildItem $outDir -Filter *.dll | ForEach-Object { $_.BaseName })

        function Get-RefNames([string]$simpleName) {
            $p = Join-Path $outDir "$simpleName.dll"
            if (-not (Test-Path $p)) { return @() }
            try {
                return @([System.Reflection.Assembly]::ReflectionOnlyLoadFrom($p).GetReferencedAssemblies() |
                    ForEach-Object { $_.Name })
            } catch { return @() }
        }

        $seen = New-Object 'System.Collections.Generic.HashSet[string]'
        $queue = New-Object 'System.Collections.Generic.Queue[string]'
        foreach ($r in (Get-RefNames ([System.IO.Path]::GetFileNameWithoutExtension($Module)))) { $queue.Enqueue($r) }

        $missingDeps = @()
        while ($queue.Count -gt 0) {
            $n = $queue.Dequeue()
            if (-not $seen.Add($n)) { continue }
            if ($inOutDir -notcontains $n) { continue }        # 框架程序集：不在输出目录
            if ($skipList.ContainsKey($n)) { continue }
            $depDll = Join-Path $outDir "$n.dll"
            $simple = $n.ToUpperInvariant()
            foreach ($xll in $xllFiles) {
                if (-not (Test-Path $xll)) { continue }
                $ascii = [System.Text.Encoding]::ASCII.GetString([System.IO.File]::ReadAllBytes($xll))
                if (-not $ascii.Contains($simple)) {
                    $missingDeps += "$n.dll 未打包进 $(Split-Path $xll -Leaf)"
                }
            }
            foreach ($r in (Get-RefNames $n)) { $queue.Enqueue($r) }
        }

        if ($missingDeps.Count -gt 0) {
            $errors += "打包依赖缺失（运行时将抛异常 → #VALUE!）：" + ($missingDeps -join '; ') +
                       " — 在 $Module-AddIn-$tfmSuffix.dna.tpl 显式登记 <Reference Path=`"...`" Pack=`"true`" />"
        } else {
            Write-Host "  [OK] 打包依赖完整性：引用闭包在输出目录内的程序集均已打包（net48）"
        }
    }
}

# Report
if ($errors.Count -gt 0) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "  VERIFY-PACK FAILED -- $Module $Tfm" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    foreach ($e in $errors) {
        Write-Host "  ERROR: $e" -ForegroundColor Red
    }
    exit 1
}

if ($warnings.Count -gt 0) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Yellow
    Write-Host "  VERIFY-PACK PASSED (with warnings)" -ForegroundColor Yellow
    Write-Host "========================================" -ForegroundColor Yellow
    foreach ($w in $warnings) {
        Write-Host "  WARN: $w" -ForegroundColor Yellow
    }
    exit 0
}

Write-Host ""
Write-Host "  VERIFY-PACK PASSED -- $Module $Tfm" -ForegroundColor Green
exit 0
