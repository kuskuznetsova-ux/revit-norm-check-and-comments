using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;

namespace RevitNormCheck.Checks
{
    // A.1.3: ширина лестничных маршей. Площадки пока не проверяются.
    public class StairWidthCheck : ICheck
    {
        public string Id => "stair-width";

        public void Run(CheckContext context, CheckDefinition definition, List<Finding> findings)
        {
            var rules = context.Rules.For(Id);
            if (rules.Count == 0)
            {
                findings.Add(context.General(definition, Results.NoRules, "В rules.json нет правил для этой проверки."));
                return;
            }

            foreach (var item in context.Elements(BuiltInCategory.OST_Stairs, typeof(Stairs)))
            {
                var stairs = (Stairs)item.Element;
                var typeName = context.Doc.GetElement(stairs.GetTypeId())?.Name ?? "";
                var levelName = context.LevelOf(stairs)?.Name ?? "";

                foreach (var runId in stairs.GetStairsRuns())
                {
                    var run = context.Doc.GetElement(runId) as StairsRun;
                    if (run == null) continue;
                    var widthMm = Math.Round(run.ActualRunWidth * CheckContext.FeetToMm);

                    foreach (var rule in rules)
                    {
                        if (rule.MinWidthMm == null) continue;
                        if (!CheckContext.AppliesTo(rule.When, "", typeName, levelName)) continue;
                        if (widthMm + 0.5 >= rule.MinWidthMm.Value) continue;

                        var f = context.NewFinding(definition, item, Results.Violation);
                        f.Actual = widthMm.ToString("0") + " мм";
                        f.Required = "не менее " + rule.MinWidthMm.Value.ToString("0") + " мм";
                        f.Comment = "Марш id " + run.Id.IntegerValue + ", ширина марша без учёта ограждений.";
                        CheckContext.ApplyRule(f, rule);
                        findings.Add(f);
                    }
                }
            }
        }
    }
}
