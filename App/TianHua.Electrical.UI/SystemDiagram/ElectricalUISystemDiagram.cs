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
        ShowAlarm uiConnect;
        [CommandMethod("TIANHUACAD", "THHZXT", CommandFlags.Modal)]
        public void THCL()
        {
            if (null != ChoiseLayers && ChoiseLayers.IsLoaded)
                return;
            ChoiseLayers = new SelectLayers();
            var isOk = AcadApp.ShowModalWindow(ChoiseLayers);
            if (isOk == true)
            {
                if (ChoiseLayers.commondType == 1)
                {
                    CommandHandlerBase.ExecuteFromCommandLine(false, "THHZXTA");
                }
                else if (ChoiseLayers.commondType == 2)
                {
                    CommandHandlerBase.ExecuteFromCommandLine(false, "THHZXTF");
                }
                else if (ChoiseLayers.commondType == 3)
                {
                    CommandHandlerBase.ExecuteFromCommandLine(false, "THHZXTP");
                }
            }
        }

        [CommandMethod("TIANHUACAD", "ZXJHMX", CommandFlags.Modal)]
        public void THC2L()
        {
            uiConnect = new ShowAlarm();
            AcadApp.ShowModelessWindow(uiConnect);
        }
    }
}
