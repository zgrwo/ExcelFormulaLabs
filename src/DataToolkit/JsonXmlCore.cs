using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;
using System.Xml.XPath;
using ExcelVbaLibraries.Foundation;

namespace ExcelVbaLibraries.DataToolkit
{
    /// <summary>JSON parsing/querying and XML XPath/table conversion. Ported from JsonUtils.bas + XmlUtils.bas.</summary>
    internal static class JsonXmlCore
    {
        // ── JSON ───────────────────────────────────────────────────────────
        internal static object? JsonParse(string json)
        { using var d=JsonDocument.Parse(json); return Elm(d.RootElement); }

        internal static object? JsonQuery(string json, string path)
        { using var d=JsonDocument.Parse(json); return Q(d.RootElement,path); }

        internal static bool JsonValidate(string json)
        { try{using var _=JsonDocument.Parse(json);return true;}catch{return false;} }

        internal static string JsonPrettify(string json)
        { using var d=JsonDocument.Parse(json); return JsonSerializer.Serialize(d,new JsonSerializerOptions{WriteIndented=true}); }

        internal static object[,]? JsonToTable(string json)
        {
            using var d=JsonDocument.Parse(json);
            if(d.RootElement.ValueKind!=JsonValueKind.Array)return null;
            var rows=new List<Dictionary<string,object?>>(); var keys=new HashSet<string>();
            foreach(var el in d.RootElement.EnumerateArray())
            { if(el.ValueKind==JsonValueKind.Object){ var row=new Dictionary<string,object?>(); foreach(var p in el.EnumerateObject()){row[p.Name]=Elm(p.Value);keys.Add(p.Name);} rows.Add(row); } }
            if(rows.Count==0)return null;
            var ka=keys.ToArray(); var r=new object[rows.Count+1,ka.Length];
            for(int c=0;c<ka.Length;c++)r[0,c]=ka[c];
            for(int i=0;i<rows.Count;i++)for(int c=0;c<ka.Length;c++)r[i+1,c]=rows[i].TryGetValue(ka[c],out var v)?(v??ExcelEmpty.Value):ExcelEmpty.Value;
            return r;
        }

        private static object? Elm(JsonElement e)=>e.ValueKind switch
        { JsonValueKind.Null=>null,JsonValueKind.True=>true,JsonValueKind.False=>false,JsonValueKind.String=>e.GetString(),JsonValueKind.Number=>e.TryGetInt64(out long l)?l:e.GetDouble(),JsonValueKind.Array=>e.EnumerateArray().Select(Elm).ToArray(),JsonValueKind.Object=>e.EnumerateObject().ToDictionary(p=>p.Name,p=>Elm(p.Value)),_=>e.GetRawText() };

        private static object? Q(JsonElement e,string p)
        { foreach(var s in p.Split('.')){int b=s.IndexOf('[');string k=b>=0?s.Substring(0,b):s; if(!string.IsNullOrEmpty(k)&&e.ValueKind==JsonValueKind.Object){ if(e.TryGetProperty(k,out JsonElement c))e=c;else return null; } if(b>=0&&e.ValueKind==JsonValueKind.Array){ if(int.TryParse(s.Substring(b+1,s.Length-b-2),out int ix)&&ix<e.GetArrayLength())e=e[ix];else return null; } } return Elm(e); }

        // ── XML ────────────────────────────────────────────────────────────
        internal static string[] XmlXPath(string xml, string xpath)
        { try{var d=XDocument.Parse(xml);return d.XPathSelectElements(xpath).Select(e=>e.Value).ToArray();}catch{return Array.Empty<string>();} }

        internal static object[,]? XmlToTable(string xml, string? rowPath=null)
        { try{var d=XDocument.Parse(xml);var rows=rowPath!=null?d.XPathSelectElements(rowPath):d.Root?.Elements()??Enumerable.Empty<XElement>();var rl=rows.ToList();if(rl.Count==0)return null;var cn=rl.SelectMany(r=>r.Elements()).Select(e=>e.Name.LocalName).Distinct().ToArray();var rt=new object[rl.Count+1,cn.Length];for(int c=0;c<cn.Length;c++)rt[0,c]=cn[c];for(int i=0;i<rl.Count;i++){var row=rl[i];for(int c=0;c<cn.Length;c++){var el=row.Element(cn[c]);rt[i+1,c]=el!=null?(object?)el.Value??ExcelEmpty.Value:ExcelEmpty.Value;}}return rt;}catch{return null;} }

        internal static bool XmlValidate(string xml)
        { try{XDocument.Parse(xml);return true;}catch{return false;} }
    }
}
