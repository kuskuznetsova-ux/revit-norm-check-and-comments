using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Text;

namespace RevitNormCheck.Export
{
    public class SheetData
    {
        public string Name { get; set; }
        public IList<string> Headers { get; set; }
        public IList<IList<object>> Rows { get; set; }
    }

    // Минимальная запись .xlsx без внешних библиотек: строки и числа, жирная шапка,
    // закреплённая первая строка и автофильтр. Можно несколько листов.
    public static class XlsxWriter
    {
        private const string MainNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private const string RelNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        public static void Write(string path, string sheetName, IList<string> headers, IList<IList<object>> rows) =>
            Write(path, new List<SheetData> { new SheetData { Name = sheetName, Headers = headers, Rows = rows } });

        public static void Write(string path, IList<SheetData> sheets)
        {
            if (sheets.Count == 0) throw new ArgumentException("Нет листов для записи.");
            if (File.Exists(path)) File.Delete(path);

            var names = UniqueNames(sheets.Select(s => s.Name));

            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                var types = new StringBuilder();
                types.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                types.Append("<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">");
                types.Append("<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>");
                types.Append("<Default Extension=\"xml\" ContentType=\"application/xml\"/>");
                types.Append("<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>");
                types.Append("<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>");
                for (int i = 1; i <= sheets.Count; i++)
                    types.Append("<Override PartName=\"/xl/worksheets/sheet" + i + ".xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
                types.Append("</Types>");
                AddEntry(zip, "[Content_Types].xml", types.ToString());

                AddEntry(zip, "_rels/.rels",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                    "<Relationship Id=\"rId1\" Type=\"" + RelNs + "/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                    "</Relationships>");

                var workbook = new StringBuilder();
                workbook.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                workbook.Append("<workbook xmlns=\"" + MainNs + "\" xmlns:r=\"" + RelNs + "\"><sheets>");
                for (int i = 1; i <= sheets.Count; i++)
                    workbook.Append("<sheet name=\"" + Escape(names[i - 1]) + "\" sheetId=\"" + i + "\" r:id=\"rId" + i + "\"/>");
                workbook.Append("</sheets></workbook>");
                AddEntry(zip, "xl/workbook.xml", workbook.ToString());

                var rels = new StringBuilder();
                rels.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                rels.Append("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
                for (int i = 1; i <= sheets.Count; i++)
                    rels.Append("<Relationship Id=\"rId" + i + "\" Type=\"" + RelNs + "/worksheet\" Target=\"worksheets/sheet" + i + ".xml\"/>");
                rels.Append("<Relationship Id=\"rId" + (sheets.Count + 1) + "\" Type=\"" + RelNs + "/styles\" Target=\"styles.xml\"/>");
                rels.Append("</Relationships>");
                AddEntry(zip, "xl/_rels/workbook.xml.rels", rels.ToString());

                AddEntry(zip, "xl/styles.xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<styleSheet xmlns=\"" + MainNs + "\">" +
                    "<fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font>" +
                    "<font><b/><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts>" +
                    "<fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill>" +
                    "<fill><patternFill patternType=\"gray125\"/></fill></fills>" +
                    "<borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders>" +
                    "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
                    "<cellXfs count=\"2\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/>" +
                    "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/></cellXfs>" +
                    "</styleSheet>");

                for (int i = 0; i < sheets.Count; i++)
                    AddEntry(zip, "xl/worksheets/sheet" + (i + 1) + ".xml", BuildSheet(sheets[i]));
            }
        }

        private static string BuildSheet(SheetData data)
        {
            var headers = data.Headers;
            var rows = data.Rows;
            var sheet = new StringBuilder();
            sheet.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sheet.Append("<worksheet xmlns=\"" + MainNs + "\">");
            sheet.Append("<sheetViews><sheetView workbookViewId=\"0\">" +
                         "<pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/>" +
                         "</sheetView></sheetViews>");
            sheet.Append("<cols><col min=\"1\" max=\"" + headers.Count + "\" width=\"18\" customWidth=\"1\"/></cols>");
            sheet.Append("<sheetData>");

            sheet.Append("<row r=\"1\">");
            for (int c = 0; c < headers.Count; c++)
                sheet.Append(StringCell(c, 1, headers[c], 1));
            sheet.Append("</row>");

            for (int r = 0; r < rows.Count; r++)
            {
                sheet.Append("<row r=\"" + (r + 2) + "\">");
                var row = rows[r];
                for (int c = 0; c < row.Count; c++)
                {
                    var value = row[c];
                    if (value == null) continue;
                    if (value is double || value is int || value is long || value is float)
                        sheet.Append(NumberCell(c, r + 2, Convert.ToDouble(value, CultureInfo.InvariantCulture)));
                    else
                        sheet.Append(StringCell(c, r + 2, value.ToString(), 0));
                }
                sheet.Append("</row>");
            }

            sheet.Append("</sheetData>");
            sheet.Append("<autoFilter ref=\"A1:" + ColumnName(headers.Count - 1) + (rows.Count + 1) + "\"/>");
            sheet.Append("</worksheet>");
            return sheet.ToString();
        }

        // Имя листа: до 31 символа, без \ / ? * [ ] :, неповторяющееся.
        private static List<string> UniqueNames(IEnumerable<string> raw)
        {
            var result = new List<string>();
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in raw)
            {
                var clean = new string((name ?? "").Select(ch => "\\/?*[]:".IndexOf(ch) >= 0 ? '_' : ch).ToArray()).Trim();
                if (clean.Length == 0) clean = "Лист";
                if (clean.Length > 31) clean = clean.Substring(0, 31);

                var candidate = clean;
                var n = 2;
                while (!used.Add(candidate))
                {
                    var suffix = " " + n++;
                    candidate = clean.Substring(0, Math.Min(clean.Length, 31 - suffix.Length)) + suffix;
                }
                result.Add(candidate);
            }
            return result;
        }

        private static string StringCell(int col, int row, string text, int style) =>
            "<c r=\"" + ColumnName(col) + row + "\" t=\"inlineStr\"" + (style > 0 ? " s=\"" + style + "\"" : "") + ">" +
            "<is><t xml:space=\"preserve\">" + Escape(text) + "</t></is></c>";

        private static string NumberCell(int col, int row, double value) =>
            "<c r=\"" + ColumnName(col) + row + "\"><v>" + value.ToString("R", CultureInfo.InvariantCulture) + "</v></c>";

        private static string ColumnName(int index)
        {
            var name = "";
            index++;
            while (index > 0)
            {
                int rem = (index - 1) % 26;
                name = (char)('A' + rem) + name;
                index = (index - 1) / 26;
            }
            return name;
        }

        private static string Escape(string text)
        {
            if (text == null) return "";
            var clean = new StringBuilder(text.Length);
            foreach (var ch in text)
            {
                // В XML 1.0 допустимы не все управляющие символы.
                if (ch == '\t' || ch == '\n' || ch == '\r' || ch >= ' ') clean.Append(ch);
            }
            return SecurityElement.Escape(clean.ToString());
        }

        private static void AddEntry(ZipArchive zip, string name, string content)
        {
            var entry = zip.CreateEntry(name);
            using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false)))
                writer.Write(content);
        }
    }
}
