using System;
using AcHelper.Commands;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.FanSelection.UI.CAD
{
    public class ThFanSelectionAppIdleHandler
    {
        public object Message { get; set; }
        public object MessageArgs { get; set; }

        public ThFanSelectionAppIdleHandler()
        {
            AcadApp.Idle += OnAppIdleHandler;
        }

        public void OnAppIdleHandler(object sender, EventArgs e)
        {
            AcadApp.Idle -= OnAppIdleHandler;
            ThFanSelectionService.Instance.Message = Message;
            ThFanSelectionService.Instance.MessageArgs = MessageArgs;
            CommandHandlerBase.ExecuteFromCommandLine(false, "THFJUIUPDATE");
        }
    }
}
