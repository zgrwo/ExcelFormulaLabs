#!/usr/bin/env python3
"""udfgen.py — UDF 元数据化与源生成（见 docs/adr/0011-udf-metadata-and-codegen.md）。

六个子命令：
  extract      从 src/**/*Udf.cs 抽取元数据到 udf-metadata/，并做 **token 级往返校验**；
               校验不通过的函数不进入生成集合（generated=false），保留手写。
  generate     从 udf-metadata/ 生成 src/**/<UdfFile>.g.cs（仅 generated=true 的函数）。
  verify       重新生成到临时目录并与入库文件比对；同时校验手写 UDF 的属性与元数据一致。
               任何差异 → 退出码 1（供 pre-commit / CI 使用）。
  extract-api  从 docs/specification/api-reference.md 抽取「返回 / 说明」两列 + 章节前缀，
               写入元数据的 returns / doc / doc_section 字段（一次性引导工具，同 extract）。
  generate-api 从元数据**原地重写** api-reference.md 的每节表格（别名 gen-api）。
  verify-api   在内存中重渲染 api-reference.md 并与磁盘逐字节比对；差异 → 退出码 1。

设计要点：
  * 元数据里的 expr 是**函数体表达式原文**，等价比对靠"删除全部空白后逐字符相同"，
    因此"生成即原代码"是构造性保证，而非靠推理。
  * 生成物入库（非 obj/），使 verify-docs 等按源码文本统计的门禁继续有效。
  * api-reference 的"返回/说明"列是中文用户向文案，元数据里原本只有英文 Excel 提示语
    （desc），两者 240/240 全不相同——故必须先 extract-api 把它们抽进元数据，再谈生成。
"""
import argparse
import json
import re
import sys
import tempfile
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
META_DIR = REPO / "udf-metadata"

# CI（GitHub Actions 的 Windows runner）上 Python 的 stdout 编码是 **cp1252**，直接 print 中文会抛
# UnicodeEncodeError——于是"校验通过"变成崩溃退出码 1，门禁把 PASS 读成 FAIL
# （实测 2026-10-07：本机 ACP 是 UTF-8，从不复现，是典型的 works-on-my-machine）。
# 这里显式把 stdout/stderr 切到 UTF-8；reconfigure 不可用时退回 errors='replace' 语义，
# 确保即使编码不支持也只是字符降级、绝不让"打印成功消息"本身成为失败路径。
for _stream in (sys.stdout, sys.stderr):
    try:
        _stream.reconfigure(encoding="utf-8", errors="replace")
    except (AttributeError, ValueError, OSError):
        pass

# 模块 → Excel 插入函数对话框分类（ADR-0011 决策 6）
CATEGORY_BY_FILE = {
    "StatsUdf.cs": "Statistics",
    "LinalgUdf.cs": "Linear Algebra",
    "LinalgAsyncUdf.cs": "Linear Algebra",
    "RegressionUdf.cs": "Regression",
    "RegressionAsyncUdf.cs": "Regression",
    "SolveUdf.cs": "Solve",
    "PhyChemUdf.cs": "Physical Chemistry",
    "DoeUdf.cs": "DOE",
    "DoeAnalysisUdf.cs": "DOE",
    "StringUdf.cs": "String",
    "DateTimeUdf.cs": "Date & Time",
    "RegexUdf.cs": "Regex",
    "ArrayUdf.cs": "Array",
    "DictSetUdf.cs": "Dictionary & Set",
    "JsonXmlUdf.cs": "JSON & XML",
    "PivotUdf.cs": "Pivot",
    "SqlUdf.cs": "SQL",
    "FileSystemUdf.cs": "File System",
    "RangeExportUdf.cs": "Range Export",
}

MAX_LINE = 118  # 超过则换行（属性 / 签名）


def nows(s: str) -> str:
    """删除全部空白——token 级等价判据。"""
    return re.sub(r"\s+", "", s or "")


def one_line(s: str) -> str:
    """折叠为单行（元数据中的 expr 用）。字符串字面量内的空白不敏感（C# 里也不会跨行）。"""
    return re.sub(r"\s+", " ", s or "").strip()


# ─────────────────────────── 解析 ───────────────────────────

DECL = re.compile(
    r'^\[ExcelFunction\((?P<fn>.*?)\)\]\s*'
    r'public static object (?P<method>\w+)\((?P<params>.*)\)\s*=>\s*'
    r'OutputWrapper\.WrapError\(\(\)\s*=>\s*(?P<expr>.*)\)\s*;?\s*$',
    re.S)

FN_ATTR = re.compile(r'Name\s*=\s*"(?P<name>(?:[^"\\]|\\.)*)"')
DESC_ATTR = re.compile(r'Description\s*=\s*"(?P<desc>(?:[^"\\]|\\.)*)"', re.S)
CAT_ATTR = re.compile(r'Category\s*=\s*"(?P<cat>(?:[^"\\]|\\.)*)"', re.S)
ARG_ATTR = re.compile(
    r'\[ExcelArgument\(Name\s*=\s*"(?P<name>[^"]*)"\s*,\s*'
    r'Description\s*=\s*"(?P<desc>(?:[^"\\]|\\.)*)"\)\]\s*(?P<decl>.+)$', re.S)
# 参数名序列抽取（P2-1 手写 UDF 属性校验用）：逐个 `[ExcelArgument(Name = "...")]` 按出现顺序收集。
# `\s*` 覆盖跨行形态（RegressionAsyncUdf.cs 的参数就是逐行折行的）。
ARG_NAME_RE = re.compile(r'\[ExcelArgument\(\s*Name\s*=\s*"(?P<name>(?:[^"\\]|\\.)*)"', re.S)
NS_RE = re.compile(r'^namespace\s+([\w.]+)', re.M)
CLASS_RE = re.compile(r'public static (?:partial )?class (\w+)')


def split_top(s: str) -> list:
    """按顶层逗号切分。"""
    out, depth, cur = [], 0, ""
    for ch in s:
        if ch in "<([{":
            depth += 1
        elif ch in ">)]}":
            depth -= 1
        if ch == "," and depth == 0:
            out.append(cur.strip()); cur = ""
        else:
            cur += ch
    if cur.strip():
        out.append(cur.strip())
    return out


def parse_args(params_raw: str):
    """→ [ {name, desc, type, default} ]；无法解析则返回 None。"""
    args = []
    for p in split_top(params_raw):
        p = p.strip()
        if not p:
            continue
        m = ARG_ATTR.match(p)
        if not m:
            return None
        decl = one_line(m.group("decl"))
        default = None
        if "=" in decl:
            decl, default = decl.split("=", 1)
            default = default.strip()
        decl = decl.strip()
        parts = decl.rsplit(" ", 1)
        if len(parts) != 2:
            return None
        return_type, pname = parts[0].strip(), parts[1].strip()
        args.append(dict(name=m.group("name"), desc=one_line(m.group("desc")),
                         type=return_type, default=default, pname=pname))
    return args


def render_args(args) -> str:
    out = []
    for a in args:
        s = f'[ExcelArgument(Name = "{a["name"]}", Description = "{a["desc"]}")] {a["type"]} {a["pname"]}'
        if a.get("default") is not None:
            s += f'={a["default"]}'
        out.append(s)
    return ", ".join(out)


def render_fn(rec, category, with_category=True) -> str:
    """重建一个 UDF 声明（用于往返校验与生成）。"""
    attrs = f'Name = "{rec["excel"]}", Description = "{rec["desc"]}"'
    if with_category:
        attrs += f', Category = "{category}"'
    sig = f'public static object {rec["method"]}({render_args(rec["args"])})'
    body = f'=> OutputWrapper.WrapError(() => {rec["expr"]});'
    return f"[ExcelFunction({attrs})] {sig} {body}"


def wrap_decl(rec, category, indent="        ") -> str:
    """单行输出一个 UDF 声明。

    **必须单行**：`scripts/verify-docs.ps1` 的检查 1/2/11 用逐行 `Select-String` 匹配
    `ExcelFunction(Name = "…"`，检查 17 亦按 `[ExcelFunction(` 与 `[ExcelArgument(` 的
    出现顺序配对参数——属性一旦折行，这些检查全部读不到（实测：UDF 计数从 240 掉到 61）。
    可读性由**元数据**承担（那才是真源），生成物追求与门禁和原始风格一致。
    """
    attrs = (f'Name = "{rec["excel"]}", Description = "{rec["desc"]}", '
             f'Category = "{category}"')
    return (f'{indent}[ExcelFunction({attrs})] '
            f'public static object {rec["method"]}({render_args(rec["args"])})\n'
            f'{indent}    => OutputWrapper.WrapError(() => {rec["expr"]});')


_PAIRS = {"(": ")", "[": "]", "{": "}"}


def _skip_string(text: str, i: int, q: str) -> int:
    """text[i] 是引号 → 返回配对引号下标（转义感知）。"""
    i += 1
    n = len(text)
    while i < n:
        if text[i] == "\\":
            i += 2
            continue
        if text[i] == q:
            return i
        i += 1
    return n


def _match_bracket(text: str, i: int) -> int:
    """text[i] ∈ `([{` → 返回配对闭合符下标；失败 -1。字面量感知。"""
    if i >= len(text) or text[i] not in _PAIRS:
        return -1
    stack = [_PAIRS[text[i]]]
    n = len(text)
    i += 1
    while i < n:
        c = text[i]
        if c == '"' or c == "'":
            i = _skip_string(text, i, c)
        elif c in _PAIRS:
            stack.append(_PAIRS[c])
        elif stack and c == stack[-1]:
            stack.pop()
            if not stack:
                return i
        i += 1
    return -1


def _find_statement_end(text: str, i: int) -> int:
    """从 i 起找 depth 0 处的 `;`，返回其下标；失败 -1。字面量感知。"""
    depth = 0
    n = len(text)
    while i < n:
        c = text[i]
        if c == '"' or c == "'":
            i = _skip_string(text, i, c)
        elif c in "([{":
            depth += 1
        elif c in ")]}":
            depth -= 1
        elif c == ";" and depth == 0:
            return i
        i += 1
    return -1


def iter_declarations(text: str):
    """产出每个 `[ExcelFunction]` 方法的**结构化声明**。

    边界由声明自身决定（表达式体到 depth 0 的 `;`、语句体到配对 `}`），**不**按
    "下一个 [ExcelFunction"切分——后者会把文件末尾 UDF 之后的私有辅助方法吞进块内
    （实测 STR.STRIPHTML 块被撑到 1324 字符，尾部是另一个方法的函数体）。

    产出 dict：text（完整声明原文）、method、params（形参原文）、
    form（"expr" 表达式体 / "block" 语句体）、body（体原文）。
    """
    sig_re = re.compile(r"public\s+static\s+object\s+(\w+)\s*\(")
    for m in re.finditer(r"\[ExcelFunction", text):
        sig = sig_re.search(text, m.start())
        if not sig:
            continue
        method = sig.group(1)
        po = sig.end() - 1
        pc = _match_bracket(text, po)
        if pc < 0:
            continue
        params = text[po + 1:pc]
        j = pc + 1
        while j < len(text) and text[j] in " \t\r\n":
            j += 1
        if text.startswith("=>", j):
            end = _find_statement_end(text, j + 2)
            if end < 0:
                continue
            yield dict(text=text[m.start():end + 1], method=method, params=params,
                       form="expr", body=text[j + 2:end],
                       start=m.start(), end=end + 1)
        elif j < len(text) and text[j] == "{":
            end = _match_bracket(text, j)
            if end < 0:
                continue
            yield dict(text=text[m.start():end + 1], method=method, params=params,
                       form="block", body=text[j + 1:end],
                       start=m.start(), end=end + 1)


USING_RE = re.compile(r"^\s*using\s+([\w.]+)\s*;", re.M)


def extract_file(path: Path):
    raw = path.read_text(encoding="utf-8")
    text = re.sub(r"//[^\n]*", "", raw)
    ns = NS_RE.search(text)
    cls = CLASS_RE.search(text)
    category = CATEGORY_BY_FILE.get(path.name, "ExcelFormulaLabs")
    # 生成文件必须继承源文件的 using 集合：expr 里可能引用 System.Math、System.Linq 等
    # （实测漏 `using System;` 会导致生成物 CS0103）。取并集并保证两个必需项。
    usings = set(USING_RE.findall(raw))
    usings |= {"ExcelDna.Integration", "ExcelFormulaLabs.Foundation"}
    recs, hand = [], []
    for it in iter_declarations(text):
        blk, method, params_raw = it["text"], it["method"], it["params"]
        fn = FN_ATTR.search(blk)
        dm = DESC_ATTR.search(blk)
        excel = fn.group("name") if fn else "?"
        desc = one_line(dm.group("desc")) if dm else ""
        args = parse_args(params_raw)
        base = dict(excel=excel, method=method, desc=desc)
        if args is None:
            hand.append(dict(base, generated=False, handReason="args-unparsed"))
            continue
        if it["form"] == "block":
            # 语句体（异步 UDF 等）：保留手写，但属性/参数纳入元数据真源
            hand.append(dict(base, args=args, generated=False,
                             handReason="statement-body"))
            continue
        m = DECL.match(blk)
        if not m:
            hand.append(dict(base, args=args, generated=False,
                             handReason="shape-unrecognised"))
            continue
        rec = dict(base, args=args, expr=one_line(m.group("expr")))
        # token 级往返校验（不含 Category：原声明里没有该属性）
        if nows(render_fn(rec, category, with_category=False)) != nows(blk):
            hand.append(dict(base, args=args, generated=False,
                             handReason="roundtrip-mismatch"))
        else:
            rec["generated"] = True
            recs.append(rec)
    return dict(sourceFile=str(path.relative_to(REPO)).replace("\\", "/"),
                generatedFile=str(path.with_suffix(".g.cs").relative_to(REPO)).replace("\\", "/"),
                namespace=ns.group(1) if ns else "",
                className=cls.group(1) if cls else path.stem,
                category=category,
                usings=sorted(usings),
                functions=recs + hand)


# ─────────────────────────── 生成 ───────────────────────────

HEADER = """// <auto-generated>
//   本文件由 tools/udfgen.py 从 udf-metadata/{meta} 生成——请勿手工修改。
//   改函数名/描述/参数/分类 → 改元数据后运行：
//       python tools/udfgen.py generate
//   改实现逻辑 → 改 Core 层，或在元数据的 expr 中调整调用。
//   元数据与生成物的一致性由 scripts/verify-udfgen.ps1 门禁强制。
// </auto-generated>
// CS8669：带 // <auto-generated> 的文件中，可空引用批注要求显式 #nullable 指令。
#nullable enable
"""


def render_module(meta: dict) -> str:
    gen = [f for f in meta["functions"] if f.get("generated")]
    if not gen:
        return ""
    meta_name = Path(meta["sourceFile"]).stem + ".json"
    usings = "\n".join(f"using {u};" for u in meta.get("usings", []))
    parts = [HEADER.format(meta=meta_name), usings, "\n",
             f"\nnamespace {meta['namespace']}\n{{\n",
             f"    public static partial class {meta['className']}\n    {{\n"]
    for f in gen:
        parts.append(wrap_decl(f, meta["category"]))
        parts.append("\n\n")
    parts.append("    }\n}\n")
    return "".join(parts)


# ─────────────────────────── 命令 ───────────────────────────

def udf_files():
    for f in sorted((REPO / "src").rglob("*Udf.cs")):
        if re.search(r"[\\/](bin|obj)[\\/]", str(f)):
            continue
        yield f


def cmd_extract(args):
    """引导式抽取：元数据一旦建立，它就是真源，**不得**再从（已迁移的）源码覆盖。

    未加 --force 时，若某元数据文件已存在且内容会变化 → 直接报错退出
    （迁移后源码里已无那些声明，重跑会把元数据清空——实测踩过）。
    """
    META_DIR.mkdir(exist_ok=True)
    tot = gen = 0
    blocked = []
    for f in udf_files():
        meta = extract_file(f)
        out = META_DIR / (f.stem + ".json")
        new_text = json.dumps(meta, ensure_ascii=False, indent=1) + "\n"
        if out.exists() and not args.force:
            old_text = out.read_text(encoding="utf-8")
            if old_text != new_text:
                blocked.append(f"{out.name}（现有 {len(json.loads(old_text)['functions'])} 函数，"
                               f"重抽得 {len(meta['functions'])}）")
                continue
        out.write_text(new_text, encoding="utf-8")
        g = sum(1 for x in meta["functions"] if x.get("generated"))
        tot += len(meta["functions"]); gen += g
        print(f"  {f.name:24} 函数 {len(meta['functions']):3}  可生成 {g:3}")
    if blocked:
        print("\nFAIL: 以下元数据已存在且会被改动——extract 是**引导工具**，"
              "迁移后请改元数据 + generate，不要重抽。确认要覆盖请加 --force：")
        for b in blocked:
            print("  - " + b)
        return 1
    print(f"\n合计 {tot} 函数，可生成 {gen}，保留手写 {tot - gen}")
    return 0


def cmd_generate(args):
    n = 0
    for meta_file in sorted(META_DIR.glob("*.json")):
        if args.only and meta_file.stem not in args.only:
            continue
        meta = json.loads(meta_file.read_text(encoding="utf-8"))
        code = render_module(meta)
        if not code:
            continue
        dst = REPO / meta["generatedFile"]
        dst.write_text(code, encoding="utf-8")
        n += 1
        print(f"  生成 {meta['generatedFile']}")
    print(f"\n共生成 {n} 个文件")
    return 0


def cmd_migrate(args):
    """把 `*Udf.cs` 中**已由元数据接管**的声明删除，并把类改为 partial。

    手工精简 19 个文件既慢又易错；本命令使迁移可复现、可审查。
    手写声明（语句体等）原样保留。
    """
    n_file = n_removed = 0
    for meta_file in sorted(META_DIR.glob("*.json")):
        if args.only and meta_file.stem not in args.only:
            continue
        meta = json.loads(meta_file.read_text(encoding="utf-8"))
        src_path = REPO / meta["sourceFile"]
        raw = src_path.read_text(encoding="utf-8")
        # 用与 extract 相同的扫描器定位（注意：注释已剥版本用于定位，但删除要落在原文上）
        spans = []
        for it in iter_declarations(raw):
            rec = next((f for f in meta["functions"]
                        if f["method"] == it["method"]), None)
            if rec and rec.get("generated"):
                spans.append((it["start"], it["end"]))
        if not spans:
            continue
        out = raw
        for s, e in sorted(spans, reverse=True):
            # 连同该行左侧空白与行尾换行一起删除
            ls = out.rfind("\n", 0, s) + 1
            if out[ls:s].strip() == "":
                s = ls
            while e < len(out) and out[e] in " \t":
                e += 1
            if e < len(out) and out[e] == "\n":
                e += 1
            out = out[:s] + out[e:]
        out = re.sub(r"public static class (\w+Udf)", r"public static partial class \1", out)
        out = re.sub(r"\n{3,}", "\n\n", out)
        src_path.write_text(out, encoding="utf-8")
        n_file += 1
        n_removed += len(spans)
        print(f"  {src_path.name:24} 移除 {len(spans):3} 个声明")
    print(f"\n共处理 {n_file} 个文件，移除 {n_removed} 个声明")
    return 0


# ─────────────────── 手写 UDF 的属性级校验（P2-1） ───────────────────
#
# 旧实现的"手写 UDF 属性校验"名不副实（review-2026-10-08 实证）：
#   ① `if not code: continue` 在**文件级**早退——0 个可生成函数的元数据文件
#      （LinalgAsyncUdf.json / RegressionAsyncUdf.json，共 12 个语句体 UDF）整文件跳过；
#   ② 其余文件只检查 `f'"{excel}"' in src`——函数名子串**存在**即通过。
# 对抗验证：篡改 LinalgAsyncUdf.cs 的 Description、把参数 `object d` 改名 `dRENAMED`，
# `udfgen.py verify` 仍 PASS exit 0，而 scripts/verify-udfgen.ps1 头注与 AGENTS.md 都
# 声称做了"手写 UDF 属性校验"。此处把声称兑现为属性级比对。
#
# 解析要点：
#   * 属性可能**跨行**（RegressionAsyncUdf.cs 的 `[ExcelFunction(Name = ...,\n Description = ...)]`
#     就是折行写法），故按行匹配必然漏——用 iter_declarations 取完整声明块（括号配对、
#     字符串字面量感知），再在 `[ExcelFunction(...)]` 属性块内取值；
#   * Name/Description 必须只在 **[ExcelFunction]** 属性块内找：块外还有
#     `[ExcelArgument(Name = ..., Description = ...)]`，混在一起会取错值；
#   * 比较口径为 one_line（折叠空白）且**不**反转义：元数据 desc 与源码属性同源同形
#     （extract 未反转义，JSON 里存的也是 `\\"`），故两边直接可比；
#   * 可选参数的 `args[].name` 带方括号（如 `[lambda]`），源码里同样带方括号——**原样比**。

def _attr_block(decl_text: str) -> str:
    """从声明原文取出 `[ExcelFunction(...)]` 属性块；失败返回 None。"""
    if not decl_text.startswith("[ExcelFunction"):
        return None
    i = decl_text.find("(")
    if i < 0:
        return None
    j = _match_bracket(decl_text, i)
    if j < 0 or j + 1 >= len(decl_text) or decl_text[j + 1] != "]":
        return None
    return decl_text[:j + 2]


def _q(v):
    """报错用值展示：None → `<缺失>`，其余加引号。"""
    return "<缺失>" if v is None else f'"{v}"'


def check_hand_written(meta: dict, src: str, bad: list) -> None:
    """手写 UDF（generated=false）↔ 元数据的**属性级**比对，差异逐条追加到 bad。"""
    srcname = meta["sourceFile"]
    decls = {}
    for it in iter_declarations(src):
        attr = _attr_block(it["text"])
        if attr is None:
            continue
        m = FN_ATTR.search(attr)
        if m:
            decls.setdefault(m.group("name"), (attr, it["text"]))

    for f in meta["functions"]:
        if f.get("generated"):
            continue
        name = f["excel"]
        hit = decls.get(name)
        if hit is None:
            bad.append(f'{srcname}: 手写 UDF "{name}" 在源码中找不到'
                       f'（元数据有、源码无 [ExcelFunction(Name = "{name}")] 声明）')
            continue
        attr, full = hit
        # Description ← 元数据 desc
        dm = DESC_ATTR.search(attr)
        got_desc = one_line(dm.group("desc")) if dm else None
        if got_desc != one_line(f.get("desc", "")):
            bad.append(f'{srcname}: {name} 字段 Description 不一致：'
                       f'元数据 {_q(one_line(f.get("desc", "")))} / 源码 {_q(got_desc)}')
        # Category ← 元数据 category
        cm = CAT_ATTR.search(attr)
        got_cat = one_line(cm.group("cat")) if cm else None
        if got_cat != one_line(meta["category"]):
            bad.append(f'{srcname}: {name} 字段 Category 不一致：'
                       f'元数据 {_q(one_line(meta["category"]))} / 源码 {_q(got_cat)}')
        # [ExcelArgument] Name 序列 ← 元数据 args[].name（原样比，含可选参数的方括号）
        if f.get("args") is not None:
            want = [a["name"] for a in f["args"]]
            got = ARG_NAME_RE.findall(full)
            if got != want:
                bad.append(f'{srcname}: {name} 字段 ExcelArgument Name 序列不一致：'
                           f'元数据 ({", ".join(want)}) / 源码 ({", ".join(got)})')


def cmd_verify(args):
    """重生成到临时目录 → 与入库文件比对；并校验手写 UDF 的属性与元数据一致。"""
    bad = []
    with tempfile.TemporaryDirectory() as td:
        for meta_file in sorted(META_DIR.glob("*.json")):
            meta = json.loads(meta_file.read_text(encoding="utf-8"))
            code = render_module(meta)
            # 注意：`code` 为空（该元数据文件 0 个可生成函数）**只**跳过生成物比对，
            # 不再整文件 continue——LinalgAsyncUdf/RegressionAsyncUdf 的全部函数都是手写
            # 语句体，旧实现因这一行让它们完全不受检（P2-1）。
            if code:
                on_disk_path = REPO / meta["generatedFile"]
                if not on_disk_path.exists():
                    bad.append(f"{meta['generatedFile']}: 缺失（请运行 udfgen.py generate）")
                elif nows(code) != nows(on_disk_path.read_text(encoding="utf-8")):
                    bad.append(f"{meta['generatedFile']}: 与元数据不一致（被手改或元数据已改未重生成）")
            # 手写 UDF：属性级比对（Description / Category / ExcelArgument Name 序列）
            src_path = REPO / meta["sourceFile"]
            if not src_path.exists():
                bad.append(f"{meta['sourceFile']}: 源码文件缺失")
                continue
            check_hand_written(meta, src_path.read_text(encoding="utf-8"), bad)
    if bad:
        print("FAIL: UDF 生成物与元数据不一致：")
        for b in bad:
            print("  - " + b)
        return 1
    print("PASS: 生成物与元数据一致（含手写 UDF 属性级校验：Description / Category / 参数名序列）")
    return 0


# ────────────────── api-reference 元数据（ADR-0011 收尾） ──────────────────
#
# 文档里 UDF 表行的列语义：
#   | `STATS.MEAN` | (number1) | `double` | 算术平均值 |
#   参数列 = ', '.join(args[].name)；返回/说明两列**元数据里不存在**（desc 是英文 Excel
#   提示语，与中文说明 240/240 全不相同），故由 extract-api 一次性抽入 returns/doc，
#   此后元数据是真源、文档表体是生成物。

API_REF = REPO / "docs/specification/api-reference.md"
API_HEADER = "| 函数 | 参数 | 返回 | 说明 |"
API_SEP = "|------|------|------|------|"
API_BEGIN = "<!-- BEGIN:generated {} -->"
API_END = "<!-- END:generated -->"
API_BEGIN_RE = re.compile(r"^<!-- BEGIN:generated (.+?) -->$")
API_END_RE = re.compile(r"^<!-- END:generated -->$")
# `## STATS.* -- 描述统计` / `## JSON.* / XML.* -- JSON/XML 处理`
API_SECTION_RE = re.compile(r"^##\s+(.+?)\s+--\s")
API_PREFIX_RE = re.compile(r"([A-Z][A-Z0-9_]*)\.\*")
# 表行。说明列允许内嵌**转义竖线**（如 `\|t\|`），故末列用贪婪 .* 收到行尾最后一个 `|`；
# 返回列恒带反引号（生成侧也只写反引号形态）。
API_ROW_RE = re.compile(
    r"^\|\s*`([A-Za-z0-9_.]+)`\s*\|\s*\(([^|]*?)\)\s*\|\s*`([^`|]*)`\s*\|\s*(.*?)\s*\|\s*$")


class ApiError(Exception):
    """元数据 ↔ api-reference 无法安全生成时抛出（各命令捕获后转退出码 1）。"""


def read_api_text(path: Path):
    """→ (text, 行尾风格)。不依赖平台换行翻译：CRLF/LF 都能读，写回时按原风格还原。"""
    raw = path.read_bytes().decode("utf-8")
    return raw.replace("\r\n", "\n"), ("\r\n" if "\r\n" in raw else "\n")


def write_api_text(path: Path, text: str, newline: str) -> None:
    path.write_bytes(text.replace("\n", newline).encode("utf-8"))


def write_json(path: Path, obj) -> None:
    """写回元数据 JSON：沿用仓库既有格式（ensure_ascii=False / indent=1 / 末尾换行），
    并保持该文件既有的行尾风格（仓库存 LF、Windows 工作区常为 CRLF，两者都不引入无谓 diff）。

    json.dumps 会把字符串里的 CR/LF 转义成 \\r\\n，故下方的换行替换只作用于缩进换行。
    """
    text = json.dumps(obj, ensure_ascii=False, indent=1) + "\n"
    newline = "\r\n" if path.exists() and b"\r\n" in path.read_bytes() else "\n"
    write_api_text(path, text, newline)


def api_section_prefixes(heading: str) -> list:
    """`STATS.* -- 描述统计` → ['STATS']；`JSON.* / XML.* -- ...` → ['JSON', 'XML']。"""
    return API_PREFIX_RE.findall(heading)


def load_metas():
    """按文件名排序读取全部元数据 → [(文件名, meta)]（与 generate / verify 同序）。"""
    return [(p.name, json.loads(p.read_text(encoding="utf-8")))
            for p in sorted(META_DIR.glob("*.json"))]


def iter_api_functions(metas):
    """全部函数（sorted 文件序 + 文件内原始顺序）→ 生成排序键即此序：
    先按 doc_section 首次出现先后，再按文件内原序。"""
    for name, meta in metas:
        for f in meta["functions"]:
            yield name, f


def parse_api_doc(text: str):
    """解析 api-reference.md 的 UDF 表行 → (sections, errors)。

    sections = [ {prefixes: [...], rows: [{line, excel, params, returns, doc, section}]} ]。
    只认「`## X.* -- ...` 标题 + `| \\`NAME\\` | (params) | \\`ret\\` | 说明 |` 行」，
    错误参考等非 UDF 表格（标题无 `.*`）不进入扫描域。
    """
    sections, errs, cur = [], [], None
    for no, ln in enumerate(text.split("\n"), 1):
        if ln.startswith("## "):
            m = API_SECTION_RE.match(ln)
            pre = api_section_prefixes(m.group(1)) if m else []
            cur = dict(prefixes=pre, rows=[]) if pre else None
            if cur:
                sections.append(cur)
            continue
        if cur is None or not ln.startswith("|"):
            continue
        if ln.strip() in (API_HEADER, API_SEP):
            continue
        m = API_ROW_RE.match(ln)
        if not m:
            errs.append(f"api-reference.md:{no} 无法解析的表格行：{ln[:70]}")
            continue
        head = m.group(1).split(".")[0]
        if head not in cur["prefixes"]:
            errs.append(f"api-reference.md:{no} {m.group(1)} 不在章节 "
                        f"{cur['prefixes'][0]}.* 内（章节={cur['prefixes']}）")
            continue
        cur["rows"].append(dict(line=no, excel=m.group(1), params=m.group(2),
                                returns=m.group(3), doc=m.group(4), section=head))
    return sections, errs


def api_block(prefixes, fns) -> list:
    """渲染一段带生成标记的表 → 行列表。

    标记里的前缀取该节**首个**前缀：`JSON.* / XML.*` 一节一表覆盖两个前缀（表内混排
    JSON.* 与 XML.* 行），标记是该表的锚点标识，故取首个（JSON）。
    """
    rows = []
    for f in fns:
        params = ", ".join(a["name"] for a in f.get("args", []))
        rows.append(f'| `{f["excel"]}` | ({params}) | `{f["returns"]}` | {f["doc"]} |')
    return [API_BEGIN.format(prefixes[0]), API_HEADER, API_SEP] + rows + [API_END]


def render_api_doc(text: str, metas) -> str:
    """把每节表格替换为元数据渲染结果。

    幂等性来源：识别顺序是「① 已有 BEGIN…END 标记块 → 整块替换；② 否则从表头
    `| 函数 | 参数 | 返回 | 说明 |` 到连续 `|` 行 → 整段替换」。块内容是元数据的纯函数，
    故第二次运行命中 ① 且写出完全相同的字节。表格以外的 prose / 引用块 / `---` 一律原样保留。
    """
    sections, errs = parse_api_doc(text)
    if errs:
        raise ApiError("api-reference.md 存在无法解析的表格行：\n  " + "\n  ".join(errs))

    ordered = [f for _, f in iter_api_functions(metas)]
    fn_by_excel = {f["excel"]: f for f in ordered}
    if len(fn_by_excel) != len(ordered):
        raise ApiError("元数据中存在重复的函数名（excel 字段）")
    lack = [f["excel"] for f in ordered
            if "returns" not in f or "doc" not in f or not f.get("doc_section")]
    if lack:
        raise ApiError("以下函数缺少 returns/doc/doc_section 字段——"
                       "请先运行 python tools/udfgen.py extract-api：\n  " + "\n  ".join(lack[:20]))
    # 列内容不得含未转义的表分隔符：一旦写进表行，行会被解析成 5 列，
    # 而 verify-api 只比"重渲染是否等于磁盘"——两者同源于元数据，坏行会自洽通过。
    broken = [f["excel"] for f in ordered
              if re.search(r"(?<!\\)\|", f["doc"]) or "\n" in f["doc"]
              or "|" in f["returns"] or "`" in f["returns"]]
    if broken:
        raise ApiError("以下函数的 doc/returns 含未转义的表格分隔符（会破坏表行结构），"
                       "请用 \\| 转义：\n  " + "\n  ".join(broken))

    known = {p for sec in sections for p in sec["prefixes"]}
    stray = sorted({f["doc_section"] for f in ordered} - known)
    if stray:
        raise ApiError("以下 doc_section 在 api-reference.md 中没有对应章节：\n  "
                       + "\n  ".join(stray))

    section_rows = {}
    for sec in sections:
        key = tuple(sec["prefixes"])
        rows = [f for f in ordered if f["doc_section"] in sec["prefixes"]]
        if not rows:
            raise ApiError(f"章节 {sec['prefixes'][0]}.* 没有任何元数据函数")
        section_rows[key] = rows
        for r in sec["rows"]:  # 反向守卫：手改/挪错节的行不静默丢弃
            f = fn_by_excel.get(r["excel"])
            if f is None:
                raise ApiError(f"文档第 {r['line']} 行 {r['excel']} 在元数据中不存在")
            if f["doc_section"] not in sec["prefixes"]:
                raise ApiError(f"文档第 {r['line']} 行 {r['excel']} 的 doc_section="
                               f"{f['doc_section']} 与本节 {sec['prefixes']} 不符")

    lines = text.split("\n")
    out, i, cur, done = [], 0, None, set()
    while i < len(lines):
        ln = lines[i]
        if ln.startswith("## "):
            m = API_SECTION_RE.match(ln)
            pre = api_section_prefixes(m.group(1)) if m else []
            cur = tuple(pre) if pre and tuple(pre) in section_rows else None
            out.append(ln)
            i += 1
            continue
        begin = API_BEGIN_RE.match(ln) if cur else None
        if begin:
            if begin.group(1) != cur[0]:
                raise ApiError(f"第 {i + 1} 行标记前缀 {begin.group(1)} 与章节 {cur[0]}.* 不符")
            j = i + 1
            while j < len(lines) and not API_END_RE.match(lines[j]):
                j += 1
            if j >= len(lines):
                raise ApiError(f"第 {i + 1} 行的 BEGIN 标记缺少配对 END")
            out.extend(api_block(cur, section_rows[cur]))
            done.add(cur)
            i = j + 1
            continue
        if cur and ln.strip() == API_HEADER:
            j = i + 1
            while j < len(lines) and lines[j].startswith("|"):
                j += 1
            out.extend(api_block(cur, section_rows[cur]))
            done.add(cur)
            i = j
            continue
        out.append(ln)
        i += 1
    missing = [k[0] for k in section_rows if k not in done]
    if missing:
        raise ApiError("以下章节找不到表格或生成标记（无法原地重写）：\n  " + "\n  ".join(missing))
    return "\n".join(out)


def cmd_extract_api(args):
    """引导式抽取：把 api-reference.md 的「返回 / 说明」两列 + 章节前缀写进元数据。

    **一次性工具**：这两列一旦进入元数据就是真源，二次运行只会用文档现值覆盖（若文档
    尚未由 generate-api 生成，等于把抽取结果原样回写；若文档被手改过，则会把元数据带偏）。
    故默认拒绝覆盖已有 returns 字段的元数据，需显式 --force。
    """
    text, _ = read_api_text(API_REF)
    sections, errs = parse_api_doc(text)
    if errs:
        print("FAIL: api-reference.md 存在无法解析/归属错误的表格行：")
        for e in errs:
            print("  - " + e)
        return 1

    doc, dup = {}, []
    for sec in sections:
        for r in sec["rows"]:
            if r["excel"] in doc:
                dup.append(f"{r['excel']}（第 {doc[r['excel']]['line']} 行与第 {r['line']} 行）")
            doc[r["excel"]] = r
    if dup:
        print("FAIL: api-reference.md 表格行重复：\n  - " + "\n  - ".join(dup))
        return 1

    metas = load_metas()
    fn_by_excel = {}
    for name, f in iter_api_functions(metas):
        if f["excel"] in fn_by_excel:
            print(f'FAIL: 元数据函数名重复：{f["excel"]}（{fn_by_excel[f["excel"]]} / {name}）')
            return 1
        fn_by_excel[f["excel"]] = name

    unmatched = sorted(set(doc) - set(fn_by_excel))
    uncovered = sorted(set(fn_by_excel) - set(doc))
    if unmatched or uncovered:
        print(f"FAIL: 文档表行 {len(doc)} 与元数据函数 {len(fn_by_excel)} 不是一一对应：")
        for x in unmatched:
            print(f"  - 文档有、元数据无：{x}（第 {doc[x]['line']} 行）")
        for x in uncovered:
            print(f"  - 元数据有、文档无：{x}")
        return 1

    if not args.force:
        holders = sorted(name for name, meta in metas
                         if any("returns" in f for f in meta["functions"]))
        if holders:
            print("FAIL: 以下元数据已有 returns 字段——extract-api 是**引导工具**，"
                  "二次运行会用文档现值覆盖已抽取的返回/说明列。确认要重抽请加 --force：")
            for h in holders:
                print("  - " + h)
            return 1

    n = 0
    for name, meta in metas:
        changed = 0
        for f in meta["functions"]:
            r = doc.get(f["excel"])
            if r is None:
                continue
            # 键顺序稳定：三个新字段追加在既有键之后（json.dumps 保持插入序）
            f["returns"] = r["returns"]
            f["doc"] = r["doc"]
            f["doc_section"] = r["section"]
            changed += 1
        if changed:
            write_json(META_DIR / name, meta)
        n += changed
        print(f"  {name:24} 写入 {changed:3} 个函数的 returns/doc/doc_section")
    print(f"\n处理了 {len(doc)} 行 / 匹配 {n} 个函数")
    if n != len(doc):
        print("FAIL: 匹配数与文档行数不符")
        return 1
    print(f"PASS: {len(metas)} 个元数据文件已更新（returns / doc / doc_section）")
    return 0


def cmd_generate_api(args):
    """从元数据原地重写 api-reference.md 的表体（prose / 标题 / `---` 原样保留）。"""
    text, newline = read_api_text(API_REF)
    try:
        new = render_api_doc(text, load_metas())
    except ApiError as e:
        print("FAIL: " + str(e))
        return 1
    sections, _ = parse_api_doc(new)
    n_fn = sum(len(s["rows"]) for s in sections)
    if new != text:
        write_api_text(API_REF, new, newline)
        print(f"  docs/specification/api-reference.md 已重写（{len(sections)} 张表 / {n_fn} 个函数）")
    else:
        print(f"  docs/specification/api-reference.md 无变化（{len(sections)} 张表 / {n_fn} 个函数）")
    print("PASS: api-reference.md 表体已由 udf-metadata/ 生成")
    return 0


def cmd_verify_api(args):
    """在内存中重渲染 → 与磁盘逐字节比较（先统一行尾为 \\n，不因 CRLF/LF 误报）。"""
    text, _ = read_api_text(API_REF)
    try:
        expect = render_api_doc(text, load_metas())
    except ApiError as e:
        print("FAIL: " + str(e))
        return 1
    got = text.replace("\r\n", "\n").split("\n")
    exp = expect.replace("\r\n", "\n").split("\n")
    if got == exp:
        sections, _ = parse_api_doc(expect)
        n_fn = sum(len(s["rows"]) for s in sections)
        print(f"PASS: api-reference.md 与元数据一致（{len(sections)} 张表 / {n_fn} 个函数）")
        return 0
    at = next((i for i in range(min(len(got), len(exp))) if got[i] != exp[i]),
              min(len(got), len(exp)))
    ndiff = sum(1 for a, b in zip(got, exp) if a != b) + abs(len(got) - len(exp))
    print(f"FAIL: docs/specification/api-reference.md 与元数据不一致"
          f"（首个差异在第 {at + 1} 行，共 {ndiff} 行不同）。"
          f"修复：python tools/udfgen.py generate-api")
    lo, hi = max(0, at - 3), at + 4
    print(f"  磁盘（第 {lo + 1}-{min(hi, len(got))} 行）：")
    for k in range(lo, min(hi, len(got))):
        print(f"    {k + 1:4} | {got[k]}")
    print(f"  期望（第 {lo + 1}-{min(hi, len(exp))} 行）：")
    for k in range(lo, min(hi, len(exp))):
        print(f"    {k + 1:4} | {exp[k]}")
    return 1


def main():
    ap = argparse.ArgumentParser(description="UDF 元数据化与源生成")
    sub = ap.add_subparsers(dest="cmd", required=True)
    e = sub.add_parser("extract", help="引导式抽取（元数据建立后勿再运行）")
    e.add_argument("--force", action="store_true",
                   help="允许覆盖已存在的元数据（仅在确知要重抽时使用）")
    e.set_defaults(func=cmd_extract)
    g = sub.add_parser("generate")
    g.add_argument("--only", nargs="*", default=None,
                   help="只生成指定元数据文件（不含 .json），用于逐模块迁移")
    g.set_defaults(func=cmd_generate)
    m = sub.add_parser("migrate", help="删除已由元数据接管的声明并置类为 partial")
    m.add_argument("--only", nargs="*", default=None)
    m.set_defaults(func=cmd_migrate)
    sub.add_parser("verify").set_defaults(func=cmd_verify)
    ea = sub.add_parser("extract-api",
                        help="从 api-reference.md 抽取 返回/说明 两列进元数据（一次性引导）")
    ea.add_argument("--force", action="store_true",
                    help="允许覆盖已存在 returns 字段的元数据（仅在确知要重抽时使用）")
    ea.set_defaults(func=cmd_extract_api)
    ga = sub.add_parser("generate-api", aliases=["gen-api"],
                        help="从元数据生成 api-reference.md 的表体")
    ga.set_defaults(func=cmd_generate_api)
    sub.add_parser("verify-api",
                   help="重渲染 api-reference.md 并与磁盘比对").set_defaults(func=cmd_verify_api)
    a = ap.parse_args()
    return a.func(a)


if __name__ == "__main__":
    sys.exit(main())
