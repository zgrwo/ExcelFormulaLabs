# examples/ — 示例

> 最小可运行的完整实践演示。结构唯一定义见 [project-structure.md](../rules/project-structure.md)。

## 交叉验证示例（TestData）

本项目的"最小可运行示例"定位由 `tests/TestData/` 承担（保持测试与参考数据同仓，避免重复维护）：

- `tests/TestData/Cross_Validation_vs_Python.xlsx` — Python（numpy/scipy）交叉验证参考值，供 CrossValRunner 比对
- `tests/TestData/generate_python_refs.py` — 参考数据再生成器（独立实现，禁止"自己验证自己"）

运行示例：

```powershell
# ① 生成/更新 Python 参考数据
python tests/TestData/generate_python_refs.py

# ② 运行交叉验证（C# ↔ Python 逐项比对，精度 1e-10）
dotnet build tests/CrossValRunner
python scripts/verify-manual.py
```

## 约定

- 新增独立示例项目时登记 `rules/project-structure.md` 目录树
- 示例代码必须可运行、可验证（禁止仅展示的伪代码）
