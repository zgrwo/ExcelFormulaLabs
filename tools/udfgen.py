#!/usr/bin/env python3
"""udfgen.py — UDF 元数据化与源生成（见 docs/adr/0011-udf-metadata-and-codegen.md）。

三个子命令：
  extract   从 src/**/*Udf.cs 抽取元数据到 udf-metadata/，并做 **token 级往返校验**；
            校验不通过的函数不进入生成集合（generated=false），保留手写。
  generate  从 udf-metadata/ 生成 src/**/<UdfFile>.g.cs（仅 generated=true 的函数）。
  verify    重新生成到临时目录并与入库文件比对；同时校验手写 UDF 的属性与元数据一致。
            任何差异 → 退出码 1（供 pre-commit / CI 使用）。

设计要点：
  * 元数据里的 expr 是**函数体表达式原文**，等价比对靠"删除全部空白后逐字符相同"，
    因此"生成即原代码"是构造性保证，而非靠推理。
  * 生成物入库（非 obj/），使 verify-docs 等按源码文本统计的门禁继续有效。
"""
import argparse
import json
import re
import sys
import tempfile
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
META_DIR = REPO / "udf-metadata"

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
ARG_ATTR = re.compile(
    r'\[ExcelArgument\(Name\s*=\s*"(?P<name>[^"]*)"\s*,\s*'
    r'Description\s*=\s*"(?P<desc>(?:[^"\\]|\\.)*)"\)\]\s*(?P<decl>.+)$', re.S)
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


def cmd_verify(args):
    """重生成到临时目录 → 与入库文件比对；并校验手写 UDF 属性与元数据一致。"""
    bad = []
    with tempfile.TemporaryDirectory() as td:
        for meta_file in sorted(META_DIR.glob("*.json")):
            meta = json.loads(meta_file.read_text(encoding="utf-8"))
            code = render_module(meta)
            on_disk_path = REPO / meta["generatedFile"]
            if not code:
                continue
            if not on_disk_path.exists():
                bad.append(f"{meta['generatedFile']}: 缺失（请运行 udfgen.py generate）")
                continue
            if nows(code) != nows(on_disk_path.read_text(encoding="utf-8")):
                bad.append(f"{meta['generatedFile']}: 与元数据不一致（被手改或元数据已改未重生成）")
            # 手写 UDF：源文件里必须存在同名函数且属性一致
            src = (REPO / meta["sourceFile"]).read_text(encoding="utf-8")
            for f in meta["functions"]:
                if f.get("generated"):
                    continue
                if f'"{f["excel"]}"' not in src:
                    bad.append(f'{meta["sourceFile"]}: 手写 UDF "{f["excel"]}" 在源码中找不到')
    if bad:
        print("FAIL: UDF 生成物与元数据不一致：")
        for b in bad:
            print("  - " + b)
        return 1
    print("PASS: 生成物与元数据一致（含手写 UDF 属性校验）")
    return 0


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
    a = ap.parse_args()
    return a.func(a)


if __name__ == "__main__":
    sys.exit(main())
