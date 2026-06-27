using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using FormulaLabs.Foundation;

namespace FormulaLabs.DataToolkit
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
        { try{using var _=JsonDocument.Parse(json, new JsonDocumentOptions{MaxDepth=64});return true;}catch(Exception ex) when(ex is not OutOfMemoryException and not StackOverflowException and not AccessViolationException){return false;} }

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
            var ka=keys.ToArray(); var r=new object[rows.Count+1,ka.Length];
            for(int c=0;c<ka.Length;c++)r[0,c]=ka[c];
            for(int i=0;i<rows.Count;i++)for(int c=0;c<ka.Length;c++)r[i+1,c]=rows[i].TryGetValue(ka[c],out var v)?(v??ExcelEmpty.Value):ExcelEmpty.Value;
            return r;
        }

        private static object? Elm(JsonElement e)=>e.ValueKind switch
        { JsonValueKind.Null=>null,JsonValueKind.True=>true,JsonValueKind.False=>false,JsonValueKind.String=>e.GetString(),JsonValueKind.Number=>ElmNumber(e),JsonValueKind.Array=>e.EnumerateArray().Select(Elm).ToArray(),JsonValueKind.Object=>e.EnumerateObject().ToDictionary(p=>p.Name,p=>Elm(p.Value)),_=>e.GetRawText() };

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
            catch (Exception ex) when (ex is not OutOfMemoryException
                and not StackOverflowException
                and not AccessViolationException)
            {
                // net48 System.Text.Json (NuGet) throws FormatException for overflow values
                // like 1e999; net8 built-in returns Infinity. Both → null.
                return null;
            }
        }

        private static object? Q(JsonElement e,string p)
        { foreach(var s in p.Split('.')){int b=s.IndexOf('[');string k=b>=0?s.Substring(0,b):s; if(!string.IsNullOrEmpty(k)&&e.ValueKind==JsonValueKind.Object){ if(e.TryGetProperty(k,out JsonElement c))e=c;else return null; } if(b>=0&&e.ValueKind==JsonValueKind.Array){ if(int.TryParse(s.Substring(b+1,s.Length-b-2),out int ix)&&ix<e.GetArrayLength())e=e[ix];else return null; } } return Elm(e); }

        // ── XML ────────────────────────────────────────────────────────────

        /// <summary>Secure XmlReader settings: no DTD, no external entities.</summary>
        private static readonly XmlReaderSettings SecureXmlSettings = new()
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersFromEntities = 0,
        };

        private static XDocument ParseXmlSafe(string xml)
        {
            using var reader = XmlReader.Create(new StringReader(xml), SecureXmlSettings);
            return XDocument.Load(reader);
        }

        internal static string[] XmlXPath(string xml, string xpath)
        { try{var d=ParseXmlSafe(xml);return d.XPathSelectElements(xpath).Select(e=>e.Value).ToArray();}catch(Exception ex) when(ex is not OutOfMemoryException and not StackOverflowException and not AccessViolationException){return Array.Empty<string>();} }

        internal static object[,]? XmlToTable(string xml, string? rowPath=null)
        { try{var d=ParseXmlSafe(xml);var rows=rowPath!=null?d.XPathSelectElements(rowPath):d.Root?.Elements()??Enumerable.Empty<XElement>();var rl=rows.ToList();if(rl.Count==0)return null;var cn=rl.SelectMany(r=>r.Elements()).Select(e=>e.Name.LocalName).Distinct().ToArray();var rt=new object[rl.Count+1,cn.Length];for(int c=0;c<cn.Length;c++)rt[0,c]=cn[c];for(int i=0;i<rl.Count;i++){var row=rl[i];for(int c=0;c<cn.Length;c++){var el=row.Element(cn[c]);rt[i+1,c]=el!=null?(object?)el.Value??ExcelEmpty.Value:ExcelEmpty.Value;}}return rt;}catch(Exception ex) when(ex is not OutOfMemoryException and not StackOverflowException and not AccessViolationException){return null;} }

        internal static bool XmlValidate(string xml)
        { try{ParseXmlSafe(xml);return true;}catch(Exception ex) when(ex is not OutOfMemoryException and not StackOverflowException and not AccessViolationException){return false;} }
    }
}
