using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RevitNormCheck.Checks
{
    // A.1.4: открывание дверей по направлению эвакуации.
    // Направление эвакуации задаёт правило: из помещений fromRoomNameContains в toRoomNameContains.
    // Сторона открывания определяется по символической дуге двери на плане.
    public class DoorSwingCheck : ICheck
    {
        public string Id => "door-swing";

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
                var frontRoom = DoorGeometry.RoomAt(context, door, true);
                var backRoom = DoorGeometry.RoomAt(context, door, false);
                var front = CheckContext.RoomName(frontRoom);
                var back = CheckContext.RoomName(backRoom);
                if (frontRoom == null && backRoom == null) continue;

                var typeName = door.Symbol?.FamilyName + " " + door.Symbol?.Name;
                var level = context.LevelOf(door);
                var levelName = level?.Name ?? "";

                // Какое направление эвакуации у двери по правилам: вперёд (в сторону front) или назад.
                foreach (var rule in rules)
                {
                    if (!CheckContext.AppliesTo(rule.When, front + " | " + back, typeName, levelName)) continue;

                    bool? evacuatesForward = null;
                    if (CheckContext.MatchesAny(back, rule.FromRoomNameContains) && !string.IsNullOrEmpty(back) &&
                        CheckContext.MatchesAny(front, rule.ToRoomNameContains) && !string.IsNullOrEmpty(front))
                        evacuatesForward = true;
                    else if (CheckContext.MatchesAny(front, rule.FromRoomNameContains) && !string.IsNullOrEmpty(front) &&
                             CheckContext.MatchesAny(back, rule.ToRoomNameContains) && !string.IsNullOrEmpty(back))
                        evacuatesForward = false;

                    if (evacuatesForward == null) continue;

                    var view = item.View ?? context.PlanFor(level);
                    var swing = DoorGeometry.SwingSide(door, view);
                    if (swing == null)
                    {
                        var missing = context.NewFinding(definition, item, Results.NoData);
                        missing.Comment = "На плане у двери нет символической дуги открывания. Помещения: " + back + " -> " + front;
                        CheckContext.ApplyRule(missing, rule);
                        findings.Add(missing);
                        continue;
                    }

                    var swingsForward = swing.Value > 0;
                    if (swingsForward == evacuatesForward.Value) continue;

                    var f = context.NewFinding(definition, item, Results.Violation);
                    f.Actual = "открывается против направления эвакуации";
                    f.Required = "по направлению эвакуации";
                    var from = evacuatesForward.Value ? back : front;
                    var to = evacuatesForward.Value ? front : back;
                    f.Comment = "Эвакуация: " + from + " -> " + to;
                    CheckContext.ApplyRule(f, rule);
                    findings.Add(f);
                }
            }
        }
    }
}
