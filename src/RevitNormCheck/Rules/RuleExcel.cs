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

        public static void Export(string path, IList<Rule> rules)
        {
            var rows = rules.Select(r => (IList<object>)new List<object>
            {
                r.Id, r.Check, r.Subgroup, r.Title, r.Document, r.Edition, r.Clause, r.Quote,
                r.MinWidthMm.HasValue ? (object)r.MinWidthMm.Value : "",
                Join(r.When?.RoomNameContains), Join(r.When?.TypeNameContains), Join(r.When?.LevelContains),
                Join(r.FromRoomNameContains), Join(r.ToRoomNameContains), r.ApprovedBy, r.ApprovedOn
            }).ToList();
            XlsxWriter.Write(path, "Правила", Headers, rows);
        }

        public static List<Rule> Import(string path)
        {
            var table = XlsxReader.ReadFirstSheet(path);
            if (table.Count == 0) throw new InvalidOperationException("Файл пуст.");

            var header = table[0];
            Func<string, int> col = name => Array.FindIndex(header, h => string.Equals(h?.Trim(), name, StringComparison.OrdinalIgnoreCase));
            var required = new[] { "id", "Проверка", "Документ", "Пункт" };
            var missing = required.Where(n => col(n) < 0).ToList();
            if (missing.Count > 0)
                throw new InvalidOperationException("В шапке нет колонок: " + string.Join(", ", missing));

            Func<string[], string, string> get = (row, name) =>
            {
                var i = col(name);
                return i >= 0 && i < row.Length ? (row[i] ?? "").Trim() : "";
            };

            var rules = new List<Rule>();
            foreach (var row in table.Skip(1))
            {
                var id = get(row, "id");
                if (id.Length == 0) continue;

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
            return rules;
        }

        private static string Join(IList<string> items) => items == null ? "" : string.Join("; ", items);

        private static List<string> Split(string text) =>
            text.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim()).Where(t => t.Length > 0).ToList();
    }
}
