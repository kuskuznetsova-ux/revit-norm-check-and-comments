using System.Reflection;
using Autodesk.Revit.UI;

namespace RevitNormCheck
{
    public class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            var panel = application.CreateRibbonPanel("Проверка норм");
            var data = new PushButtonData(
                "RevitNormCheck_Open",
                "Проверка\nнорм",
                Assembly.GetExecutingAssembly().Location,
                typeof(Commands.OpenCheckWindowCommand).FullName);
            panel.AddItem(data);
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application) => Result.Succeeded;
    }
}
