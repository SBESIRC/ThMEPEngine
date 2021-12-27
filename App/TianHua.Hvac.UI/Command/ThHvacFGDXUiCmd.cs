using System;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using TianHua.Hvac.UI.UI;
using ThMEPHVAC.Service;
using ThMEPEngineCore.Command;

namespace TianHua.Hvac.UI.Command
{
    public class ThHvacFGDXUiCmd : ThMEPBaseCommand, IDisposable
    {
        public ThHvacFGDXUiCmd()
        {
            ActionName = "插风管断线";
            CommandName = "THFGDX";
        }
        public void Dispose()
        {
            //
        }
        public override void SubExecute()
        {
            var roomSelector = new ThRoomSelector();
            roomSelector.Select();
            if (roomSelector.Rooms.Count==0)
            {
                return;
            }
            else
            {
                if (uiFGDXParameter.Instance != null)
                {
                    uiFGDXParameter.Instance.SetRooms(roomSelector.Rooms);
                }
                ShowUI();
            }
        }

       

        private void ShowUI()
        {
            if (null != uiFGDXParameter.Instance && uiFGDXParameter.Instance.IsLoaded)
            {
                if (!uiFGDXParameter.Instance.IsVisible)
                {
                    uiFGDXParameter.Instance.Show();
                }
                return;
            }
            uiFGDXParameter.Instance.WindowStartupLocation =
                System.Windows.WindowStartupLocation.CenterScreen;
            AcadApp.ShowModelessWindow(uiFGDXParameter.Instance);
        }
    }
}
