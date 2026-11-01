using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security;
using System.Text;

namespace RevitNormCheck.Export
{
    // Минимальная запись .xlsx без внешних библиотек: один лист, строки и числа,
    // жирная шапка, закреплённая первая строка и автофильтр.
    public static class XlsxWriter
    {
        private const string MainNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private const string RelNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        public static void Write(string path, string sheetName, IList<string> headers, IList<IList<object>> rows)
        {
            if (File.Exists(path)) File.Delete(path);

            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                AddEntry(zip, "[Content_Types].xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                    "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                    "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                    "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
                    "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
                    "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>" +
                    "</Types>");

                AddEntry(zip, "_rels/.rels",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                    "<Relationship Id=\"rId1\" Type=\"" + RelNs + "/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                    "</Relationships>");

                AddEntry(zip, "xl/workbook.xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<workbook xmlns=\"" + MainNs + "\" xmlns:r=\"" + RelNs + "\">" +
                    "<sheets><sheet name=\"" + Escape(sheetName) + "\" sheetId=\"1\" r:id=\"rId1\"/></sheets>" +
                    "</workbook>");

                AddEntry(zip, "xl/_rels/workbook.xml.rels",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                    "<Relationship Id=\"rId1\" Type=\"" + RelNs + "/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
                    "<Relationship Id=\"rId2\" Type=\"" + RelNs + "/styles\" Target=\"styles.xml\"/>" +
                    "</Relationships>");

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
                AddEntry(zip, "xl/worksheets/sheet1.xml", sheet.ToString());
            }
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
