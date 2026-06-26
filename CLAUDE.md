# CLAUDE.md — 项目宪法

> 全局架构、绝对红线与核心流程。编码细节按需加载 Skill。

## 元数据

- **语言**：文档与注释默认中文
- **术语**：[CONTEXT.md](CONTEXT.md)
- **数字唯一基准**：[api-reference.md](docs/api-reference.md)
- **信息单一事实来源**：其余仅链接引用，禁止重复定义

## 技能加载

修改代码前**必须**加载对应 Skill。Skill 文件路径：

| 范围 | Skill 文件 | 内容 |
| :--- | :--- | :--- |
| Foundation / Analytics / DataToolkit / 编码规范 | `skills/excel-dna-project/skill.md` | 编码规范、架构、MapOver 变体、表头/哨兵契约、测试模式、全量测试命令 |
| UDF / .xll 打包 / 分发 | `skills/excel-dna-addins/skill.md` | UDF 声明规范、Excel-DNA 黄金法则、打包流程 |

> **执行方式**：直接 `Read` skill 文件（Skill 工具无法加载项目级 skill）。

## 架构分层

```
UDF 层 (public static, [ExcelFunction])  ← 入口：仅分发与适配
  ↓ MapOver / MapOverMulti / V() 分发
Core 层 (internal static, 纯逻辑)       ← 零 Excel 依赖
  ↓ 依赖
Foundation (共享工具)                    ← InputNormalizer, ElementWiseMapper, OutputWrapper, …
```

- ✅ UDF 不包含业务逻辑；Core 不引用 `ExcelDna.Integration`
- ❌ 禁止跨层直接调用或反向依赖

## 红线规则

### 1. 接口与兼容性

| ✅ DO | ❌ DON'T |
| :--- | :--- |
| 保持 Public 签名、UDF 参数/返回值不变 | 修改公开接口或破坏双 TFM 兼容 |
| `#if NET48` 仅限内部实现，方法签名双目标一致 | 在签名/参数上使用条件编译 |
| 新增 NuGet 前确认双 TFM 可用 | 引入单框架依赖 |

### 2. 防错三原则（违反 = bug）

| 原则 | ✅ DO | ❌ DON'T |
| :--- | :--- | :--- |
| **静默传播阻断** | 显式守卫 `NaN`/`Inf`/`null`/`default!`，WrapError 不兜底 | 依赖 IEEE 754 传播（如 `0/0=NaN` 静默返回） |
| **防御完整性** | 安全机制覆盖模块所有方法（ValidatePath / Regex Timeout / SQL 参数化） | 新方法遗漏防御（对照同模块已有方法补齐） |
| **异常过滤器** | `catch (Exception ex) when` 统一排除 `OutOfMemoryException`、`StackOverflowException`、`AccessViolationException` | 裸 `catch {}` 或 `catch (Exception)` 无 `when` |

> **提交前自检**：`grep -rn "catch\s*{" src/ --include="*.cs"` 必须返回空。

### 3. 表头行契约（`object[,]` 表格方法）

- ✅ 所有接受 `object[,]` 的 Core 方法**必须**含 `bool hasHeaders = true`（`CrossJoin` 等无表头语义的方法豁免）
- ✅ `hasHeaders=true`（默认）：跳过第一行（表头），数据从 `r=1` 开始；需要表头文本时 `r=0` 读取（不参与计算）

### 4. 哨兵契约（InputNormalizer L1-L5）

| 层级 | 职责 | ✅ DO | ❌ DON'T | 违反后果 |
| :--- | :--- | :--- | :--- | :--- |
| **L1 守卫** | 类型转换前 | 显式 `double.IsNaN(d)` / `double.IsInfinity(d)` | 依赖 CLR 未定义行为（如 `(long)NaN`） | 未定义行为 / 静默损坏 |
| **L2 哨兵** | 不可转换值 | 返回类型零值哨兵：`double`→`NaN`、`long`→0、`int`→0、`bool`→`false`、`DateTime`→`MinValue`、`string`→`""` | 抛异常或返回非零值 | 调用方误判 |
| **L3 Excel 信号** | `null`/`DBNull`/`ExcelEmpty`/`ExcelError`/`ExcelMissing` | 返回哨兵（语义：无有效值，跳过） | 抛异常或静默赋值 | Excel 交互异常 |
| **L4 已知取舍** | `long`/`bool` 哨兵（0/false）与真实值不可区分 | 文档说明；调用方前置 `IsNumericCell` | 依赖「0/false 表示错误」语义 | 数据误判 |
| **L5 最终边界** | `ConvertValue<T>` 未知类型 `Convert.ChangeType` 失败 | 已知 6 类委托 `InputNormalizer.ToXxx`；新类型：`double`→`NaN`，其余**必须 `throw`** | `return default(T)` 静默替代 | 脏数据传播 |

## 开发流程

### 修改前（强制）

1. **Read** 对应 Skill 文件（`skills/excel-dna-project/skill.md` 或 `skills/excel-dna-addins/skill.md`）
2. **CodeGraph** 检查调用者与影响范围（优先级高于 Skill）
3. 确认不违反红线规则（接口兼容、表头契约、哨兵契约、异常过滤器）

### 修改后

- 验证与调用方一致（签名/返回值/异常传播链路）
- 运行 `dotnet build && dotnet test` 确认无回归
- 缺陷处理：追溯根因 → 写入 memory 或记录改进计划（禁止仅修表面）

### 构建与测试

| 场景 | 命令 |
| :--- | :--- |
| 日常构建 | `dotnet restore && dotnet build && dotnet test` |
| 分发构建 | `dotnet build -c Release`（生成 `.xll`） |
| 全量测试 | ① `bash scripts/verify-docs.sh` ② `dotnet test` ③ `dotnet test --filter "CrossVal"` ④ `python scripts/verify-manual.py` ⑤ `dotnet build -c Debug && dotnet build -c Release` |

### Git

| ✅ DO | ❌ DON'T |
| :--- | :--- |
| 推送 `src/` 和 `tests/` 内的文件 | 推送这两个目录之外的任何文件 |
| Commit 前确认 `dotnet test` 全绿 | 未经用户明确同意执行 `git push` |
| Commit message 描述变更内容与原因 | 空 message 或无意义提交 |

## 参考

- [CONTEXT.md](CONTEXT.md) — 领域术语表
- [README.md](README.md) — 用户向功能指南
- [docs/api-reference.md](docs/api-reference.md) — 219 UDF 签名唯一信源
- [skills/excel-dna-project/skill.md](skills/excel-dna-project/skill.md) — 编码规范与架构详情
- [skills/excel-dna-addins/skill.md](skills/excel-dna-addins/skill.md) — Excel-DNA 开发全流程
- [docs/user-manual.md](docs/user-manual.md) — 每函数详细示例（Python 交叉验证）
