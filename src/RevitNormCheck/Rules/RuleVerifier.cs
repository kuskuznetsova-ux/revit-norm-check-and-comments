using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RevitNormCheck.Corpus;

namespace RevitNormCheck.Rules
{
    public enum RuleStatus
    {
        NotChecked,   // нет корпуса, сверить нельзя
        Current,      // пункт найден, цитата совпала
        Changed,      // пункт найден, но цитаты в нём нет
        Missing       // документа или пункта нет в папке с нормами
    }

    public static class RuleVerifier
    {
        private static readonly Regex ClauseNumber = new Regex(@"\d+(?:\.\d+)+", RegexOptions.Compiled);

        public static RuleStatus Check(Rule rule, CorpusFile corpus)
        {
            if (corpus == null) return RuleStatus.NotChecked;

            var documentKey = CorpusStore.Key(rule.Document);
            if (documentKey.Length == 0) return RuleStatus.Missing;
            var documents = corpus.Documents.Where(d => CorpusStore.Key(d.Name).Contains(documentKey)).ToList();
            if (documents.Count == 0) return RuleStatus.Missing;

            var match = ClauseNumber.Match(rule.Clause ?? "");
            if (!match.Success) return RuleStatus.Missing;
            var clause = documents.SelectMany(d => d.Clauses).FirstOrDefault(c => c.Number == match.Value);
            if (clause == null) return RuleStatus.Missing;

            var quote = CorpusStore.Normalize((rule.Quote ?? "").Trim().Trim('…', '.', ' '));
            if (quote.Length == 0) return RuleStatus.Changed;
            return CorpusStore.Normalize(clause.Text).Contains(quote) ? RuleStatus.Current : RuleStatus.Changed;
        }

        // Сверяет утверждённые правила с корпусом: изменившиеся и пропавшие исключаются из применения.
        public static void Apply(RuleSet set, CorpusFile corpus)
        {
            foreach (var rule in set.Rules)
            {
                var status = Check(rule, corpus);
                set.Status[rule.Id] = status;
                if (status == RuleStatus.Changed || status == RuleStatus.Missing)
                    set.Excluded.Add(rule.Id);
            }
        }
    }
}
