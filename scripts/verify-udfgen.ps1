# verify-udfgen.ps1 — UDF 生成物与元数据一致性门禁（ADR-0011）
#
# 检查两件事：
#   ① src/**/<X>Udf.g.cs 与 udf-metadata/<X>Udf.json 重新生成的结果一致
#      （既防手改生成物，也防"改了元数据忘记重生成"）；
#   ② 保留手写的 UDF（语句体，如 *Async）在源码中存在且函数名与元数据一致。
#
# 用法：powershell -File scripts/verify-udfgen.ps1
# 退出码：0 = 通过；1 = 不一致（含 Python 缺失）
#
# 修复方式：python tools/udfgen.py generate
$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$gen = Join-Path $repoRoot "tools/udfgen.py"

if (-not (Test-Path $gen)) {
    Write-Host "[FAIL] 找不到生成器：tools/udfgen.py"
    exit 1
}

$python = Get-Command python -ErrorAction SilentlyContinue
if (-not $python) {
    Write-Host "[FAIL] 需要 Python 运行 tools/udfgen.py（见 requirements.txt）"
    exit 1
}

Push-Location $repoRoot
try {
    $out = & python $gen verify 2>&1
    $code = $LASTEXITCODE
} finally {
    Pop-Location
}

foreach ($line in $out) { Write-Host $line }

if ($code -ne 0) {
    Write-Host ""
    Write-Host "[FAIL] UDF 生成物与元数据不一致。执行以下命令修复后重试：" -ForegroundColor Red
    Write-Host "       python tools/udfgen.py generate"
    exit 1
}
Write-Host "[PASS] UDF 生成物与元数据一致。"
exit 0
