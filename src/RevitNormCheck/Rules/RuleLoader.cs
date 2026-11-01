using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;

namespace RevitNormCheck.Rules
{
    public class RuleSet
    {
        public List<Rule> Rules { get; } = new List<Rule>();
        public List<string> Errors { get; } = new List<string>();

        public List<Rule> For(string checkId) =>
            Rules.Where(r => string.Equals(r.Check, checkId, StringComparison.OrdinalIgnoreCase)).ToList();
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
    }
}
