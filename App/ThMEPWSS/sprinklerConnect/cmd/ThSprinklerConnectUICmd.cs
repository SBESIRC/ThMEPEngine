using System;

using AcHelper;
using Autodesk.AutoCAD.ApplicationServices;
using DotNetARX;
using Linq2Acad;

using ThMEPEngineCore.Command;
using ThMEPWSS.SprinklerConnect.Service;
using ThMEPWSS.ViewModel;

namespace ThMEPWSS.SprinklerConnect.Cmd
{
    public class ThSprinklerConnectUICmd : ThMEPBaseCommand, IDisposable
    {
        public static ThSprinklerConnectVM SprinklerConnectVM { get; set; }
        public bool DrawPipe { get; set; } = true;
        public ThSprinklerConnectUICmd()
        {
            ActionName = "绘制主管";
            CommandName = "THPTLGBZ";
        }
        public void Dispose()
        {
            //
        }

        public override void SubExecute()
        {
            using (var docLock = Active.Document.LockDocument())
            {
                if (DrawPipe)
                {
                    CreateAIMainPipeLayer();
                }
                else
                {
                    CreateAISubMainPipeLayer();
                }
            }
        }

        private void CommandWillStartHandler(object sender, CommandEventArgs e)
        {
            //
        }

        private void CreateAIMainPipeLayer()
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.CreateAIMainPipeLayer();
                acadDatabase.Database.SetCurrentLayer(ThWSSCommon.Sprinkler_Connect_MainPipe);
                Active.Editor.WriteMessage($"请绘制不接支管的主管");
                Active.Document.SendStringToExecute("_Pline ", true, false, true);
            }
        }

        private void CreateAISubMainPipeLayer()
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.CreateAISubMainPipeLayer();
                acadDatabase.Database.SetCurrentLayer(ThWSSCommon.Sprinkler_Connect_SubMainPipe);
                Active.Editor.WriteMessage($"请绘制连接支管的主管");
                Active.Document.SendStringToExecute("_Pline ", true, false, true);
            }
        }
    }
}
