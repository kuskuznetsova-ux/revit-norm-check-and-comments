using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.UI;
using RevitNormCheck.Corpus;
using RevitNormCheck.Export;
using RevitNormCheck.Rules;
using RevitNormCheck.Settings;

namespace RevitNormCheck.Checks
{
    public static class CheckRunner
    {
        private static readonly List<ICheck> Implemented = new List<ICheck>
        {
            new DoorWidthCheck(),
            new DoorSwingCheck(),
            new StairWidthCheck()
        };

        public static List<Finding> Run(UIDocument uidoc, PluginSettings settings)
        {
            var doc = uidoc.Document;
            var findings = new List<Finding>();
            var selected = CheckDefinition.All.Where(c => settings.EnabledCheckIds.Contains(c.Id)).ToList();

            var rules = selected.Any(c => c.Category == CheckCategory.Norms)
                ? RuleLoader.Load(settings.NormsFolder)
                : new RuleSet();
            if (selected.Any(c => c.Category == CheckCategory.Norms) && rules.Rules.Count > 0)
            {
                var corpus = CorpusStore.Load(settings.NormsFolder);
                rules.CorpusLoaded = corpus != null;
                RuleVerifier.Apply(rules, corpus);

                if (corpus == null)
                {
                    findings.Add(new Finding
                    {
                        CheckId = "rules", CheckTitle = "Сверка правил", Result = Results.NotVerified,
                        Comment = "В папке с нормами нет corpus.json: правила не сверены с текстом. Нажмите «Подготовить нормы»."
                    });
                }
                foreach (var rule in rules.Rules)
                {
                    RuleStatus status;
                    if (!rules.Status.TryGetValue(rule.Id, out status)) continue;
                    if (status != RuleStatus.Changed && status != RuleStatus.Missing) continue;
                    findings.Add(new Finding
                    {
                        CheckId = rule.Check,
                        CheckTitle = rule.Title ?? rule.Id,
                        Result = status == RuleStatus.Changed ? Results.RuleChanged : Results.RuleMissing,
                        Document = rule.Document, Clause = rule.Clause, Quote = rule.Quote,
                        Comment = status == RuleStatus.Changed
                            ? "Пункт найден, но цитаты в нём нет: норма могла измениться. Правило не применяется."
                            : "Документа или пункта нет в папке с нормами. Правило не применяется."
                    });
                }
            }

            var views = ScopeResolver.ResolveViews(uidoc, settings);
            var context = new CheckContext(doc, views, ScopeResolver.BuildViewToSheet(doc), rules);

            foreach (var error in rules.Errors)
                findings.Add(new Finding { CheckId = "rules", CheckTitle = "Файл правил", Result = Results.Error, Comment = error });

            var unapproved = rules.Unapproved;
            if (unapproved.Count > 0)
            {
                findings.Add(new Finding
                {
                    CheckId = "rules",
                    CheckTitle = "Файл правил",
                    Result = Results.Unapproved,
                    Comment = "Не применяются неутверждённые правила (" + unapproved.Count + "): " +
                              string.Join(", ", unapproved.Select(r => r.Id))
                });
            }

            foreach (var definition in selected)
            {
                var check = Implemented.FirstOrDefault(c => c.Id == definition.Id);
                if (check == null)
                {
                    findings.Add(context.General(definition, Results.NotImplemented, "Проверка ещё не реализована."));
                    continue;
                }

                try { check.Run(context, definition, findings); }
                catch (System.Exception ex)
                {
                    findings.Add(context.General(definition, Results.Error, ex.Message));
                }
            }
            return findings;
        }
    }
}
