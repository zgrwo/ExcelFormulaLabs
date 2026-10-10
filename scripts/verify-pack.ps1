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

# 1. packed XLL 存在性 + **完整性**
#    绝对下限（旧判据 100 KB）抓不到**截断**产物：失败的 ExcelDnaPack 会留下部分写入的
#    xll（实测 1,168,896 / 658,944 字节，正常 3,730,432 / 1,196,544），而截断产物随后会让
#    每一次 pack 都失败在 EndUpdateResource（Win32Exception 110）且错误信息指不到真因。
#    判据改为：① PE 头可解析（MZ + e_lfanew 处 PE\0\0）；② 同模块同 TFM 的 32/64 位两个
#    产物体积比在 [0.85, 1.18] 内。区间由**实测**标定：8 个正常产物（2 模块 × 2 TFM × 2 配置）
#    比值区间为 0.941–0.994；而一次真实事故里被截断的 Analytics-AddIn-net8.0-64-packed.xll
#    （1,168,896 字节，正常约 1,668,096）比值为 0.705 —— 原先宽松的 [0.6, 1.7] **漏掉了它**。
#    另注：PE 头检查抓不到尾部截断（MZ / PE 头在文件开头仍完整），体积比才是该场景的判据。
$minSize = 256 * 1024
$xllFiles = @(
    "$PublishDir\$Module-AddIn-$tfmSuffix-packed.xll",
    "$PublishDir\$Module-AddIn-$tfmSuffix-64-packed.xll"
)
$packedSizes = @{}
foreach ($xll in $xllFiles) {
    if (-not (Test-Path $xll)) {
        $errors += "Missing packed XLL: $xll"
        continue
    }
    $size = (Get-Item $xll).Length
    $packedSizes[$xll] = $size
    $isPe = $false
    try {
        $fs = [System.IO.File]::OpenRead($xll)
        try {
            $br = New-Object System.IO.BinaryReader($fs)
            if ($br.ReadUInt16() -eq 0x5A4D) {
                $fs.Position = 0x3C
                if ($fs.Length -ge 0x40) {
                    $peOff = $br.ReadInt32()
                    if ($peOff -gt 0 -and ($peOff + 4) -le $size) {
                        $fs.Position = $peOff
                        $isPe = ($br.ReadUInt32() -eq 0x00004550)
                    }
                }
            }
        } finally { $fs.Close() }
    } catch { $isPe = $false }
    if (-not $isPe) {
        $errors += "$xll is not a valid PE image (truncated or corrupt): $size bytes"
        continue
    }
    if ($size -lt $minSize) {
        $errors += "$xll size too small: $size bytes (min $minSize)"
        continue
    }
    Write-Host "  [OK] $(Split-Path $xll -Leaf) ($([math]::Round($size / 1024)) KB)"
}
# ② 双位数产物体积比：同源构建比值接近 1，截断会显著偏离
if ($packedSizes.Count -eq 2) {
    $vals = @($packedSizes.Values | Sort-Object)
    $ratio = $vals[0] / $vals[1]
    if ($ratio -lt 0.85 -or $ratio -gt 1.18) {
        $detail = (($packedSizes.GetEnumerator() | Sort-Object Name | ForEach-Object { "$(Split-Path $_.Key -Leaf)=$($_.Value)" }) -join ', ')
        $errors += "packed XLL size mismatch between bitness variants (ratio $([math]::Round($ratio, 3))) — one of them is likely truncated: $detail"
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
