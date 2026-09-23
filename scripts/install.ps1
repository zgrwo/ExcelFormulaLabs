#Requires -Version 5.1
<#
.SYNOPSIS
    ExcelFormulaLabs 一键安装 / 卸载脚本（Windows PowerShell 5.1+）。
.DESCRIPTION
    安装流程：
      1. 校验 .xll（可选 SHA-256 与 Release 的 SHA256SUMS.txt 对账）；
      2. 复制到稳定目录（默认 %LOCALAPPDATA%\ExcelFormulaLabs，重启 Excel 不失效）；
      3. Unblock-File 解除「来自其他计算机」锁定（否则 Excel 拒绝加载）；
      4. 注册到 Excel 加载项列表（HKCU\Software\Microsoft\Office\<ver>\Excel\Options 的 OPEN/OPENn）。
    卸载（-Uninstall）：移除指向安装目录的注册项并删除安装目录。

    脚本只写 HKCU（当前用户），不需要管理员权限；不修改系统级注册表。
.PARAMETER XllPath
    下载的 .xll 路径（如 Analytics-AddIn-net48-64-packed.xll）。
.PARAMETER InstallDir
    安装目录，默认 %LOCALAPPDATA%\ExcelFormulaLabs。
.PARAMETER ExpectedSha256
    期望的 SHA-256（与 Release 资产 SHA256SUMS.txt 对账；不提供则跳过校验并打印实际值）。
.PARAMETER NoRegister
    只复制/解锁，不写 Excel 加载项注册表（用于排查或手动加载）。
.PARAMETER Uninstall
    卸载：移除注册项 + 删除安装目录。
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts/install.ps1 .\Analytics-AddIn-net48-64-packed.xll
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts/install.ps1 .\DataToolkit-AddIn-net8.0-64-packed.xll -ExpectedSha256 ABCD...
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts/install.ps1 -Uninstall
#>
param(
    [Parameter(Position = 0)][string]$XllPath,
    [string]$InstallDir = (Join-Path $env:LOCALAPPDATA "ExcelFormulaLabs"),
    [string]$ExpectedSha256,
    [switch]$NoRegister,
    [switch]$Uninstall
)

$ErrorActionPreference = "Stop"

function Get-ExcelOptionsKeys {
    # 返回按 Office 版本降序的 Excel\Options 注册表键（16.0 = 2016+，15.0 = 2013）。
    $base = "HKCU:\Software\Microsoft\Office"
    if (-not (Test-Path $base)) { return @() }
    $keys = @()
    foreach ($v in @(Get-ChildItem $base -ErrorAction SilentlyContinue)) {
        if ($v.PSChildName -match '^\d+\.\d+$') {
            $k = Join-Path $v.PSPath "Excel\Options"
            if (Test-Path $k) {
                $keys += [PSCustomObject]@{ Version = [version]$v.PSChildName; Key = $k }
            }
        }
    }
    return @($keys | Sort-Object -Property Version -Descending)
}

function Register-Xll {
    param([string]$Path)
    $keys = Get-ExcelOptionsKeys
    if ($keys.Count -eq 0) {
        Write-Host "  [WARN] 未找到 Excel 加载项注册表键（Excel 尚未启动过？）" -ForegroundColor Yellow
        Write-Host "         请手动加载：Excel → 文件 → 选项 → 加载项 → 转到 → 浏览 → 选择 $Path" -ForegroundColor Yellow
        return $false
    }
    $target = $keys[0]
    $props = Get-ItemProperty -Path $target.Key
    foreach ($p in @($props.PSObject.Properties)) {
        if ($p.Name -match '^OPEN\d*$' -and "$($p.Value)" -like "*$Path*") {
            Write-Host "  [OK] 已注册（Excel $($target.Version)）：$($p.Name) = $($p.Value)" -ForegroundColor Green
            return $true
        }
    }
    # 找第一个空位：OPEN, OPEN1, OPEN2, ...
    $name = "OPEN"; $i = 1
    while ($null -ne $props.PSObject.Properties[$name]) { $name = "OPEN$i"; $i++ }
    Set-ItemProperty -Path $target.Key -Name $name -Value "/A `"$Path`""
    Write-Host "  [OK] 已注册（Excel $($target.Version)）：$name = /A `"$Path`"" -ForegroundColor Green
    return $true
}

function Unregister-Xll {
    param([string]$Dir)
    $removed = 0
    foreach ($k in Get-ExcelOptionsKeys) {
        $props = Get-ItemProperty -Path $k.Key
        foreach ($p in @($props.PSObject.Properties)) {
            if ($p.Name -match '^OPEN\d*$' -and "$($p.Value)" -like "*$Dir*") {
                Remove-ItemProperty -Path $k.Key -Name $p.Name
                Write-Host "  [OK] 已移除注册项（Excel $($k.Version)）：$($p.Name)" -ForegroundColor Green
                $removed++
            }
        }
    }
    return $removed
}

Write-Host ""
Write-Host "===== ExcelFormulaLabs 安装程序 =====" -ForegroundColor Cyan

if ($Uninstall) {
    $n = Unregister-Xll $InstallDir
    if (Test-Path $InstallDir) {
        Remove-Item -Recurse -Force $InstallDir
        Write-Host "  [OK] 已删除安装目录：$InstallDir" -ForegroundColor Green
    }
    Write-Host ""
    Write-Host "卸载完成（移除 $n 个注册项）。请重启 Excel 使变更生效。" -ForegroundColor Green
    exit 0
}

if (-not $XllPath) {
    throw "请提供 .xll 路径：powershell -File scripts/install.ps1 <path\to\xxx-packed.xll>（卸载用 -Uninstall）"
}
if (-not (Test-Path -LiteralPath $XllPath -PathType Leaf)) { throw "文件不存在：$XllPath" }
$src = (Resolve-Path -LiteralPath $XllPath).Path
if ([System.IO.Path]::GetExtension($src) -ne ".xll") { throw "不是 .xll 文件：$src" }

$srcHash = (Get-FileHash -LiteralPath $src -Algorithm SHA256).Hash
if ($ExpectedSha256) {
    $expect = ($ExpectedSha256 -replace '\s', '').ToUpperInvariant()
    if ($srcHash -ne $expect) {
        throw "SHA-256 不匹配：实际 $srcHash，期望 $expect（文件可能损坏或被篡改，已中止）"
    }
    Write-Host "  [OK] SHA-256 校验通过：$srcHash" -ForegroundColor Green
} else {
    Write-Host "  [INFO] SHA-256（可与 Release 的 SHA256SUMS.txt 对账）：$srcHash"
}

New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
$dest = Join-Path $InstallDir (Split-Path $src -Leaf)
Copy-Item -LiteralPath $src -Destination $dest -Force
Unblock-File -LiteralPath $dest
Write-Host "  [OK] 已复制并解除锁定：$dest" -ForegroundColor Green

if (-not $NoRegister) {
    [void](Register-Xll $dest)
} else {
    Write-Host "  [SKIP] 按 -NoRegister 跳过注册；可手动加载：$dest" -ForegroundColor DarkYellow
}

if (Get-Process EXCEL -ErrorAction SilentlyContinue) {
    Write-Host "  [WARN] 检测到 Excel 正在运行——请重启 Excel 使加载项生效。" -ForegroundColor Yellow
} else {
    Write-Host "  [OK] 打开 Excel 即自动加载（首次可能提示「启用」）。" -ForegroundColor Green
}
Write-Host ""
Write-Host "完成。卸载：powershell -File scripts/install.ps1 -Uninstall" -ForegroundColor Cyan
