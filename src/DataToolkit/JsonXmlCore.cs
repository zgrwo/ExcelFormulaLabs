using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.DataToolkit
{
    /// <summary>JSON parsing/querying and XML XPath/table conversion. Ported from JsonUtils.bas + XmlUtils.bas.</summary>
    internal static class JsonXmlCore
    {
        // ── JSON ───────────────────────────────────────────────────────────
        internal static object? JsonParse(string json)
        { using var d=JsonDocument.Parse(json, new JsonDocumentOptions{MaxDepth=64}); return Elm(d.RootElement); }

        internal static object? JsonQuery(string json, string path)
        { using var d=JsonDocument.Parse(json, new JsonDocumentOptions{MaxDepth=64}); return Q(d.RootElement,path); }

        internal static bool JsonValidate(string json)
        { try{using var _=JsonDocument.Parse(json, new JsonDocumentOptions{MaxDepth=64});return true;}catch(Exception ex) when(ExceptionFilters.IsCatchable(ex)){return false;} }

        internal static string JsonPrettify(string json)
        { using var d=JsonDocument.Parse(json, new JsonDocumentOptions{MaxDepth=64}); return JsonSerializer.Serialize(d,new JsonSerializerOptions{WriteIndented=true}); }

        internal static object[,]? JsonToTable(string json)
        {
            using var d=JsonDocument.Parse(json, new JsonDocumentOptions{MaxDepth=64});
            if(d.RootElement.ValueKind!=JsonValueKind.Array)return null;
            var rows=new List<Dictionary<string,object?>>(); var keys=new List<string>(); var seen=new HashSet<string>();
            foreach(var el in d.RootElement.EnumerateArray())
            { if(el.ValueKind==JsonValueKind.Object){ var row=new Dictionary<string,object?>(); foreach(var p in el.EnumerateObject()){row[p.Name]=Elm(p.Value);if(seen.Add(p.Name))keys.Add(p.Name);} rows.Add(row); } }
            if(rows.Count==0)return null;
            // 纵深防御——UDF 输入经单元格字符串（32,767 字符）天然有界，但 .NET 直调方无此约束；
            // 对齐 PivotCore/RangeExportCore 的规模纪律（分配前拒绝）。
            if(rows.Count>100_000) throw new ArgumentException($"JSON input produces {rows.Count} rows; maximum is 100000.");
            var ka=keys.ToArray(); var r=new object[rows.Count+1,ka.Length];
            for(int c=0;c<ka.Length;c++)r[0,c]=ka[c];
            // 网格单元的 null =「空单元格」哨兵（JSON null 值与缺键均合法），v! / null! 豁免的是
            // object[,] 元素类型无法表达可空，而非断言运行时非空（object?[,] 本地数组方案会在
            // 返回处触发 CS8619，不可用）。
            for(int i=0;i<rows.Count;i++)for(int c=0;c<ka.Length;c++)r[i+1,c]=rows[i].TryGetValue(ka[c],out var v)?v!:null!;
            return r;
        }

        private static object? Elm(JsonElement e)=>e.ValueKind switch
        { JsonValueKind.Null=>null,JsonValueKind.True=>true,JsonValueKind.False=>false,JsonValueKind.String=>e.GetString(),JsonValueKind.Number=>ElmNumber(e),JsonValueKind.Array=>e.EnumerateArray().Select(Elm).ToArray(),JsonValueKind.Object=>ElmObject(e),_=>e.GetRawText() };

        /// <summary>重复键须统一为「后者覆盖」（JSON 解析器通行语义，Python json.loads /
        /// JSON.NET 同款）：ToDictionary 遇重复键抛异常，而 QUERY（TryGetProperty 取首个）与
        /// TOTABLE（后写覆盖）各自成功——三条通道行为矛盾。</summary>
        private static object? ElmObject(JsonElement e)
        {
            var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var p in e.EnumerateObject()) dict[p.Name] = Elm(p.Value);
            return dict;
        }

        /// <summary>取同名属性的最后一次出现（与 ElmObject 的后者覆盖语义一致）。</summary>
        private static bool TryGetPropertyLast(JsonElement e, string name, out JsonElement value)
        {
            value = default;
            bool found = false;
            foreach (var p in e.EnumerateObject())
                if (p.Name == name) { value = p.Value; found = true; }
            return found;
        }

        private static object? ElmNumber(JsonElement e)
        {
            if (e.TryGetInt64(out long l)) return l;
            try
            {
                double d = e.GetDouble();
                // Guard against IEEE 754 Infinity/NaN from extreme JSON numbers (e.g. 1e999).
                // Aligns with RangeExportCore.JsonVal which returns "null" for these.
                if (double.IsNaN(d) || double.IsInfinity(d)) return null;
                return d;
            }
            catch (Exception ex) when (ExceptionFilters.IsCatchable(ex))
            {
                // net48 System.Text.Json (NuGet) throws FormatException for overflow values
                // like 1e999; net8 built-in returns Infinity. Both → null.
                return null;
            }
        }

        private static object? Q(JsonElement e,string p)
        { foreach(var s in p.Split('.')){int b=s.IndexOf('[');string k=b>=0?s.Substring(0,b):s; if(!string.IsNullOrEmpty(k)&&e.ValueKind==JsonValueKind.Object){ if(TryGetPropertyLast(e,k,out JsonElement c))e=c;else return null; } if(b>=0&&e.ValueKind==JsonValueKind.Array){ int idxLen=s.Length-b-2; if(idxLen>=0&&int.TryParse(s.Substring(b+1,idxLen),out int ix)&&ix>=0&&ix<e.GetArrayLength())e=e[ix];else return null; } } return Elm(e); }

        // ── XML ────────────────────────────────────────────────────────────

        /// <summary>Secure XmlReader settings: no DTD, no external entities.
        /// XDocument 构树按元素深度递归，20,000 层直调会 StackOverflow（进程退出，不可捕获）。
        /// 解析前用迭代 XmlReader 预扫深度（MaxXmlDepth 远低于栈上限），超限即抛 XmlException。</summary>
        private const int MaxXmlDepth = 1024;

        private static readonly XmlReaderSettings SecureXmlSettings = new()
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersFromEntities = 0,
        };

        private static XDocument ParseXmlSafe(string xml)
        {
            // 迭代预扫：不构树、不递归，只统计嵌套深度（自闭合元素无 EndElement）。
            using (var scan = XmlReader.Create(new StringReader(xml), SecureXmlSettings))
            {
                int depth = 0;
                while (scan.Read())
                {
                    if (scan.NodeType == XmlNodeType.Element)
                    {
                        if (++depth > MaxXmlDepth)
                            throw new XmlException(
                                $"XML nesting depth exceeds the limit of {MaxXmlDepth}.");
                        if (scan.IsEmptyElement) depth--;
                    }
                    else if (scan.NodeType == XmlNodeType.EndElement)
                    {
                        depth--;
                    }
                }
            }
            using var reader = XmlReader.Create(new StringReader(xml), SecureXmlSettings);
            return XDocument.Load(reader);
        }

        internal static string[] XmlXPath(string xml, string xpath)
        { try{var d=ParseXmlSafe(xml);return d.XPathSelectElements(xpath).Select(e=>e.Value).ToArray();}catch(Exception ex) when(ExceptionFilters.IsCatchable(ex)){System.Diagnostics.Debug.WriteLine($"[XmlXPath] Failed: {ex.Message}");return Array.Empty<string>();} }

        internal static object[,]? XmlToTable(string xml, string? rowPath=null)
            // 缺元素单元格写 null =「空单元格」哨兵，null! 豁免可空性分析。
            // 同 JsonToTable——行数 >100_000 分配前拒绝（.NET 直调方纵深防御）。
        { try{var d=ParseXmlSafe(xml);var rows=rowPath!=null?d.XPathSelectElements(rowPath):d.Root?.Elements()??Enumerable.Empty<XElement>();var rl=rows.ToList();if(rl.Count==0)return null;if(rl.Count>100_000)throw new ArgumentException($"XML input produces {rl.Count} rows; maximum is 100000.");var cn=rl.SelectMany(r=>r.Elements()).Select(e=>e.Name.LocalName).Distinct().ToArray();var rt=new object[rl.Count+1,cn.Length];for(int c=0;c<cn.Length;c++)rt[0,c]=cn[c];for(int i=0;i<rl.Count;i++){var row=rl[i];for(int c=0;c<cn.Length;c++){var el=row.Element(cn[c]);rt[i+1,c]=el!=null?el.Value:null!;}}return rt;}catch(Exception ex) when(ExceptionFilters.IsCatchable(ex)){System.Diagnostics.Debug.WriteLine($"[XmlToTable] Failed: {ex.Message}");return null;} }

        internal static bool XmlValidate(string xml)
        { try{ParseXmlSafe(xml);return true;}catch(Exception ex) when(ExceptionFilters.IsCatchable(ex)){return false;} }
    }
}
