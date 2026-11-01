using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitNormCheck.UI;

namespace RevitNormCheck.Commands
{
    [Transaction(TransactionMode.ReadOnly)]
    public class OpenCheckWindowCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var window = new CheckWindow(commandData.Application);
            window.ShowDialog();
            return Result.Succeeded;
        }
    }
}
