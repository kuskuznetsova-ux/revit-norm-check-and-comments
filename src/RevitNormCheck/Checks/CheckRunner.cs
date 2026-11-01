using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.UI;
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
            var views = ScopeResolver.ResolveViews(uidoc, settings);
            var context = new CheckContext(doc, views, ScopeResolver.BuildViewToSheet(doc), rules);

            foreach (var error in rules.Errors)
                findings.Add(new Finding { CheckId = "rules", CheckTitle = "Файл правил", Result = Results.Error, Comment = error });

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
