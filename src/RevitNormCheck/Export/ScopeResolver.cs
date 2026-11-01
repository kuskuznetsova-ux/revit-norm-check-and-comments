using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitNormCheck.Settings;

namespace RevitNormCheck.Export
{
    public static class ScopeResolver
    {
        // Возвращает виды выбранной области. null означает «вся модель».
        public static List<View> ResolveViews(UIDocument uidoc, PluginSettings settings)
        {
            var doc = uidoc.Document;
            var result = new List<View>();
            var seen = new HashSet<int>();

            void Add(View view)
            {
                if (view == null || view.IsTemplate || view is ViewSchedule) return;
                if (seen.Add(view.Id.IntegerValue)) result.Add(view);
            }

            void AddSheet(ViewSheet sheet)
            {
                foreach (var viewportId in sheet.GetAllViewports())
                {
                    var viewport = doc.GetElement(viewportId) as Viewport;
                    if (viewport != null) Add(doc.GetElement(viewport.ViewId) as View);
                }
            }

            switch (settings.ScopeMode)
            {
                case "WholeModel":
                    return null;

                case "Selected":
                    foreach (var uniqueId in settings.ScopeUniqueIds)
                    {
                        var element = doc.GetElement(uniqueId);
                        var sheet = element as ViewSheet;
                        if (sheet != null) AddSheet(sheet);
                        else Add(element as View);
                    }
                    break;

                default:
                    var active = uidoc.ActiveView;
                    var activeSheet = active as ViewSheet;
                    if (activeSheet != null) AddSheet(activeSheet);
                    else Add(active);
                    break;
            }

            return result;
        }

        public static Dictionary<int, string> BuildViewToSheet(Document doc)
        {
            var map = new Dictionary<int, string>();
            foreach (var viewport in new FilteredElementCollector(doc).OfClass(typeof(Viewport)).Cast<Viewport>())
            {
                var sheet = doc.GetElement(viewport.SheetId) as ViewSheet;
                if (sheet == null) continue;
                var key = viewport.ViewId.IntegerValue;
                if (!map.ContainsKey(key)) map[key] = sheet.SheetNumber + " - " + sheet.Name;
            }
            return map;
        }
    }
}
