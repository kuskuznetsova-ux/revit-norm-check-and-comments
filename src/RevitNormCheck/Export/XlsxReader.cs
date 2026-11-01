using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace RevitNormCheck.Export
{
    // Читает первый лист .xlsx как таблицу строк. Поддерживает файлы, сохранённые Excel
    // (общие строки) и записанные XlsxWriter (inline-строки).
    public static class XlsxReader
    {
        private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private static readonly XNamespace Rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        private static readonly XNamespace PkgRel = "http://schemas.openxmlformats.org/package/2006/relationships";

        public static List<string[]> ReadFirstSheet(string path) => ReadSheets(path).First().Value;

        // Все листы книги: название и таблица строк.
        public static List<KeyValuePair<string, List<string[]>>> ReadSheets(string path)
        {
            using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var zip = new ZipArchive(file, ZipArchiveMode.Read))
            {
                var shared = ReadSharedStrings(zip);
                var result = new List<KeyValuePair<string, List<string[]>>>();

                foreach (var sheetInfo in FindSheets(zip))
                {
                    var sheet = Load(zip, sheetInfo.Value);
                    var rows = new List<string[]>();

                    foreach (var row in sheet.Descendants(Main + "row"))
                    {
                        var cells = new Dictionary<int, string>();
                        var max = -1;
                        foreach (var cell in row.Elements(Main + "c"))
                        {
                            var index = ColumnIndex((string)cell.Attribute("r"));
                            cells[index] = CellText(cell, shared);
                            if (index > max) max = index;
                        }
                        var values = new string[max + 1];
                        for (int i = 0; i <= max; i++) values[i] = cells.ContainsKey(i) ? cells[i] : "";
                        rows.Add(values);
                    }
                    result.Add(new KeyValuePair<string, List<string[]>>(sheetInfo.Key, rows));
                }
                return result;
            }
        }

        private static string CellText(XElement cell, List<string> shared)
        {
            var type = (string)cell.Attribute("t");
            if (type == "inlineStr")
                return string.Concat(cell.Descendants(Main + "t").Select(t => t.Value));

            var raw = (string)cell.Element(Main + "v") ?? "";
            if (type == "s")
            {
                int index;
                return int.TryParse(raw, out index) && index >= 0 && index < shared.Count ? shared[index] : "";
            }
            return raw;
        }

        private static List<string> ReadSharedStrings(ZipArchive zip)
        {
            var result = new List<string>();
            var entry = zip.GetEntry("xl/sharedStrings.xml");
            if (entry == null) return result;
            var doc = Load(zip, "xl/sharedStrings.xml");
            foreach (var si in doc.Descendants(Main + "si"))
                result.Add(string.Concat(si.Descendants(Main + "t").Select(t => t.Value)));
            return result;
        }

        // Название листа и путь к его xml внутри архива, в порядке книги.
        private static List<KeyValuePair<string, string>> FindSheets(ZipArchive zip)
        {
            var workbook = Load(zip, "xl/workbook.xml");
            var rels = Load(zip, "xl/_rels/workbook.xml.rels");
            var targets = rels.Descendants(PkgRel + "Relationship")
                .ToDictionary(r => (string)r.Attribute("Id"), r => (string)r.Attribute("Target"));

            var list = new List<KeyValuePair<string, string>>();
            foreach (var sheet in workbook.Descendants(Main + "sheet"))
            {
                var id = (string)sheet.Attribute(Rel + "id");
                string target;
                if (id == null || !targets.TryGetValue(id, out target)) continue;
                var path = target.StartsWith("/") ? target.TrimStart('/') : "xl/" + target;
                list.Add(new KeyValuePair<string, string>((string)sheet.Attribute("name"), path));
            }
            return list;
        }

        private static XDocument Load(ZipArchive zip, string name)
        {
            var entry = zip.GetEntry(name);
            using (var reader = new StreamReader(entry.Open(), Encoding.UTF8))
                return XDocument.Load(reader);
        }

        private static int ColumnIndex(string reference)
        {
            var index = 0;
            foreach (var ch in reference)
            {
                if (ch < 'A' || ch > 'Z') break;
                index = index * 26 + (ch - 'A' + 1);
            }
            return index - 1;
        }
    }
}
