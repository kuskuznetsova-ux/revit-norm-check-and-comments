using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RevitNormCheck.Export;

namespace RevitNormCheck.Rules
{
    // Правила <-> Excel: таблицу удобно просматривать, править и утверждать (колонки «Утвердил», «Дата»).
    public static class RuleExcel
    {
        private static readonly string[] Headers =
        {
            "id", "Проверка", "Подгруппа", "Название", "Документ", "Редакция", "Пункт", "Цитата",
            "Мин. ширина, мм", "Помещения содержат", "Тип содержит", "Уровень содержит",
            "Эвакуация из помещений", "Эвакуация в помещения", "Утвердил", "Дата"
        };

        // Одна вкладка на документ: так проще проверять правила вручную.
        public static void Export(string path, IList<Rule> rules)
        {
            var sheets = rules
                .GroupBy(r => string.IsNullOrWhiteSpace(r.Document) ? "Без документа" : r.Document.Trim())
                .OrderBy(g => g.Key)
                .Select(g => new SheetData
                {
                    Name = g.Key,
                    Headers = Headers,
                    Rows = g.Select(r => (IList<object>)new List<object>
                    {
                        r.Id, r.Check, r.Subgroup, r.Title, r.Document, r.Edition, r.Clause, r.Quote,
                        r.MinWidthMm.HasValue ? (object)r.MinWidthMm.Value : "",
                        Join(r.When?.RoomNameContains), Join(r.When?.TypeNameContains), Join(r.When?.LevelContains),
                        Join(r.FromRoomNameContains), Join(r.ToRoomNameContains), r.ApprovedBy, r.ApprovedOn
                    }).ToList()
                })
                .ToList();
            XlsxWriter.Write(path, sheets);
        }

        // Читает все вкладки, в которых есть шапка правил; остальные вкладки пропускаются.
        public static List<Rule> Import(string path)
        {
            var rules = new List<Rule>();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var foundSheet = false;

            foreach (var sheet in XlsxReader.ReadSheets(path))
            {
                if (sheet.Value.Count == 0) continue;
                var header = sheet.Value[0];
                Func<string, int> col = name => Array.FindIndex(header, h => string.Equals(h?.Trim(), name, StringComparison.OrdinalIgnoreCase));

                var required = new[] { "id", "Проверка", "Документ", "Пункт" };
                var missing = required.Where(n => col(n) < 0).ToList();
                if (missing.Count == required.Length) continue;   // не вкладка правил
                if (missing.Count > 0)
                    throw new InvalidOperationException("На вкладке «" + sheet.Key + "» нет колонок: " + string.Join(", ", missing));
                foundSheet = true;

                Func<string[], string, string> get = (row, name) =>
                {
                    var i = col(name);
                    return i >= 0 && i < row.Length ? (row[i] ?? "").Trim() : "";
                };

                foreach (var row in sheet.Value.Skip(1))
                {
                    var id = get(row, "id");
                    if (id.Length == 0) continue;
                    if (!ids.Add(id))
                        throw new InvalidOperationException("Повторяется id правила «" + id + "» (вкладка «" + sheet.Key + "»).");

                    var rule = new Rule
                    {
                        Id = id,
                        Check = get(row, "Проверка"),
                        Subgroup = get(row, "Подгруппа"),
                        Title = get(row, "Название"),
                        Document = get(row, "Документ"),
                        Edition = get(row, "Редакция"),
                        Clause = get(row, "Пункт"),
                        Quote = get(row, "Цитата"),
                        FromRoomNameContains = Split(get(row, "Эвакуация из помещений")),
                        ToRoomNameContains = Split(get(row, "Эвакуация в помещения")),
                        ApprovedBy = get(row, "Утвердил"),
                        ApprovedOn = get(row, "Дата")
                    };

                    var width = get(row, "Мин. ширина, мм").Replace(',', '.');
                    double parsed;
                    if (width.Length > 0 && double.TryParse(width, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
                        rule.MinWidthMm = parsed;

                    var when = new RuleWhen
                    {
                        RoomNameContains = Split(get(row, "Помещения содержат")),
                        TypeNameContains = Split(get(row, "Тип содержит")),
                        LevelContains = Split(get(row, "Уровень содержит"))
                    };
                    if (when.RoomNameContains.Count + when.TypeNameContains.Count + when.LevelContains.Count > 0)
                        rule.When = when;

                    rules.Add(rule);
                }
            }

            if (!foundSheet) throw new InvalidOperationException("В файле нет вкладок с правилами (шапка: id, Проверка, Документ, Пункт...).");
            return rules;
        }

        private static string Join(IList<string> items) => items == null ? "" : string.Join("; ", items);

        private static List<string> Split(string text) =>
            text.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim()).Where(t => t.Length > 0).ToList();
    }
}
