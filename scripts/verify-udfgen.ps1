# verify-udfgen.ps1 — UDF 生成物与元数据一致性门禁（ADR-0011）
#
# 检查三件事：
#   ① src/**/<X>Udf.g.cs 与 udf-metadata/<X>Udf.json 重新生成的结果一致
#      （既防手改生成物，也防"改了元数据忘记重生成"）；
#   ② 保留手写的 UDF（语句体，如 *Async）在源码中存在，且**属性级**与元数据一致：
#      Description / Category / [ExcelArgument] Name 序列 / C# 形参名（pname）序列
#      （支持属性跨行写法；pname 比对在源码侧剥离默认值，`object lambda = null` ↔ `lambda`）；
#      注意 0 生成函数的元数据文件（LinalgAsyncUdf/RegressionAsyncUdf）同样受检——
#      旧实现 `if not code: continue` 会把这两个文件整文件跳过（P2-1）；
#   ③ docs/specification/api-reference.md 的表体与元数据一致（udfgen.py verify-api）。
#
# 用法：powershell -File scripts/verify-udfgen.ps1
# 退出码：0 = 通过；1 = 任一子检查不一致（含 Python 缺失）
#
# 修复方式：python tools/udfgen.py generate && python tools/udfgen.py generate-api
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

# 两个子检查都跑（不短路）：一次运行即暴露全部不一致，避免修一个再跑一遍才发现下一个。
$failed = @()
foreach ($sub in @("verify", "verify-api")) {
    Push-Location $repoRoot
    try {
        $out = & python $gen $sub 2>&1
        $code = $LASTEXITCODE
    } finally {
        Pop-Location
    }
    Write-Host ""
    Write-Host "--- udfgen.py $sub ---"
    foreach ($line in $out) { Write-Host $line }
    if ($code -ne 0) { $failed += $sub }
}

if ($failed.Count -gt 0) {
    Write-Host ""
    Write-Host "[FAIL] 生成物/文档与元数据不一致：$($failed -join ', ')。执行以下命令修复后重试：" -ForegroundColor Red
    Write-Host "       python tools/udfgen.py generate"
    Write-Host "       python tools/udfgen.py generate-api"
    exit 1
}
Write-Host ""
Write-Host "[PASS] UDF 生成物与 api-reference 均与元数据一致。"
exit 0
