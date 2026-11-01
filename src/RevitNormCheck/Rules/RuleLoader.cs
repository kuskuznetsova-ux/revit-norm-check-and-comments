using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;

namespace RevitNormCheck.Rules
{
    public class RuleSet
    {
        public List<Rule> Rules { get; } = new List<Rule>();
        public List<string> Errors { get; } = new List<string>();

        // Статус сверки правил с текстом норм и правила, исключённые из применения.
        public Dictionary<string, RuleStatus> Status { get; } = new Dictionary<string, RuleStatus>();
        public HashSet<string> Excluded { get; } = new HashSet<string>();
        public bool CorpusLoaded { get; set; }

        public static bool IsApproved(Rule rule) => !string.IsNullOrWhiteSpace(rule.ApprovedBy);

        // Только утверждённые правила этой проверки.
        public List<Rule> For(string checkId) =>
            Rules.Where(r => IsApproved(r) && !Excluded.Contains(r.Id) && string.Equals(r.Check, checkId, StringComparison.OrdinalIgnoreCase)).ToList();

        public List<Rule> Unapproved => Rules.Where(r => !IsApproved(r)).ToList();
    }

    public static class RuleLoader
    {
        public const string FileName = "rules.json";

        // Читает rules.json из выбранной папки с нормами (в самой папке, не в подпапках).
        public static RuleSet Load(string folder)
        {
            var set = new RuleSet();
            var path = Path.Combine(folder ?? "", FileName);
            if (!File.Exists(path))
            {
                set.Errors.Add("В папке нет файла " + FileName + ": " + path);
                return set;
            }

            try
            {
                using (var stream = File.OpenRead(path))
                {
                    var file = (RuleFile)new DataContractJsonSerializer(typeof(RuleFile)).ReadObject(stream);
                    if (file?.Rules != null) set.Rules.AddRange(file.Rules);
                }
            }
            catch (Exception ex)
            {
                set.Errors.Add("Не удалось прочитать " + path + ": " + ex.Message);
            }
            return set;
        }

        // Сохраняет правила в читаемом виде (с отступами) в rules.json папки.
        public static void Save(string folder, IList<Rule> rules)
        {
            var file = new RuleFile { Rules = rules.ToList() };
            string json;
            using (var memory = new MemoryStream())
            {
                using (var writer = System.Runtime.Serialization.Json.JsonReaderWriterFactory
                           .CreateJsonWriter(memory, Encoding.UTF8, false, true))
                {
                    new DataContractJsonSerializer(typeof(RuleFile)).WriteObject(writer, file);
                }
                json = Encoding.UTF8.GetString(memory.ToArray());
            }
            // Сериализатор экранирует «/» как «\/», для читаемости возвращаем обычный символ.
            json = json.Replace("\\/", "/");
            File.WriteAllText(Path.Combine(folder, FileName), json, new UTF8Encoding(false));
        }
    }
}
