# agents.md — ExcelFormulaLabs 项目宪法

> Excel 函数增强库：232 UDF，基于 C# / Excel-DNA，双 TFM (net48 + net8.0)。
> 本文件面向 AI 编程助手，编码细节按需加载 Skill。术语见 [context.md](rules/context.md)。

## 元数据

- **项目名**：ExcelFormulaLabs
- **GitHub**：https://github.com/zgrwo/ExcelFormulaLabs
- **语言**：C#（文档与注释默认中文）
- **术语**：[context.md](rules/context.md)
- **数字唯一基准**：[api-reference.md](rules/api-reference.md) — 232 UDF 签名以此为准
- **SSOT**：每个事实只在一处定义，其余仅链接引用

## 四条核心准则

### 1. 先想后写 (Think Before Coding)

- **不确定就提问**。不要猜测业务规则——去查 specification。
- **说出来你做假设了**。"假设 X 不超过 5 级 → 代码按此编写。"
- **主动呈现权衡**。"两种方案：A 简单 O(N)，B 复杂 O(1)。当前规模下 A 足够。"
- **发现架构偏离时停下来**。例如：发现自己在 Core 层引用了 ExcelDna → 停下，走 UDF 层。

### 2. 简洁至上 (Simplicity First)

- **最少代码解决问题**。
- **不为一成不变的场景建抽象层**。
- **自检**：一个资深开发者看这段代码会觉得过度设计吗？如果是，简化。

### 3. 精准修改 (Surgical Changes)

- **只改该改的**。不要顺带重构无关代码。
- **匹配现有风格**。
- **发现无关问题时提出来，不擅自改**。

### 4. 目标驱动 (Goal-Driven Execution)

- **先定义验证方式，再开始写代码**。
- 将指令转化为可验证目标：

| 而不是 | 而是 |
|--------|------|
| "添加 UDF" | "新 UDF 通过 CrossVal 与 Python 独立实现一致。去验证。" |
| "修复 Bug" | "复现测试 FAILS → 修复后 PASSES + 无回归。去验证。" |

## 技能加载

修改代码前**必须**加载对应 Skill：

| 范围 | Skill 文件 | 内容 |
| :--- | :--- | :--- |
| Foundation / Analytics / DataToolkit / 编码规范 | `skills/excel-dna-project.md` | 编码规范、架构、MapOver 变体、表头/哨兵契约、测试模式 |
| UDF / .xll 打包 / 分发 | `skills/excel-dna-addins.md` | UDF 声明规范、Excel-DNA 黄金法则、打包流程 |

### 专家 Skill（重构生命周期）

| 阶段 | Skill | 触发时机 |
|------|-------|----------|
| 决策前 | `skills/architecture-reviewer.md` | 新增组件/层级/依赖前 |
| 执行中 | `skills/refactoring-guardian.md` | 每个 Phase 开始/结束时 |
| 执行后 | `skills/project-plan-review.md` | 里程碑复盘/规划评审时 |

## 架构分层

```
UDF 层 (public static, [ExcelFunction])  ← 入口：仅分发与适配
  ↓ MapOver / MapOverMulti / V() 分发
Core 层 (internal static, 纯逻辑)       ← 零 Excel 依赖
  ↓ 依赖
Foundation (共享工具)                    ← InputNormalizer, ElementWiseMapper, OutputWrapper
```

- ✅ UDF 不包含业务逻辑；Core 不引用 `ExcelDna.Integration`
- ❌ 禁止跨层直接调用或反向依赖

## 仓库目录树

> 路由地图：所有文件路径均以此为基准。详细结构见 [project-structure.md](rules/project-structure.md)。

```
ExcelFormulaLabs/
├── src/                          # 源码（Foundation / Analytics / DataToolkit）
├── tests/                        # 测试 + CrossVal
├── rules/                        # 规范文档
├── skills/                       # Skill 定义
├── tools/                        # 构建/验证脚本
├── build/                        # CI/CD 配置
├── agents.md                     # 本文件
├── README.md                     # 用户向功能指南
└── .gitignore
```

## 红线规则

### 1. 接口与兼容性

| ✅ DO | ❌ DON'T |
| :--- | :--- |
| 保持 Public 签名、UDF 参数/返回值不变 | 修改公开接口或破坏双 TFM 兼容 |
| `#if NET48` 仅限内部实现 | 在签名/参数上使用条件编译 |
| 新增 NuGet 前确认双 TFM 可用 | 引入单框架依赖 |

### 2. 防错三原则

| 原则 | 核心 |
| :--- | :--- |
| **静默传播阻断** | 显式守卫 NaN/Inf/null/default!，WrapError 不兜底 |
| **防御完整性** | ValidatePath / Regex Timeout / SQL 参数化 覆盖所有方法 |
| **异常过滤器** | `catch when` 排除 OOM/StackOverflow/AccessViolation |

> `grep -rn "catch\s*{" src/ --include="*.cs"` 必须返回空。

### 3. IntelliSense 框架隔离

- ✅ net48：启用 ExcelDna.IntelliSense
- ❌ net8.0：**禁止添加 IntelliSense 代码**（Excel-DNA Issue #343）

### 4. 表头行契约

所有接受 `object[,]` 的 Core 方法必须含 `bool hasHeaders = true`。
豁免：纯结构变换（Transpose / SelectColumns / SelectRows / CrossJoin / Flatten2D / Count）不解释表头语义，无需该参数。

### 5. 哨兵契约（L1-L5）

不可转换值返回类型零值哨兵，不抛异常。未知类型 Convert 失败：double→NaN，其余必须 throw。

### 6. 闭环验证（Python ↔ C# ↔ 手册）

- 禁止自校验 `check(name, X, X)`
- 数值类 UDF 必须 `cross_check()`
- 修改后运行全量验证 5 步

## 构建与测试

| 场景 | 命令 |
| :--- | :--- |
| 日常构建 | `dotnet restore && dotnet build && dotnet test` |
| 分发构建 | `dotnet build -c Release` |
| 全量测试 | ① verify-docs ② dotnet test ③ CrossVal ④ verify-manual.py ⑤ Release build |

## 历史经验（从 diff 提炼）

### 高频修复模式

| 模式 | 出现次数 | 根因 |
|------|----------|------|
| NaN/Inf 守卫缺失 | 10+ | 初始实现未考虑退化输入 |
| IntelliSense 反复尝试 | 8 次 | Excel-DNA net8.0 已知 bug |
| 文档数字不一致 | 5+ | 多处硬编码计数 |
| 交叉验证自校验 | 3 处 | check(X,X) 假阴性 |
| long[] 封送失败 | 2 处 | Excel-DNA 不支持 long[] |

### 关键设计决策

- 双 TFM 而非单框架：覆盖 .NET Framework 4.8（免安装）和 .NET 8（性能）
- MapOver 三变体：标量/数组/多参数广播统一处理
- Python 交叉验证：独立实现比对，消除"自己验证自己"的假阴性

## 开发流程

### 修改前（强制）

1. **Read** 对应 Skill 文件（Skill-first，不凭记忆编造实现方式）
2. 检查调用者与影响范围
3. 确认不违反红线规则

### 修改后

- 验证与调用方一致（签名/返回值/异常传播链路）
- 运行构建 + 测试确认无回归
- 缺陷处理：追溯根因 → 写入 memory（禁止仅修表面）

### 遇到 Bug 时

1. 写最小复现测试 → confirm: 测试 FAILS（Bug 存在）
2. 修复 → confirm: 复现测试 PASSES + 已有测试无回归
3. **保留复现测试**（它现在是回归守卫）
4. 检查是否需要更新 spec / skill / review

### 提交前必检

- [ ] 所有新代码有对应的测试
- [ ] 无跨层/跨线程违规
- [ ] 命名空间与文件夹一致
- [ ] 没动无关文件
- [ ] `dotnet build` 双 TFM 通过 + `dotnet test` 全绿

## 防幻觉铁律

| 铁律 | 说明 |
|------|------|
| **不靠记忆引用文档** | 每次引用 rules/ 或 skills/ 中的内容时，先 Read/Grep 确认 |
| **不确定 = 承认不确定** | 不要编造业务规则；说“我需要在 spec 中确认”然后去查 |
| **写过的代码 = 读过的代码** | 不要假设自己知道某个文件内容——Read 它再改 |
| **版本号是事实锚点** | 每个结论标注来源文档版本 |

## 会话管理

### 何时自查

- **每完成一个独立功能点** — 对照四条核心准则自检
- **上下文超过 5 个文件 / 20 轮对话** — 提醒用户开新会话
- **反复纠正同一个错误时** — 停下来写进文档或更新规则

### 跨会话接力

```
上一个会话结束时 → 在回复末尾简述：
  ✅ 已完成: [具体交付物]
  🔜 下一步: [下一动作 + 涉及文件]
  ⚠️ 待决策: [阻塞项]
  📄 关键上下文: [后续会话必须知道的约束/假设]
```

### 基本原则

- 新会话先读本文件 + 对应 Skill
- 跨会话通过 git commit 衔接，不依赖对话历史
- 每个 commit 应自包含、可追溯

## 参考

| 文档 | 角色 | 内容 |
| :--- | :--- | :--- |
| [README.md](README.md) | 用户入口 | 安装、模块速览、使用模式 |
| [context.md](rules/context.md) | 术语表 | 所有术语唯一定义 |
| [api-reference.md](rules/api-reference.md) | 数字唯一信源 | 232 UDF 签名、参数、错误行为 |
| [user-manual.md](rules/user-manual.md) | 学习教程 | 每函数详细示例 + 结果解读 |
| [project-structure.md](rules/project-structure.md) | 结构地图 | 文件职责与层级关系 |
| [documentation.md](rules/documentation.md) | 文档职责 | 各文档分工与维护规则 |
| [code-review-prompt.md](rules/code-review-prompt.md) | 审查模板 | 深度代码审查 Prompt |
| [refactoring-plan.md](rules/refactoring-plan.md) | 重构计划 | Phase 0-4 重构路线图 |
