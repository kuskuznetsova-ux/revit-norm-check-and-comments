using System.Collections.Generic;
using System.Linq;
using RevitNormCheck.Checks;
using RevitNormCheck.Export;

namespace RevitNormCheck.Reporting
{
    public static class ReportWriter
    {
        private static readonly string[] Headers =
        {
            "№", "Проверка", "Результат", "Лист", "Вид", "Этаж", "Элемент", "ID элемента",
            "Факт", "Требуется", "Документ", "Пункт", "Цитата", "Комментарий", "X, мм", "Y, мм"
        };

        public static void Write(string path, IList<Finding> findings)
        {
            var rows = new List<IList<object>>();
            var number = 1;
            foreach (var f in findings)
            {
                rows.Add(new List<object>
                {
                    number++, f.CheckTitle, f.Result, f.Sheet, f.View, f.Level, f.Element,
                    f.ElementId.HasValue ? (object)f.ElementId.Value : "",
                    f.Actual, f.Required, f.Document, f.Clause, f.Quote, f.Comment,
                    f.XMm.HasValue ? (object)f.XMm.Value : "",
                    f.YMm.HasValue ? (object)f.YMm.Value : ""
                });
            }
            XlsxWriter.Write(path, "Замечания", Headers, rows);
        }

        public static string Summary(IList<Finding> findings)
        {
            var groups = findings.GroupBy(f => f.Result).Select(g => g.Key + ": " + g.Count());
            return string.Join("\n", groups);
        }
    }
}
