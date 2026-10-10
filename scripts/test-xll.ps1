# test-xll.ps1 — XLL 真机加载门禁：在真实 Excel 中注册 Release 产物并逐条断言 UDF 行为。
#
# 为什么需要它：单元测试 / 交叉验证 / 打包检查都在**进程内**跑（依赖 DLL 齐备），
# 看不到"程序集没打进 XLL"这类缺陷。2026-10-07 实例：net48 版 DataToolkit 的 5 个
# JSON.* 在真实 Excel 中全部 #VALUE!（System.Text.Json.dll 未打进 XLL），
# 而 2889 个单测 + 523 项交叉验证 + 27 项文档检查 + 4/4 打包检查全绿。
# 只有真机加载 XLL 才暴露——本脚本就是这类缺陷的唯一拦截点。
#
# 退出码（CI 必须区分，不得把 2 当成功）：
#   0 = 全部断言通过，且**实际执行的断言数 == 计划数**
#   1 = 存在断言失败（含 XLL 加载失败、断言值不符、单元格无法写入）
#   2 = 环境不可用：无 Excel / 缺 Release 产物 / 执行数少于计划数（例如 XLL 被整体跳过）
#   —— 绝不允许"0 个断言执行却 exit 0"：执行数 < 计划数一律 2，并在末尾明确说明原因。
#
# 用法：
#   powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-xll.ps1
#   powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-xll.ps1 -BaseDir D:\some\artifacts -Rounds 1
#
# 参数：
#   -BaseDir        产物根目录；默认 = <仓库根>\src（本仓库 Release 产物所在处）。
#                   每个 XLL 按两种布局依次探测：
#                     ① 仓库布局 <BaseDir>\<Module>\bin\<Configuration>\<Tfm>\publish\<File>
#                     ② 扁平布局 <BaseDir>\<Tfm>\<File>（旧"已编译文件"目录风格：net48\、net8.0-windows\）
#   -Configuration  Debug | Release，默认 Release（门禁只认 Release 产物）。
#   -Rounds         每个 XLL 重复"启动 Excel → 注册 → 断言 → 退出"的轮数，默认 4
#                   （覆盖重复加载 / 卸载稳定性；-Rounds 1 用于快速本地排查）。
#
# 前置条件：
#   ① 产物已构建：dotnet build ExcelFormulaLabs.sln -c Release -m:1
#   ② 本机装有**桌面版** Excel（64 位）；无 Excel 或无产物 → exit 2
#   ③ 32 位 Excel 无法加载 64 位 XLL → 加载失败记 exit 1（请改用 64 位 Excel）
#
# 在 CI / self-hosted runner 上使用：
#   - 必须挂到**装有桌面版 Excel 的 Windows self-hosted runner**（GitHub 托管 runner 无 Excel）。
#     runs-on 标签（如 [self-hosted, windows, excel]）由维护者决定；本仓库当前**未**把它接入
#     .github/workflows/ci.yml —— 是否接入、挂哪个 runner 是维护者决策，本脚本只保证自身具备门禁语义。
#   - job 步骤示例（务必让非 0 退出码直接失败，**不要** continue-on-error）：
#       - run: dotnet build ExcelFormulaLabs.sln -c Release -m:1
#       - run: powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-xll.ps1
#         # exit 0 = 通过；1 = 断言失败；2 = 环境不可用（runner 无 Excel / 无产物）
#   - runner 上确实没有 Excel 时，正确做法是**用 runner 标签筛掉这个 job**，
#     而不是吞掉 exit 2——吞掉等于把这道门禁关掉。
#   - 耗时：每个 XLL 独占一个 Excel 进程（避免同名函数互相顶替），共 4 个 XLL × Rounds 轮；
#     实测 4 轮 ≈ 60–100 秒（含 16 次 Excel 启动）。Excel 首次启动可能弹许可/激活对话框，
#     请在 runner 上预先激活；无桌面会话的 runner（服务账户）无法跑 GUI Excel。
#
# 设计要点：
#   ① **一个 XLL 一个 Excel 进程**：net48 与 net8.0 变体导出同名函数，同进程内注册会互相顶替
#      （后注册者生效），那样被顶替的那个变体等于没测 —— 正是本脚本要防的假绿。
#   ② 期望值全部**硬编码**（含错误路径），禁止 check(X, X) 式自校验。
#   ③ 数值比较用 Cell.Value2（不用 .Text：列宽会让 1704067200 显示成 1.7E+09）。
#   ④ 多单元格返回值用 CSE 数组公式（Range.FormulaArray）落在精确尺寸的区间上——
#      不依赖动态数组溢出（旧版 Excel 无溢出），也让"数组 → 区间"封送路径真正被测到。
#   ⑤ 错误值用 SCODE 比较（#NUM! = -2146826252 …），不比对 .Text，避免非英文版 Excel 误判。
#   ⑥ 脚本会结束本机**所有** EXCEL.EXE 进程（与旧脚本一致，用于清掉僵尸进程）——
#      请勿在有未保存工作簿的 Excel 上运行。
#   ⑦ 注册表：Application.RegisterXLL 只在当前会话注册，**不写** HKCU\...\Excel\Options 的
#      OPEN* 值，故本脚本不产生需要清理的注册表残留；脚本只做只读探测并在发现别的自动加载项
#      （可能顶替被测函数 → 假绿）时告警。

[CmdletBinding()]
param(
    [string] $BaseDir,
    [ValidateSet('Debug', 'Release')] [string] $Configuration = 'Release',
    [ValidateRange(1, 20)] [int] $Rounds = 4
)

$ErrorActionPreference = 'Stop'

# 退出码语义（见头注）
$EXIT_PASS = 0
$EXIT_FAIL = 1
$EXIT_ENV  = 2

# 全局兜底：任何未预料的终止性错误（含 COM 崩溃）也要清掉残留 EXCEL.EXE 再退出，
# 且退出码绝不为 0——门禁语义下"脚本自己炸了"必须红（这里硬编码 1，不依赖变量是否已赋值）。
trap {
    Write-Host ""
    Write-Host "[FAIL] 脚本异常终止：$_" -ForegroundColor Red
    # 兜底清理：函数可能尚未定义（错误发生在脚本极早期），失败则退回内联杀进程
    try { Clear-ExcelCom -Xl $null -Wb $null } catch { }
    try { Get-Process -Name 'EXCEL' -ErrorAction SilentlyContinue | ForEach-Object { try { $_.Kill() } catch { } } } catch { }
    exit 1
}

# 错误值 SCODE（Excel 常量 XlCVError 的 SCODE 形式；-2146828288 + errNum）
$ErrorCode = @{
    '#NULL!'  = -2146826288
    '#DIV/0!' = -2146826281
    '#VALUE!' = -2146826273
    '#REF!'   = -2146826265
    '#NAME?'  = -2146826259
    '#NUM!'   = -2146826252
    '#N/A'    = -2146826246
}

$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($BaseDir)) { $BaseDir = Join-Path $repoRoot 'src' }
# 必须转成绝对路径：Excel 的 RegisterXLL 按 **Excel 自己的工作目录**解析相对路径，
# 传相对路径时 Test-Path 找得到、注册却返回 False（实测踩到）。
if (-not [System.IO.Path]::IsPathRooted($BaseDir)) {
    $BaseDir = [System.IO.Path]::GetFullPath((Join-Path (Get-Location).ProviderPath $BaseDir))
}

# ============================================================================
# 用例表（期望值硬编码；来源：docs/specification/api-reference.md 与各 Core 单测）
# ============================================================================
# 字段：Name 断言名；Formula 公式（Excel 英文公式语法）；Expect 期望值
#       （标量 / 一维 @(1,2,3) = 1×3 行 / 二维 @(@(1,2),@(3,4)) = 2×2）；
#       Tol 数值容差（相对 + 绝对）；Async 异步 UDF（读取前轮询等待回填）。
#       返回矩阵尺寸由 Expect 形状推导；尺寸 > 1 时用 CSE 数组公式写入该尺寸区间。
function New-Case {
    param(
        [string] $Name,
        [string] $Formula,
        $Expect,
        [double] $Tol = 1e-9,
        [switch] $Async
    )
    # 归一化期望为二维网格 $Grid[$r][$c]：
    #   @(@(1,2),@(3,4)) → 2×2；@(1,2,3) → 1×3（一维数组返回值按行铺开）；3.0 → 1×1
    # 注意 PowerShell 会展开单元素嵌套数组（@(@(1,2,3)) 就等于 @(1,2,3)），
    # 故一律按"元素本身是不是数组"判断，不依赖嵌套层数。
    if ($Expect -is [array]) {
        if ($Expect.Count -gt 0 -and $Expect[0] -is [array]) {
            $grid = $Expect
            $rows = $Expect.Count
            $cols = $Expect[0].Count
        } else {
            $grid = , $Expect
            $rows = 1
            $cols = $Expect.Count
        }
    } else {
        $grid = , @($Expect)
        $rows = 1
        $cols = 1
    }
    @{ Name = $Name; Formula = $Formula; Grid = $grid; Rows = $rows; Cols = $cols; Tol = $Tol; Async = [bool]$Async }
}

# SOLVE.* 历史表：与 tests/Analytics.Tests/SolveCoreTests.cs 同构（刻意非共线，避免秩亏 #VALUE!）
#   IncomingA = 1..10，VariableU1 = (3a mod 10)+2，OutputY1 = 2 + 0.5*IncomingA + 1.5*VariableU1（精确线性）
$SolveData = '{"IncomingA","VariableU1","OutputY1";1,5,10;2,8,15;3,11,20;4,4,10;5,7,15;6,10,20;7,3,10;8,6,15;9,9,20;10,2,10}'

$AnalyticsCases = @(
    (New-Case 'STATS.MEAN'          '=STATS.MEAN({1,2,3,4,5})'                 3.0),
    (New-Case 'STATS.STDEVP'        '=STATS.STDEVP({2,4,4,4,5,5,7,9})'         2.0),
    (New-Case 'STATS.PERCENTILE'    '=STATS.PERCENTILE({1,2,3,4},50)'          2.5),
    (New-Case 'STATS.IQR'           '=STATS.IQR({1,2,3,4,5})'                  2.0),
    (New-Case 'STATS.PEARSON'       '=STATS.PEARSON({1,2,3},{2,4,6})'          1.0),
    # 总体标准差口径（z = (x-mean)/σ_pop），与 StatsCoreTests.ZScore_tiny_scale 同值
    (New-Case 'STATS.ZSCORE'        '=STATS.ZSCORE({1,2,3})'                   @(@(-1.224744871391589, 0.0, 1.224744871391589))),
    # [n, mean, stdev, min, q1, median, q3, max, iqr]
    (New-Case 'STATS.SUMMARY'       '=STATS.SUMMARY({1,2,3,4,5})'              @(@(5.0, 3.0, 1.5811388300841898, 1.0, 2.0, 3.0, 4.0, 5.0, 2.0))),
    (New-Case 'STATS.SQRT'          '=STATS.SQRT({1,4,9})'                     @(@(1.0, 2.0, 3.0))),
    (New-Case 'PHYCHEM.C_TO_F'      '=PHYCHEM.C_TO_F(100)'                     212.0),
    (New-Case 'PHYCHEM.MOLWT'       '=PHYCHEM.MOLWT("H2SO4")'                  98.078),
    # 反解模式：* 表示该量待求 → n = PV/(RT) = 22.4/(0.082057*273.15)
    (New-Case 'PHYCHEM.IDEALGAS 反解' '=PHYCHEM.IDEALGAS(1,22.4,"*",273.15)'   0.9993767482825352),
    # 错误路径：四个参数全给数值 → 待求量不唯一 → #NUM!
    (New-Case 'PHYCHEM.IDEALGAS 错误路径' '=PHYCHEM.IDEALGAS(1,22.4,1,273.15)' '#NUM!'),
    (New-Case 'LINALG.DET'          '=LINALG.DET({1,2;3,4})'                   (-2.0)),
    (New-Case 'LINALG.TRACE'        '=LINALG.TRACE({1,2;3,4})'                 5.0),
    (New-Case 'LINALG.SOLVE'        '=LINALG.SOLVE({2,0;0,4},{2;8})'           @(@(1.0, 2.0))),
    # 错误路径：奇异方程组 → #VALUE!（文档：病态/奇异请改用 PINV）
    (New-Case 'LINALG.SOLVE 奇异'   '=LINALG.SOLVE({1,2;2,4},{1;2})'           '#VALUE!'),
    (New-Case 'LINALG.MATMUL'       '=LINALG.MATMUL({1,2;3,4},{5,6;7,8})'      @(@(19.0, 22.0), @(43.0, 50.0))),
    # 错误路径：非正定矩阵 → #VALUE!
    (New-Case 'LINALG.CHOLESKY 非正定' '=LINALG.CHOLESKY({1,2;2,1})'           '#VALUE!'),
    # 异步 UDF：先返回 #N/A 再回填；1 维数组按行铺开，故取 [1,2]（= 解向量第二个元素）
    (New-Case 'LINALG.SOLVE_ASYNC'  '=INDEX(LINALG.SOLVE_ASYNC({2,0;0,4},{2;8}),1,2)' 2.0 -Async),
    (New-Case 'REGRESS.RSQ'         '=REGRESS.RSQ({1;2;3},{1;2;3})'            1.0),
    (New-Case 'DOE.PLAN 全因子'     '=DOE.PLAN(2,2,0,2,"full",FALSE)'          @(@('StdOrder', 'RunOrder', 'A', 'B'), @(1.0, 1.0, -1.0, -1.0), @(2.0, 2.0, 1.0, -1.0), @(3.0, 3.0, -1.0, 1.0), @(4.0, 4.0, 1.0, 1.0))),
    # 错误路径：因子数 0 非法 → #VALUE!
    (New-Case 'DOE.PLAN 参数非法'   '=DOE.PLAN(0,2,0,2,"full",FALSE)'          '#VALUE!'),
    # 正向预测：y = 2 + 0.5*6 + 1.5*7 = 15.5
    (New-Case 'SOLVE.PREDICT'       "=SOLVE.PREDICT($SolveData,{6,7},""linear"")" 15.5),
    (New-Case 'SOLVE.EQUATION'      "=SOLVE.EQUATION($SolveData,""linear"")"   @(@('输出', '类型', '表达式'), @('OutputY1', '前向方程', 'OutputY1 = 2 + 0.5*IncomingA + 1.5*VariableU1'))),
    # 精确线性模型 → LOO 交叉验证 R²=1、MAE≈0（浮点噪声 1e-15 量级，容差 1e-9）
    (New-Case 'SOLVE.QUALITY'       "=SOLVE.QUALITY($SolveData,""linear"")"    @(@('输出', '候选', 'CV方案', 'CV_R2', 'CV_MAE', '选用'), @('OutputY1', 'linear', 'LOO', 1.0, 0.0, '是')))
)

$DataToolkitCases = @(
    (New-Case 'STR.REVERSE'         '=STR.REVERSE("hello")'                    'olleh'),
    (New-Case 'STR.TITLE'           '=STR.TITLE("hello world")'                'Hello World'),
    (New-Case 'STR.LEVENSHTEIN'     '=STR.LEVENSHTEIN("kitten","sitting")'     3.0),
    (New-Case 'STR.EXTRACT'         '=STR.EXTRACT("a[b]c[d]","[","]",-1)'      'd'),
    (New-Case 'REGEX.TEST'          '=REGEX.TEST("abc123","\d+")'              $true),
    (New-Case 'REGEX.MATCH'         '=REGEX.MATCH("Order #12345","\d+")'       '12345'),
    (New-Case 'REGEX.MATCH 第2个'   '=REGEX.MATCH("a1b2","\d+",FALSE,2)'       '2'),
    (New-Case 'REGEX.REPLACE'       '=REGEX.REPLACE("a1b2","\d","#")'          'a#b#'),
    (New-Case 'REGEX.SPLIT'         '=REGEX.SPLIT("a,b;c","[,;]")'             @(@('a', 'b', 'c'))),
    # 错误路径：非法正则 → #VALUE!
    (New-Case 'REGEX.TEST 非法模式'  '=REGEX.TEST("abc","(")'                  '#VALUE!'),
    # ↓↓↓ 本会话失效面（net48 未打包 System.Text.Json → 全族 #VALUE!）的回归守卫
    (New-Case 'JSON.QUERY'          '=JSON.QUERY("{""a"":{""b"":7}}","a.b")'   7.0),
    (New-Case 'JSON.QUERY 数组索引'  '=JSON.QUERY("{""items"":[10,20,30]}","items[1]")' 20.0),
    # 2026-10-10（用户手册实例逐一核对）：手册 12-json-xml.md 的示例用**裸整数下标**
    # （"0.Name"），旧实现只认 "[0]" → 该段被静默跳过 → 返回整段数组 → #VALUE!。
    # 真机回归守卫：裸整数与方括号两种写法都必须给出同样的值。
    (New-Case 'JSON.QUERY 裸整数下标'  '=JSON.QUERY("[{""Name"":""Alice""}]","0.Name")' 'Alice'),
    (New-Case 'JSON.QUERY 裸下标嵌套'  '=JSON.QUERY("{""a"":[{""b"":9}]}","a.0.b")' 9.0),
    (New-Case 'JSON.QUERY 方括号等价'  '=JSON.QUERY("[{""Name"":""Alice""}]","[0].Name")' 'Alice'),
    (New-Case 'JSON.VALIDATE 合法'   '=JSON.VALIDATE("{""a"":1}")'             $true),
    (New-Case 'JSON.VALIDATE 非法'   '=JSON.VALIDATE("{a:1}")'                 $false),
    (New-Case 'JSON.TOTABLE'        '=JSON.TOTABLE("[{""a"":1},{""a"":2}]")'   @(@('a'), @(1.0), @(2.0))),
    (New-Case 'XML.XPATH'           '=XML.XPATH("<r><a>1</a><a>2</a></r>","//a")' @(@('1', '2'))),
    (New-Case 'XML.VALIDATE 合法'    '=XML.VALIDATE("<r><a>1</a></r>")'        $true),
    (New-Case 'XML.VALIDATE 非法'    '=XML.VALIDATE("<r><a>1</r>")'            $false),
    # SQL 源数据落在 R1:S4（见 Initialize-Sheet），返回含表头行
    (New-Case 'SQL.QUERY'           '=SQL.QUERY($R$1:$S$4,"SELECT name, score FROM data ORDER BY score DESC",TRUE)' @(@('name', 'score'), @('b', 30.0), @('c', 20.0), @('a', 10.0))),
    (New-Case 'SQL.QUERY 聚合'      '=SQL.QUERY($R$1:$S$4,"SELECT SUM(score) AS total FROM data",TRUE)' @(@('total'), @(60.0))),
    # 错误路径：只读契约（DDL 被拒）→ #VALUE!
    (New-Case 'SQL.QUERY 只读契约'   '=SQL.QUERY($R$1:$S$4,"DROP TABLE data")'  '#VALUE!'),
    # 2026-10-10 审查 C-1 回归守卫：字面量内含关键字不得误拒（旧实现整条 → #VALUE!）。
    # R1:S4 中无 'do not delete' → 仅返回表头行。
    (New-Case 'SQL.QUERY 字面量含关键字' '=SQL.QUERY($R$1:$S$4,"SELECT name FROM data WHERE name = ''do not delete''",TRUE)' @(@('name'))),
    (New-Case 'ARR.SORTNUM'         '=ARR.SORTNUM({3,1,2})'                    @(@(1.0, 2.0, 3.0))),
    (New-Case 'ARR.INDEXOF'         '=ARR.INDEXOF({5,6,7},6)'                  1.0),
    (New-Case 'ARR.INDEXOF 未找到'   '=ARR.INDEXOF({1,2},9)'                   (-1.0)),
    (New-Case 'DICT.INTERSECT'      '=DICT.INTERSECT({1,2,3},{2,3,4})'         @(@(2.0, 3.0))),
    (New-Case 'DICT.FREQUENCY'      '=DICT.FREQUENCY({1,"",2,1})'              @(@(1.0, 2.0), @($null, 1.0), @(2.0, 1.0))),
    # has_headers 是兼容参数（无效果），CSV 恒导出全部行且每行以 CRLF 结尾
    (New-Case 'RANGE.TOCSV'         '=RANGE.TOCSV({1,2;3,4},",",FALSE)'        "1,2`r`n3,4`r`n"),
    (New-Case 'RANGE.TRANSPOSE'     '=RANGE.TRANSPOSE({1,2;3,4})'              @(@(1.0, 3.0), @(2.0, 4.0))),
    (New-Case 'DT.ISLEAP 闰年'       '=DT.ISLEAP(2024)'                        $true),
    (New-Case 'DT.ISLEAP 平年'       '=DT.ISLEAP(2023)'                        $false),
    (New-Case 'DT.DIM'              '=DT.DIM(2024,2)'                          29.0),
    (New-Case 'DT.DATEDIFF'         '=DT.DATEDIFF("d",DATE(2024,1,1),DATE(2024,3,1))' 60.0),
    # Excel 序列号按"墙上时刻"换算（实现与机器时区无关），1704067200 列宽不足时 .Text 会显示 1.7E+09
    (New-Case 'DT.UNIXTS'           '=DT.UNIXTS(DATE(2024,1,1))'               1704067200.0),
    (New-Case 'FS.FNAME'            '=FS.FNAME("C:\data\x.txt")'               'x.txt'),
    (New-Case 'FS.EXT'              '=FS.EXT("C:\data\x.txt")'                 '.txt'),
    (New-Case 'PIVOT.GROUPBY'       '=PIVOT.GROUPBY({"k","v";"a",1;"b",2;"a",3},{0},1,"SUM",TRUE)' @(@('a', 4.0), @('b', 2.0)))
)

# 产物清单：模块 / TFM / 文件名（64 位变体）
$Artifacts = @(
    @{ Module = 'Analytics';   Tfm = 'net48';          File = 'Analytics-AddIn-net48-64-packed.xll' },
    @{ Module = 'Analytics';   Tfm = 'net8.0-windows'; File = 'Analytics-AddIn-net8.0-64-packed.xll' },
    @{ Module = 'DataToolkit'; Tfm = 'net48';          File = 'DataToolkit-AddIn-net48-64-packed.xll' },
    @{ Module = 'DataToolkit'; Tfm = 'net8.0-windows'; File = 'DataToolkit-AddIn-net8.0-64-packed.xll' }
)

# ============================================================================
# 辅助函数
# ============================================================================

# 按两种布局列出候选路径（① 仓库布局；② 扁平布局，兼容旧"已编译文件"目录）
function Get-XllCandidates {
    param([hashtable] $Spec)
    return @(
        (Join-Path $BaseDir (Join-Path $Spec.Module (Join-Path 'bin' (Join-Path $Configuration (Join-Path $Spec.Tfm (Join-Path 'publish' $Spec.File)))))),
        (Join-Path $BaseDir (Join-Path $Spec.Tfm $Spec.File))
    )
}

# 按两种布局解析产物路径；都不存在返回 $null
function Resolve-XllPath {
    param([hashtable] $Spec)
    foreach ($c in (Get-XllCandidates $Spec)) { if (Test-Path -LiteralPath $c -PathType Leaf) { return $c } }
    return $null
}

function Get-CaseLabel {
    param([hashtable] $Spec)
    $tfm = if ($Spec.Tfm -eq 'net48') { 'net48' } else { 'net8.0' }
    return "$($Spec.Module)-$tfm"
}

function Get-CaseCells {
    param($Cases)
    $n = 0
    foreach ($c in $Cases) { $n += ($c.Rows * $c.Cols) }
    return $n
}

# 把值转成 double（非数值返回 $null）
function ConvertTo-DoubleOrNull {
    param($Value)
    if ($null -eq $Value) { return $null }
    if ($Value -is [double] -or $Value -is [int] -or $Value -is [long] -or $Value -is [single] -or $Value -is [decimal]) {
        return [double]$Value
    }
    return $null
}

# 值是否 Excel 错误值（SCODE 区间）
function Test-IsErrorValue {
    param($Value)
    $d = ConvertTo-DoubleOrNull $Value
    if ($null -eq $d) { return $false }
    return ($d -le -2146820000 -and $d -ge -2146830000)
}

function Format-Actual {
    param($Value)
    if ($null -eq $Value) { return '<空>' }
    if (Test-IsErrorValue $Value) {
        $name = '未知错误'
        foreach ($k in $ErrorCode.Keys) { if ($ErrorCode[$k] -eq [int]$Value) { $name = $k } }
        return "$name($Value)"
    }
    if ($Value -is [bool]) { return $(if ($Value) { 'TRUE' } else { 'FALSE' }) }
    if ($Value -is [string]) { return "'" + ($Value -replace "`r", '\r' -replace "`n", '\n') + "'" }
    return [string]$Value
}

# 单值断言：返回 $true/$false
function Test-ExpectedValue {
    param($Actual, $Expect, [double] $Tol)

    # ① 期望是错误值 → 比 SCODE（不比对文本，避免非英文版 Excel 误判）
    if ($Expect -is [string] -and $ErrorCode.ContainsKey($Expect)) {
        $d = ConvertTo-DoubleOrNull $Actual
        return ($null -ne $d -and [int]$d -eq $ErrorCode[$Expect])
    }
    # ② 期望是布尔
    if ($Expect -is [bool]) {
        return ($Actual -is [bool] -and $Actual -eq $Expect)
    }
    # ③ 期望是数值
    if ($Expect -is [double] -or $Expect -is [int] -or $Expect -is [long] -or $Expect -is [single] -or $Expect -is [decimal]) {
        if (Test-IsErrorValue $Actual) { return $false }
        $d = ConvertTo-DoubleOrNull $Actual
        if ($null -eq $d) { return $false }
        $e = [double]$Expect
        return ([Math]::Abs($d - $e) -le ($Tol + $Tol * [Math]::Abs($e)))
    }
    # ④ 期望是字符串（空串按空单元格处理）
    if ($Expect -is [string]) {
        if (Test-IsErrorValue $Actual) { return $false }
        $s = if ($null -eq $Actual) { '' } else { [string]$Actual }
        return ($s -ceq [string]$Expect)
    }
    # ⑤ 期望是 $null（例如 DICT.FREQUENCY 的空键）
    if ($null -eq $Expect) { return ($null -eq $Actual -or ([string]$Actual) -eq '') }
    return $false
}

# SQL 用例的源区域（R1:S4 = 表头 + 三行数据）
function Initialize-Sheet {
    param($Ws)
    $Ws.Cells(1, 18).Value2 = 'name';  $Ws.Cells(1, 19).Value2 = 'score'
    $Ws.Cells(2, 18).Value2 = 'a';     $Ws.Cells(2, 19).Value2 = 10
    $Ws.Cells(3, 18).Value2 = 'b';     $Ws.Cells(3, 19).Value2 = 30
    $Ws.Cells(4, 18).Value2 = 'c';     $Ws.Cells(4, 19).Value2 = 20
}

# 释放 COM 并清掉残留 EXCEL.EXE（失败/异常路径同样执行）
function Clear-ExcelCom {
    param($Xl, $Wb)
    if ($Wb) {
        try { $Wb.Close($false) } catch { }
        try { [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($Wb) } catch { }
    }
    if ($Xl) {
        try { $Xl.Quit() } catch { }
        try { [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($Xl) } catch { }
    }
    [GC]::Collect()
    [GC]::WaitForPendingFinalizers()
    [GC]::Collect()

    # ② 先给 Excel 1 秒自行退出，再强制结束残留进程；
    # ③ 等到进程真正消失（否则脚本退出的瞬间仍能查到 EXCEL.EXE，验收"无残留进程"会误判）。
    #    实测 Excel 收到 Quit + RCW 释放后常不自行退出（隐藏窗口/加载项常驻），宽限给短一些。
    $grace = (Get-Date).AddSeconds(1)
    while ((Get-Date) -lt $grace -and (Get-Process -Name 'EXCEL' -ErrorAction SilentlyContinue)) {
        Start-Sleep -Milliseconds 200
    }
    Get-Process -Name 'EXCEL' -ErrorAction SilentlyContinue | ForEach-Object { try { $_.Kill() } catch { } }
    $hard = (Get-Date).AddSeconds(5)
    while ((Get-Date) -lt $hard -and (Get-Process -Name 'EXCEL' -ErrorAction SilentlyContinue)) {
        Start-Sleep -Milliseconds 200
    }
    if (Get-Process -Name 'EXCEL' -ErrorAction SilentlyContinue) {
        Write-Host "[警告] 仍有 EXCEL.EXE 未退出（请手动确认）。" -ForegroundColor Yellow
    }
}

# 只读检查 HKCU\...\Excel\Options 的 OPEN* 项。
# 本脚本用 Application.RegisterXLL（COM）注册，**只影响当前会话、不写注册表**，
# 因此不会留下需要清理的 OPEN* 残留（无需删除任何键；也不该删——那是用户的配置）。
# 这里只做只读探测：若发现 OPEN* 指向 *-AddIn-*-packed.xll，说明本机存在自动加载项，
# 可能让断言命中"另一个 XLL 的同名函数"→ 假绿风险 → 打印警告。
function Get-ExcelAutoOpenKeys {
    $found = @()
    $officeRoot = 'HKCU:\Software\Microsoft\Office'
    if (-not (Test-Path $officeRoot)) { return $found }
    foreach ($ver in @(Get-ChildItem -Path $officeRoot -ErrorAction SilentlyContinue)) {
        $optPath = $ver.PSPath + '\Excel\Options'
        if (-not (Test-Path $optPath)) { continue }
        $props = Get-ItemProperty -Path $optPath -ErrorAction SilentlyContinue
        if (-not $props) { continue }
        foreach ($p in $props.PSObject.Properties) {
            if ($p.Name -like 'OPEN*' -and $p.Value -is [string] -and $p.Value -like '*.xll') {
                $found += "$($ver.PSChildName)\...\Excel\Options\$($p.Name) = $($p.Value)"
            }
        }
    }
    return $found
}

# ============================================================================
# 主流程
# ============================================================================

$plannedCells = 0
$executedCells = 0
$failures = New-Object System.Collections.ArrayList

Write-Host "=========================================================="
Write-Host " XLL 真机加载门禁（Excel / Excel-DNA）"
Write-Host "=========================================================="
Write-Host " 产物根目录 : $BaseDir"
Write-Host " 构建配置   : $Configuration"
Write-Host " 轮数       : $Rounds（每个 XLL 重复 $Rounds 次 启动→注册→断言→退出）"
Write-Host " 用例数     : Analytics $($AnalyticsCases.Count) 条 / DataToolkit $($DataToolkitCases.Count) 条"
Write-Host ""

# ---- 1. 产物解析（缺失 = 环境不可用 → exit 2） ----
$targets = @()
$missing = @()
foreach ($spec in $Artifacts) {
    $path = Resolve-XllPath $spec
    if ($null -eq $path) {
        $missing += @{ Label = (Get-CaseLabel $spec); Candidates = (Get-XllCandidates $spec) }
    } else {
        $targets += @{ Spec = $spec; Path = $path; Label = (Get-CaseLabel $spec) }
    }
}
if ($missing.Count -gt 0) {
    Write-Host "[环境不可用] 缺少 $($missing.Count) 个 XLL 产物：" -ForegroundColor Red
    foreach ($m in $missing) {
        Write-Host "  - $($m.Label)（以下路径均不存在）" -ForegroundColor Red
        foreach ($c in $m.Candidates) { Write-Host "      $c" -ForegroundColor DarkGray }
    }
    Write-Host "  请先构建：dotnet build ExcelFormulaLabs.sln -c $Configuration -m:1" -ForegroundColor Yellow
    Write-Host "  或用 -BaseDir 指向别处的产物目录（支持仓库布局与 net48\net8.0-windows 扁平布局）。" -ForegroundColor Yellow
    exit $EXIT_ENV
}
foreach ($t in $targets) { Write-Host ("  [产物] {0,-18} {1}" -f $t.Label, $t.Path) }

# ---- 2. Excel 可用性（不可用 = 环境不可用 → exit 2） ----
$probe = $null
try {
    $probe = New-Object -ComObject Excel.Application
    $probe.Visible = $false
    $probe.DisplayAlerts = $false
    $excelVersion = $probe.Version
} catch {
    Write-Host "[环境不可用] 无法启动 Excel（COM 不可用）：$($_.Exception.Message)" -ForegroundColor Red
    Write-Host "  本脚本需要**桌面版 64 位 Excel**；无 Excel 的机器请用 runner 标签筛掉该 job，不要吞掉 exit 2。" -ForegroundColor Yellow
    if ($probe) { Clear-ExcelCom -Xl $probe -Wb $null }
    exit $EXIT_ENV
}
Clear-ExcelCom -Xl $probe -Wb $null
Write-Host " Excel 版本 : $excelVersion"
Write-Host ""

# ---- 3. 只读注册表探测（警告，不影响退出码） ----
foreach ($k in @(Get-ExcelAutoOpenKeys)) {
    Write-Host "[警告] 检测到 Excel 自动加载项注册表项：$k" -ForegroundColor Yellow
    Write-Host "       若它导出与本脚本被测 XLL 同名的函数，断言可能命中另一个 XLL（假绿风险）。" -ForegroundColor Yellow
}

# ---- 4. 逐轮、逐 XLL 执行（一个 XLL 一个 Excel 进程，避免同名函数互相顶替） ----
$sw = [System.Diagnostics.Stopwatch]::StartNew()
for ($round = 1; $round -le $Rounds; $round++) {
    Write-Host "===== 第 $round/$Rounds 轮 ====="
    foreach ($t in $targets) {
        $cases = if ($t.Spec.Module -eq 'Analytics') { $AnalyticsCases } else { $DataToolkitCases }
        $plannedCells += (Get-CaseCells $cases)
        $roundFail = 0
        $xl = $null; $wb = $null; $ws = $null
        try {
            $xl = New-Object -ComObject Excel.Application
            $xl.Visible = $false
            $xl.DisplayAlerts = $false
            try { $xl.ScreenUpdating = $false } catch { }
            $registered = $xl.RegisterXLL($t.Path)
            if (-not $registered) { throw "RegisterXLL 返回 False（XLL 加载失败：32 位 Excel 装不了 64 位 XLL、产物损坏/被占用、或路径不是绝对路径）" }
            $wb = $xl.Workbooks.Add()
            $ws = $wb.Worksheets.Item(1)
            Initialize-Sheet -Ws $ws

            # 4.1 写入全部公式（网格用 CSE 数组公式落在精确尺寸区间）
            $anchor = 1
            $pending = @()
            foreach ($c in $cases) {
                $cellRef = "A$anchor"
                $setError = $null
                try {
                    if ($c.Rows -gt 1 -or $c.Cols -gt 1) {
                        $rng = $ws.Range($ws.Cells($anchor, 1), $ws.Cells($anchor + $c.Rows - 1, $c.Cols))
                        $rng.FormulaArray = $c.Formula
                    } else {
                        $ws.Cells($anchor, 1).Formula = $c.Formula
                    }
                } catch {
                    $setError = $_.Exception.Message
                }
                $pending += @{ Case = $c; Anchor = $anchor; CellRef = $cellRef; SetError = $setError }
                $anchor += 6   # 最大网格 5 行，留 1 行间隔，网格之间不会互相压盖
            }

            # 4.2 等计算完成 + 异步回填留时间
            $calcSw = [System.Diagnostics.Stopwatch]::StartNew()
            while ($calcSw.ElapsedMilliseconds -lt 10000) {
                $state = 0
                try { $state = [int]$xl.CalculationState } catch { break }
                if ($state -eq 0) { break }
                Start-Sleep -Milliseconds 100
            }
            Start-Sleep -Milliseconds 400

            # 4.3 读取并断言
            foreach ($p in $pending) {
                $c = $p.Case
                if ($p.SetError) {
                    $roundFail++
                    [void]$failures.Add("[$(Get-CaseLabel $t.Spec)] R$round $($c.Name)：公式写入失败（$($p.CellRef)）—— $($p.SetError)")
                    continue
                }
                # 异步 UDF：读取前轮询等待回填（占位值为 #N/A）
                if ($c.Async) {
                    $asyncSw = [System.Diagnostics.Stopwatch]::StartNew()
                    while ($asyncSw.ElapsedMilliseconds -lt 2500) {
                        $probeVal = $ws.Cells($p.Anchor, 1).Value2
                        if (-not (Test-IsErrorValue $probeVal)) { break }
                        if ([int](ConvertTo-DoubleOrNull $probeVal) -ne $ErrorCode['#N/A']) { break }
                        Start-Sleep -Milliseconds 350
                    }
                }
                for ($r = 0; $r -lt $c.Rows; $r++) {
                    for ($col = 0; $col -lt $c.Cols; $col++) {
                        $expectCell = $c.Grid[$r][$col]
                        $cellRef = "$([char](65 + $col))$($p.Anchor + $r)"
                        $actual = $null
                        try {
                            $actual = $ws.Cells($p.Anchor + $r, 1 + $col).Value2
                        } catch {
                            $roundFail++
                            [void]$failures.Add("[$(Get-CaseLabel $t.Spec)] R$round $($c.Name)：读取 $cellRef 失败 —— $($_.Exception.Message)")
                            continue
                        }
                        $executedCells++
                        if (-not (Test-ExpectedValue -Actual $actual -Expect $expectCell -Tol $c.Tol)) {
                            $roundFail++
                            [void]$failures.Add("[$(Get-CaseLabel $t.Spec)] R$round $($c.Name) @$cellRef：期望 $(Format-Actual $expectCell)，实际 $(Format-Actual $actual)")
                        }
                    }
                }
            }
            if ($roundFail -eq 0) {
                Write-Host ("  [PASS] {0,-18} R{1}：{2} 条用例 / {3} 个断言" -f $t.Label, $round, $cases.Count, (Get-CaseCells $cases)) -ForegroundColor Green
            } else {
                Write-Host ("  [FAIL] {0,-18} R{1}：{2} 个断言不符" -f $t.Label, $round, $roundFail) -ForegroundColor Red
            }
        } catch {
            # XLL 加载失败 / Excel 崩溃：本 XLL 本轮全部断言未执行（executed < planned → 兜底 exit 2）
            $roundFail++
            [void]$failures.Add("[$(Get-CaseLabel $t.Spec)] R$round 执行中断：$($_.Exception.Message)")
            Write-Host ("  [FAIL] {0,-18} R{1}：执行中断 —— {2}" -f $t.Label, $round, $_.Exception.Message) -ForegroundColor Red
        } finally {
            Clear-ExcelCom -Xl $xl -Wb $wb
        }
    }
}
$sw.Stop()

# 收尾清扫（各 XLL 的 finally 已清过一轮）：保证脚本退出时本机没有残留 EXCEL.EXE
Clear-ExcelCom -Xl $null -Wb $null

# ---- 5. 汇总与退出码 ----
Write-Host ""
Write-Host "=========================================================="
Write-Host " 结果：执行断言 $executedCells / 计划 $plannedCells，失败 $($failures.Count) 条，耗时 $([int]$sw.Elapsed.TotalSeconds) 秒"
Write-Host "=========================================================="

if ($failures.Count -gt 0) {
    Write-Host ""
    Write-Host "断言失败明细：" -ForegroundColor Red
    foreach ($f in $failures) { Write-Host "  - $f" -ForegroundColor Red }
    Write-Host ""
    Write-Host "[FAIL] 存在断言失败（exit $EXIT_FAIL）。" -ForegroundColor Red
    exit $EXIT_FAIL
}

if ($executedCells -lt $plannedCells) {
    Write-Host ""
    Write-Host "[环境不可用] 实际执行 $executedCells 个断言，少于计划 $plannedCells 个。" -ForegroundColor Red
    Write-Host "  可能原因：XLL 未注册成功、Excel 中途退出、产物被占用。" -ForegroundColor Yellow
    Write-Host "  按门禁语义，'执行数不足'一律视为环境不可用（exit $EXIT_ENV），不得当成功。" -ForegroundColor Yellow
    exit $EXIT_ENV
}

if ($executedCells -eq 0) {
    # 双保险：0 个断言执行绝不放行（正常路径已被上面的 executed < planned 拦下）
    Write-Host "[环境不可用] 0 个断言被执行（exit $EXIT_ENV）。" -ForegroundColor Red
    exit $EXIT_ENV
}

Write-Host ""
Write-Host "[PASS] 全部 $executedCells 个断言通过（$Rounds 轮 × $($targets.Count) 个 XLL）。" -ForegroundColor Green
exit $EXIT_PASS
