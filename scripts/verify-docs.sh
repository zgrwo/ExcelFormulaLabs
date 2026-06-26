#!/bin/bash
# 文档一致性验证 — 作为全量测试的一部分运行
# 检查：信源唯一性、模块完整性、UDF覆盖、架构一致性、版本匹配、catch残留
set -euo pipefail
cd "$(dirname "$0")/.."

PASS=0; FAIL=0
check() { local label="$1" result="$2"; if [ "$result" = "OK" ]; then echo "  ✅ $label"; PASS=$((PASS+1)); else echo "  ❌ $label: $result"; FAIL=$((FAIL+1)); fi; }

# 1. UDF数量：以api-reference.md为准
DOC_UDF=$(grep -cE '^\| `[A-Z]+\.[A-Z]' docs/api-reference.md 2>/dev/null || echo 0)
CODE_UDF=$(grep -roh 'ExcelFunction(Name *= *"[^"]*"' src/ --include="*.cs" 2>/dev/null | sed 's/ExcelFunction(Name *= *"//;s/"//' | sort -u | grep -c . || echo 0)
[ "$DOC_UDF" = "$CODE_UDF" ] && check "UDF数量 ($DOC_UDF)" "OK" || check "UDF数量" "doc=$DOC_UDF code=$CODE_UDF"

# 2. 每个源码UDF在api-reference.md有对应条目
while IFS= read -r udf; do
    [ -z "$udf" ] && continue
    grep -q "$udf" docs/api-reference.md || { check "UDF全覆盖: $udf" "缺失"; }
done < <(grep -roh 'ExcelFunction(Name *= *"[^"]*"' src/ --include="*.cs" 2>/dev/null | sed 's/ExcelFunction(Name *= *"//;s/"//' | sort -u)
check "UDF全覆盖检查完成" "OK"

# 3. skill.md包含RangeExport
grep -q 'RangeExport' skills/excel-dna-project/skill.md && check "skill.md RangeExport" "OK" || check "skill.md RangeExport" "缺失"

# 4. 架构术语现代性
grep -q 'MapOver' skills/excel-dna-project/skill.md && check "skill.md MapOver术语" "OK" || check "skill.md MapOver术语" "缺失"
grep -q 'ElementWiseMapper' README.md && check "README 内部类名ElementWiseMapper" "用户文档应使用MapOver而非内部类名" || check "README 无内部实现细节" "OK"

# 5. 版本号匹配（CONTEXT.md vs csproj）
DOC_VER=$(grep -oP 'MathNet\.Numerics\s+\K[0-9.]+' docs/CONTEXT.md 2>/dev/null || echo "?")
CSPROJ_VER=$(grep -oP 'MathNet\.Numerics.*Version="\K[0-9.]+' src/Analytics/Analytics.csproj 2>/dev/null || echo "?")
[ "$DOC_VER" = "$CSPROJ_VER" ] && check "MathNet版本 ($DOC_VER)" "OK" || check "MathNet版本" "doc=$DOC_VER csproj=$CSPROJ_VER"

# 6. 全项目无裸catch{}
BARE=$(grep -rn 'catch\s*{' src/ --include="*.cs" 2>/dev/null | grep -v obj/ || true)
[ -z "$BARE" ] && check "无裸catch{}" "OK" || { check "无裸catch{}" "$(echo "$BARE" | wc -l)处"; echo "$BARE" | head -3; }

# 7. .dna模板完整
[ -f src/DataToolkit/DataToolkit-AddIn-net8.dna.tpl ] && check "net8 .dna模板" "OK" || check "net8 .dna模板" "缺失"
[ -f src/DataToolkit/DataToolkit-AddIn-net48.dna.tpl ] && check "net48 .dna模板" "OK" || check "net48 .dna模板" "缺失"

# 8. 无残留生成.dna
find src/DataToolkit -maxdepth 1 -name "DataToolkit-AddIn.dna" ! -name "*.tpl" 2>/dev/null | grep -q . && check "无残留.dna" "发现残留" || check "无残留.dna" "OK"

echo "=== 通过:$PASS  失败:$FAIL ==="
[ "$FAIL" -eq 0 ] && exit 0 || exit 1
