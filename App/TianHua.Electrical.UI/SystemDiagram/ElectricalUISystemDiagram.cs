using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.Runtime;
using TianHua.Electrical.UI.SystemDiagram.UI;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Electrical.UI.SystemDiagram
{
    public class ElectricalUISystemDiagram
    {
        SelectLayers ChoiseLayers;
        [CommandMethod("TIANHUACAD", "THAFAS", CommandFlags.Modal)]
        public void THCL()
        {
            if (null != ChoiseLayers && ChoiseLayers.IsLoaded)
                return;
            ChoiseLayers = new SelectLayers();
            //AcadApp.ShowModelessWindow(ChoiseLayers);
            var isOk = AcadApp.ShowModalWindow(ChoiseLayers);
            if (isOk == true)
            {
                if (ChoiseLayers.commondType == 1)
                {
                    CommandHandlerBase.ExecuteFromCommandLine(false, "THAFASA");
                }
                else if (ChoiseLayers.commondType == 2)
                {
                    CommandHandlerBase.ExecuteFromCommandLine(false, "THAFASF");
                }
            }
        }
    }
}
