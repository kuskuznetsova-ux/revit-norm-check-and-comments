using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using RevitNormCheck.Settings;

namespace RevitNormCheck.Export
{
    // Выгрузка данных о дверях без проверки норм: показывает, что есть в модели.
    public static class DoorExporter
    {
        private const double FeetToMm = 304.8;

        public static readonly string[] Headers =
        {
            "ID элемента", "Лист", "Вид", "Этаж", "Марка", "Семейство", "Тип",
            "Ширина, мм", "Высота, мм", "Помещение (From)", "Помещение (To)",
            "HandFlipped", "FacingFlipped", "Facing X", "Facing Y",
            "Огнестойкость", "X, мм", "Y, мм", "Комментарии"
        };

        public static List<IList<object>> Collect(UIDocument uidoc, PluginSettings settings)
        {
            var doc = uidoc.Document;
            var viewToSheet = ScopeResolver.BuildViewToSheet(doc);
            var rows = new List<IList<object>>();
            var seen = new HashSet<int>();

            var views = ScopeResolver.ResolveViews(uidoc, settings);
            if (views == null)
            {
                foreach (var door in Doors(new FilteredElementCollector(doc)))
                    if (seen.Add(door.Id.IntegerValue)) rows.Add(BuildRow(doc, door, "", ""));
                return rows;
            }

            foreach (var view in views)
            {
                string sheet;
                viewToSheet.TryGetValue(view.Id.IntegerValue, out sheet);
                try
                {
                    foreach (var door in Doors(new FilteredElementCollector(doc, view.Id)))
                        if (seen.Add(door.Id.IntegerValue)) rows.Add(BuildRow(doc, door, view.Name, sheet ?? ""));
                }
                catch (Autodesk.Revit.Exceptions.ArgumentException)
                {
                    // Вид, для которого нельзя собрать элементы (например, спецификация): пропускаем.
                }
            }
            return rows;
        }

        private static IEnumerable<FamilyInstance> Doors(FilteredElementCollector collector) =>
            collector.OfCategory(BuiltInCategory.OST_Doors)
                .WhereElementIsNotElementType()
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .ToList();

        private static IList<object> BuildRow(Document doc, FamilyInstance door, string viewName, string sheet)
        {
            var level = doc.GetElement(door.LevelId) as Level;
            var levelName = level != null
                ? level.Name
                : door.get_Parameter(BuiltInParameter.SCHEDULE_LEVEL_PARAM)?.AsValueString() ?? "";

            var width = NumberParam(door, BuiltInParameter.DOOR_WIDTH) ?? NumberParam(door, BuiltInParameter.FAMILY_WIDTH_PARAM);
            var height = NumberParam(door, BuiltInParameter.DOOR_HEIGHT) ?? NumberParam(door, BuiltInParameter.FAMILY_HEIGHT_PARAM);

            var facing = door.FacingOrientation;
            var point = (door.Location as LocationPoint)?.Point;

            return new List<object>
            {
                door.Id.IntegerValue,
                sheet,
                viewName,
                levelName,
                door.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "",
                door.Symbol?.FamilyName ?? "",
                door.Symbol?.Name ?? "",
                width.HasValue ? (object)Math.Round(width.Value * FeetToMm) : "",
                height.HasValue ? (object)Math.Round(height.Value * FeetToMm) : "",
                RoomName(door.FromRoom),
                RoomName(door.ToRoom),
                door.HandFlipped ? "да" : "нет",
                door.FacingFlipped ? "да" : "нет",
                Math.Round(facing.X, 3),
                Math.Round(facing.Y, 3),
                TextByNames(door, "Огнестойкость", "Предел огнестойкости", "Fire Rating"),
                point != null ? (object)Math.Round(point.X * FeetToMm) : "",
                point != null ? (object)Math.Round(point.Y * FeetToMm) : "",
                door.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? ""
            };
        }

        private static string RoomName(Room room)
        {
            if (room == null) return "";
            var name = room.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString() ?? "";
            return (room.Number + " " + name).Trim();
        }

        private static double? NumberParam(FamilyInstance door, BuiltInParameter parameter)
        {
            var p = door.get_Parameter(parameter);
            if (p == null || !p.HasValue) p = door.Symbol?.get_Parameter(parameter);
            return p != null && p.HasValue && p.StorageType == StorageType.Double ? (double?)p.AsDouble() : null;
        }

        private static string TextByNames(FamilyInstance door, params string[] names)
        {
            foreach (var name in names)
            {
                var p = door.LookupParameter(name) ?? door.Symbol?.LookupParameter(name);
                if (p == null || !p.HasValue) continue;
                return p.StorageType == StorageType.String ? p.AsString() : p.AsValueString();
            }
            return "";
        }
    }
}
