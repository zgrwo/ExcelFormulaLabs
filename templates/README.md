# templates/ — 模块脚手架

> 新增模块/函数的起点模板。结构唯一定义见 [project-structure.md](../rules/project-structure.md)。

## NewModule/（UDF 四文件模板）

新增一个 UDF 模块（如 `FOO.*`）时使用：

| 模板文件 | 生成目标 | 内容 |
| :--- | :--- | :--- |
| `{Name}Core.cs.template` | `src/<Module>/{Name}Core.cs` | 纯逻辑 + 哨兵契约 + 异常过滤器 |
| `{Name}Udf.cs.template` | `src/<Module>/{Name}Udf.cs` | MapOver 分发 + `[ExcelFunction]` |
| `{Name}Core.Tests.cs.template` | `tests/<Module>.Tests/{Name}CoreTests.cs` | 边界/NaN/空值测试 |
| `{Name}CrossVal.py.template` | `tests/CrossValRunner/{Name}CrossVal.py` | Python 独立实现 + `cross_check()` |

展开方式：

```powershell
.\scripts\scaffold-udf.ps1 -Name Foo -Module DataToolkit
```

## 约定

- 模板中的 `{Name}` 为单花括号占位符（由 scaffold 脚本替换），禁止使用 `{{...}}` 双花括号
- 新模板文件必须登记到 `rules/project-structure.md` 目录树
