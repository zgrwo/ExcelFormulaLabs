# NaN/Inf 守卫 Checklist

> 每个新增或修改的 Core 方法必须逐项确认。未通过任何一项 = 不允许合入。

## 使用方法

新增 Core 方法后，复制下方 checklist 到 PR 描述中逐项勾选。

---

## 输入守卫（静默传播阻断）

- [ ] **NaN 输入**：`double.IsNaN(input)` → 返回 `double.NaN`，不继续计算
- [ ] **+Inf 输入**：`double.IsPositiveInfinity(input)` → 返回 `double.NaN`
- [ ] **-Inf 输入**：`double.IsNegativeInfinity(input)` → 返回 `double.NaN`
- [ ] **null/空字符串**：`string.IsNullOrEmpty(input)` → 返回 `string.Empty` 或 `double.NaN`
- [ ] **空数组/集合**：长度为 0 → 返回 `double.NaN` 或空结果，不抛异常

## 计算守卫（防御完整性）

- [ ] **除零**：分母为 0 → 返回 `double.NaN`（L2 哨兵）
- [ ] **负数开方**：`Math.Sqrt(负数)` → 返回 `double.NaN`
- [ ] **对数非正**：`Math.Log(<=0)` → 返回 `double.NaN`
- [ ] **溢出**：`checked` 或结果 `double.IsInfinity` → 返回 `double.NaN`（L4 哨兵）
- [ ] **空集合统计**：Mean/Std/Var 空输入 → 返回 `double.NaN`
- [ ] **单元素方差**：n=1 时 Var/Std → 返回 `double.NaN` 或 0（视 ddof）

## 输出守卫（结果验证）

- [ ] **结果 Inf 检查**：计算完成后 `double.IsInfinity(result)` → 替换为 `double.NaN`
- [ ] **结果 NaN 传播**：如果中间步骤产生 NaN，最终结果应为 NaN（不吞没）
- [ ] **数组结果**：矩阵/数组输出中不含 Inf（可用 `NumericGuard` 扫描）

## 异常处理（异常过滤器）

- [ ] **无裸 catch**：不存在 `catch { }` 或 `catch (Exception) { }` 无 when 过滤器
- [ ] **排除致命异常**：`catch when (!(ex is OutOfMemoryException || ex is StackOverflowException || ex is AccessViolationException))`
- [ ] **WrapError 包裹**：UDF 层用 `OutputWrapper.WrapError()` 将异常转为 `#VALUE!`
- [ ] **不吞没异常**：catch 块中必须有日志/重抛/返回错误值，不允许空 catch

## 哨兵契约一致性

- [ ] **double 方法**：无效输入 → `double.NaN`（不抛异常）
- [ ] **string 方法**：无效输入 → `string.Empty`（不抛异常）
- [ ] **object[,] 方法**：含 `bool hasHeaders = true` 参数
- [ ] **L5 例外**：未知类型 Convert 失败 → 允许 throw（由 WrapError 兜底）

## 交叉验证

- [ ] **Python 独立实现**：数值类 UDF 有对应的 `cross_check()` 验证
- [ ] **非自校验**：`check(name, X, X)` 模式不存在
- [ ] **容差合理**：默认 1e-10，统计函数可放宽到 1e-4

---

## 快速 grep 验证命令

```powershell
# 裸 catch 检查（必须返回空）
Get-ChildItem src -Recurse -Filter "*.cs" | Select-String 'catch\s*\{'

# 自校验检查（必须返回空）
Select-String -Path scripts/verify-manual.py -Pattern 'check\([^,]+,\s*([^,]+),\s*\1\s*[,)]'

# Core 层 ExcelDna 引用（必须返回空）
Get-ChildItem src -Recurse -Filter "*Core.cs" | Select-String 'ExcelDna'
```
