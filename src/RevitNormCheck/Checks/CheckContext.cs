using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using RevitNormCheck.Rules;

namespace RevitNormCheck.Checks
{
    public class ElementInView
    {
        public Element Element { get; set; }
        public View View { get; set; }
        public string ViewName { get; set; }
        public string Sheet { get; set; }
    }

    public class CheckContext
    {
        public const double FeetToMm = 304.8;

        private readonly Dictionary<int, string> _viewToSheet;
        private Dictionary<int, View> _planByLevel;

        public Document Doc { get; }
        // null означает «вся модель».
        public List<View> Views { get; }
        public RuleSet Rules { get; }

        public CheckContext(Document doc, List<View> views, Dictionary<int, string> viewToSheet, RuleSet rules)
        {
            Doc = doc;
            Views = views;
            _viewToSheet = viewToSheet;
            Rules = rules;
        }

        // Элементы области проверки. На видах каждый элемент попадает один раз, с первым видом, где он найден.
        public IEnumerable<ElementInView> Elements(BuiltInCategory category, Type type)
        {
            var seen = new HashSet<int>();

            if (Views == null)
            {
                foreach (var e in Collect(new FilteredElementCollector(Doc), category, type))
                    if (seen.Add(e.Id.IntegerValue))
                        yield return new ElementInView { Element = e, ViewName = "", Sheet = "" };
                yield break;
            }

            foreach (var view in Views)
            {
                IList<Element> found;
                try { found = Collect(new FilteredElementCollector(Doc, view.Id), category, type); }
                catch (Autodesk.Revit.Exceptions.ArgumentException) { continue; }

                string sheet;
                _viewToSheet.TryGetValue(view.Id.IntegerValue, out sheet);
                foreach (var e in found)
                    if (seen.Add(e.Id.IntegerValue))
                        yield return new ElementInView { Element = e, View = view, ViewName = view.Name, Sheet = sheet ?? "" };
            }
        }

        private static IList<Element> Collect(FilteredElementCollector collector, BuiltInCategory category, Type type) =>
            collector.OfCategory(category).WhereElementIsNotElementType().OfClass(type).ToElements();

        // План этажа для уровня: нужен, чтобы увидеть символические линии открывания дверей.
        public View PlanFor(Level level)
        {
            if (level == null) return null;
            if (_planByLevel == null)
            {
                _planByLevel = new Dictionary<int, View>();
                foreach (var plan in new FilteredElementCollector(Doc).OfClass(typeof(ViewPlan)).Cast<ViewPlan>())
                {
                    if (plan.IsTemplate || plan.ViewType != ViewType.FloorPlan || plan.GenLevel == null) continue;
                    var key = plan.GenLevel.Id.IntegerValue;
                    if (!_planByLevel.ContainsKey(key)) _planByLevel[key] = plan;
                }
            }
            View result;
            return _planByLevel.TryGetValue(level.Id.IntegerValue, out result) ? result : null;
        }

        public Level LevelOf(Element element) => Doc.GetElement(element.LevelId) as Level;

        public Finding NewFinding(CheckDefinition check, ElementInView item, string result)
        {
            var level = LevelOf(item.Element);
            var point = (item.Element.Location as LocationPoint)?.Point;
            return new Finding
            {
                CheckId = check.Id,
                CheckTitle = check.Title,
                Result = result,
                Sheet = item.Sheet,
                View = item.ViewName,
                Level = level?.Name ?? "",
                Element = Describe(item.Element),
                ElementId = item.Element.Id.IntegerValue,
                XMm = point != null ? (double?)Math.Round(point.X * FeetToMm) : null,
                YMm = point != null ? (double?)Math.Round(point.Y * FeetToMm) : null
            };
        }

        public Finding General(CheckDefinition check, string result, string comment) =>
            new Finding { CheckId = check.Id, CheckTitle = check.Title, Result = result, Comment = comment };

        public static void ApplyRule(Finding finding, Rule rule)
        {
            finding.Document = rule.Document ?? "";
            finding.Clause = rule.Clause ?? "";
            finding.Quote = rule.Quote ?? "";
        }

        private static string Describe(Element element)
        {
            var instance = element as FamilyInstance;
            if (instance != null)
                return (instance.Symbol?.FamilyName + " : " + instance.Symbol?.Name).Trim(' ', ':');
            return element.Name;
        }

        public static string RoomName(Room room)
        {
            if (room == null) return "";
            var name = room.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString() ?? "";
            return (room.Number + " " + name).Trim();
        }

        // Совпадение хотя бы с одной подстрокой; пустой список подходит любому тексту.
        public static bool MatchesAny(string text, IList<string> needles)
        {
            if (needles == null || needles.Count == 0) return true;
            if (string.IsNullOrEmpty(text)) return false;
            return needles.Any(n => !string.IsNullOrEmpty(n) &&
                                    text.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public static bool AppliesTo(RuleWhen when, string roomNames, string typeName, string levelName)
        {
            if (when == null) return true;
            return MatchesAny(roomNames, when.RoomNameContains)
                   && MatchesAny(typeName, when.TypeNameContains)
                   && MatchesAny(levelName, when.LevelContains);
        }
    }
}
