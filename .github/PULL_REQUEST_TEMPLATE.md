## 变更描述

简要说明本 PR 做了什么。

关联 Issue：#

## 变更类型

- [ ] Bug 修复（不改变公开接口）
- [ ] 新功能（新增 UDF 或模块）
- [ ] 重构（不改变行为）
- [ ] 文档更新
- [ ] CI/构建改进

## 验证清单

- [ ] `dotnet build` 双 TFM 通过（net48 + net8.0）
- [ ] `dotnet test` 全量测试通过
- [ ] `python scripts/verify-manual.py` 通过（如涉及数值变更）
- [ ] 无裸 `catch {}`（`grep -rn "catch\s*{" src/` 返回空）
- [ ] 无自校验模式
- [ ] 未修改现有 UDF 公开签名
- [ ] net8.0 未添加 IntelliSense 代码
- [ ] CHANGELOG.md 已更新

## 测试说明

描述如何验证此变更：

```
测试命令或步骤
```

## 截图（如适用）

Excel 中的函数运行截图。
