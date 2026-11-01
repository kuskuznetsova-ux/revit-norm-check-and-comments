using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace RevitNormCheck.Corpus
{
    // Разбирает PDF с нормами на пункты. Не интерпретирует текст: только режет по нумерации.
    public static class CorpusBuilder
    {
        // Начало пункта: «4.2.18 Текст». Только номера с точкой, чтобы не принимать за пункты заголовки разделов.
        private static readonly Regex ClauseStart = new Regex(@"^(\d{1,2}(?:\.\d+)+)\.?\s+(?=\S)", RegexOptions.Compiled);

        public static CorpusFile Build(string folder, IProgress<string> progress)
        {
            var previous = CorpusStore.Load(folder);
            var corpus = new CorpusFile { Built = DateTime.Now.ToString("yyyy-MM-dd HH:mm") };
            var files = Directory.EnumerateFiles(folder, "*.pdf", SearchOption.AllDirectories).ToList();

            for (int i = 0; i < files.Count; i++)
            {
                var path = files[i];
                var info = new FileInfo(path);
                var relative = path.Substring(folder.Length).TrimStart('\\', '/');

                // Файл не менялся с прошлой сборки: берём готовый разбор.
                var cached = previous?.Documents.FirstOrDefault(d =>
                    d.Path == relative && d.SizeBytes == info.Length && d.ModifiedUtcTicks == info.LastWriteTimeUtc.Ticks);
                if (cached != null)
                {
                    corpus.Documents.Add(cached);
                    continue;
                }

                progress?.Report($"{i + 1}/{files.Count}: {info.Name}");
                var document = new CorpusDocument
                {
                    Name = Path.GetFileNameWithoutExtension(path),
                    Path = relative,
                    SizeBytes = info.Length,
                    ModifiedUtcTicks = info.LastWriteTimeUtc.Ticks
                };

                try
                {
                    document.Clauses = Parse(path);
                    var textLength = document.Clauses.Sum(c => c.Text.Length);
                    if (document.Clauses.Count == 0 || textLength < 500)
                        document.Error = "Текст не найден или нет нумерованных пунктов (возможно, скан).";
                }
                catch (Exception ex)
                {
                    document.Error = ex.Message;
                }
                corpus.Documents.Add(document);
            }
            return corpus;
        }

        private static List<CorpusClause> Parse(string path)
        {
            var clauses = new List<CorpusClause>();
            CorpusClause current = null;
            var text = new StringBuilder();

            Action flush = () =>
            {
                if (current != null)
                {
                    current.Text = text.ToString().Trim();
                    clauses.Add(current);
                }
                current = null;
                text.Clear();
            };

            using (var pdf = PdfDocument.Open(path))
            {
                foreach (var page in pdf.GetPages())
                {
                    foreach (var line in Lines(page))
                    {
                        var match = ClauseStart.Match(line);
                        if (match.Success)
                        {
                            flush();
                            current = new CorpusClause { Number = match.Groups[1].Value, Page = page.Number };
                            text.Append(line.Substring(match.Length));
                        }
                        else if (current != null)
                        {
                            text.Append(' ').Append(line);
                        }
                    }
                }
            }
            flush();
            return clauses;
        }

        // Строки страницы сверху вниз; слова строки слева направо.
        private static IEnumerable<string> Lines(Page page)
        {
            var words = page.GetWords()
                .OrderByDescending(w => w.BoundingBox.Bottom)
                .ThenBy(w => w.BoundingBox.Left)
                .ToList();

            var line = new List<Word>();
            double lineY = 0;
            foreach (var word in words)
            {
                if (line.Count == 0 || Math.Abs(word.BoundingBox.Bottom - lineY) <= 3)
                {
                    if (line.Count == 0) lineY = word.BoundingBox.Bottom;
                    line.Add(word);
                    continue;
                }
                yield return Join(line);
                line = new List<Word> { word };
                lineY = word.BoundingBox.Bottom;
            }
            if (line.Count > 0) yield return Join(line);
        }

        private static string Join(List<Word> line) =>
            string.Join(" ", line.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text));
    }
}
