using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RevitNormCheck.Checks
{
    // A.1.1: ширина дверей. Минимум берётся из rules.json (по правилам, подходящим к двери).
    public class DoorWidthCheck : ICheck
    {
        public string Id => "door-width";

        public void Run(CheckContext context, CheckDefinition definition, List<Finding> findings)
        {
            var rules = context.Rules.For(Id);
            if (rules.Count == 0)
            {
                findings.Add(context.General(definition, Results.NoRules, "В rules.json нет правил для этой проверки."));
                return;
            }

            foreach (var item in context.Elements(BuiltInCategory.OST_Doors, typeof(FamilyInstance)))
            {
                var door = (FamilyInstance)item.Element;
                var width = DoorWidthMm(door);
                var typeName = door.Symbol?.FamilyName + " " + door.Symbol?.Name;
                var level = context.LevelOf(door)?.Name ?? "";
                var rooms = CheckContext.RoomName(DoorGeometry.RoomAt(context, door, true)) + " | " +
                            CheckContext.RoomName(DoorGeometry.RoomAt(context, door, false));

                foreach (var rule in rules)
                {
                    if (rule.MinWidthMm == null) continue;
                    if (!CheckContext.AppliesTo(rule.When, rooms, typeName, level)) continue;

                    if (width == null)
                    {
                        var missing = context.NewFinding(definition, item, Results.NoData);
                        missing.Comment = "Не найден параметр ширины двери. Помещения: " + rooms;
                        CheckContext.ApplyRule(missing, rule);
                        findings.Add(missing);
                        continue;
                    }

                    if (width.Value + 0.5 >= rule.MinWidthMm.Value) continue;

                    var f = context.NewFinding(definition, item, Results.Violation);
                    f.Actual = width.Value.ToString("0") + " мм";
                    f.Required = "не менее " + rule.MinWidthMm.Value.ToString("0") + " мм";
                    f.Comment = "Помещения: " + rooms + ". Ширина взята из параметра двери, не в свету.";
                    CheckContext.ApplyRule(f, rule);
                    findings.Add(f);
                }
            }
        }

        private static double? DoorWidthMm(FamilyInstance door)
        {
            var feet = Number(door, BuiltInParameter.DOOR_WIDTH) ?? Number(door, BuiltInParameter.FAMILY_WIDTH_PARAM);
            return feet.HasValue ? (double?)Math.Round(feet.Value * CheckContext.FeetToMm) : null;
        }

        private static double? Number(FamilyInstance door, BuiltInParameter parameter)
        {
            var p = door.get_Parameter(parameter);
            if (p == null || !p.HasValue) p = door.Symbol?.get_Parameter(parameter);
            return p != null && p.HasValue && p.StorageType == StorageType.Double ? (double?)p.AsDouble() : null;
        }
    }
}
