# templates/ — 模块脚手架

> 新增模块/函数的起点模板。结构唯一定义见 [project-structure.md](../docs/governance/project-structure.md)。
> **UDF 声明不手写**——属性与签名由元数据生成，见 [ADR-0011](../docs/adr/0011-udf-metadata-and-codegen.md)。

## NewModule/（三文件模板）

新增一个 UDF 模块（如 `FOO.*`）时使用：

| 模板文件 | 生成目标 | 内容 |
| :--- | :--- | :--- |
| `{Name}Core.cs.template` | `src/<Module>/{Name}Core.cs` | 纯逻辑 + 哨兵契约 + 异常过滤器 |
| `{Name}Udf.json.template` | `udf-metadata/{Name}Udf.json` | **UDF 声明单一真源**：函数名/描述/参数/分类/调用表达式 |
| `{Name}Core.Tests.cs.template` | `tests/<Module>.Tests/{Name}CoreTests.cs` | 边界/NaN/空值测试 |

脚本还会调用 `udfgen.py generate` 产出 `src/<Module>/{Name}Udf.g.cs`（生成物，勿手改）。

```powershell
.\scripts\scaffold-udf.ps1 -Module DataToolkit -Name Foo -Prefix FOO
```

> 不再有 `{Name}Udf.cs.template`：手写 `[ExcelFunction]` 包装方法已由元数据生成取代。
> 仅当需要分发层辅助方法或语句体（如 `*_ASYNC`）时，才手写 `{Name}Udf.cs` 并在其中放
> `public static partial class` + `private static` 助手。

## 交叉验证写法（数值类 UDF 必做）

两处同步：`tests/CrossValRunner/test_manifest.json` 加条目（供 C# 侧算出参考值），
`scripts/verify-manual.py` 对应 section 加检查。

```python
# 数值类：cross_check 走 Python 独立实现 vs C# CrossValRunner
cross_check("FOO.COMPUTE", np.mean([1, 2, 3, 4, 5]))

# 带容差
cross_check("FOO.STAT", float(stats.describe([1,2,3,4,5]).variance), tol=1e-8)

# 非数值类：check() + **硬编码**期望值（禁止 check(name, X, X) 自校验）
check("FOO.PROCESS", "hello world".title(), "Hello World")
```

```json
{ "id": "FOO.COMPUTE", "module": "FOO", "coreClass": "FooCore",
  "coreMethod": "Compute", "args": [[1,2,3,4,5]], "tolerance": 1e-10 }
```

## 约定

- 模板中的 `{Name}` / `{Module}` / `{PREFIX}` 为单花括号占位符（由 scaffold 脚本替换），
  禁止使用 `{{...}}` 双花括号
- 新模板文件必须登记到 `docs/governance/project-structure.md` 目录树
- `udf-metadata/*.json` 一般**不由脚手架覆盖**：同名元数据已存在时脚本会拒绝并提示直接编辑
