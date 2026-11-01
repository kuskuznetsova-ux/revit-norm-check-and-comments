using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace RevitNormCheck.Corpus
{
    public static class CorpusStore
    {
        public const string FileName = "corpus.json";

        // null, если файла нет или он не читается.
        public static CorpusFile Load(string folder)
        {
            try
            {
                var path = Path.Combine(folder ?? "", FileName);
                if (!File.Exists(path)) return null;
                using (var stream = File.OpenRead(path))
                    return (CorpusFile)new DataContractJsonSerializer(typeof(CorpusFile)).ReadObject(stream);
            }
            catch
            {
                return null;
            }
        }

        public static void Save(string folder, CorpusFile corpus)
        {
            using (var stream = File.Create(Path.Combine(folder, FileName)))
                new DataContractJsonSerializer(typeof(CorpusFile)).WriteObject(stream, corpus);
        }

        // Текст для сравнения: без регистра, переносов и лишних пробелов.
        public static string Normalize(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var sb = new StringBuilder(text.Length);
            var space = false;
            foreach (var ch in text)
            {
                if (char.IsWhiteSpace(ch) || ch == ' ') { space = sb.Length > 0; continue; }
                if (space) { sb.Append(' '); space = false; }
                sb.Append(char.ToLowerInvariant(ch));
            }
            return sb.ToString();
        }

        // Ключ названия документа: только буквы и цифры, для сопоставления «СП 1.13130.2020» с именем файла.
        public static string Key(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var sb = new StringBuilder();
            foreach (var ch in text)
                if (char.IsLetterOrDigit(ch)) sb.Append(char.ToLowerInvariant(ch));
            return sb.ToString();
        }
    }
}
