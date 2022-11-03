using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using Tianhua.Platform3D.UI.Command;
using Tianhua.Platform3D.UI.EventMonitor;
using Tianhua.Platform3D.UI.UI;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Tianhua.Platform3D.UI
{
    public class Platform3DUIApp : IExtensionApplication
    {
        public void Initialize()
        {
            //add code to run when the ExtApp initializes. Here are a few examples:
            //  Checking some host information like build #, a patch or a particular Arx/Dbx/Dll;
            //  Creating/Opening some files to use in the whole life of the assembly, e.g. logs;
            //  Adding some ribbon tabs, panels, and/or buttons, when necessary;
            //  Loading some dependents explicitly which are not taken care of automatically;
            //  Subscribing to some events which are important for the whole session;
            //  Etc.
            PlatformAddEvents();
        }

        public void Terminate()
        {
            //add code to clean up things when the ExtApp terminates. For example:
            //  Closing the log files;
            //  Deleting the custom ribbon tabs/panels/buttons;
            //  Unloading those dependents;
            //  Un-subscribing to those events;
            //  Etc.
            PlatformRemoveEvents();
        }

        [CommandMethod("TIANHUACAD", "THBM", CommandFlags.Modal)]
        public void THBM()
        {
            Platform3DMainService.Instace.ShowUI();
        }

        [CommandMethod("TIANHUACAD", "THSMBTUI", CommandFlags.Modal)]
        public void THSMBTUI()
        {
            using (var cmd = new ThStructurePlaneCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THAMBTUI", CommandFlags.Modal)]
        public void THAUTSC()
        {
            using (var cmd = new ThArchitecturePlaneCmd())
            {
                cmd.Execute();
            }
        }

        #region 平台CAD相关事件
        private void PlatformAddEvents() 
        {
            AcadApp.DocumentManager.DocumentActivated += Platform3DMainEvent.DocumentManager_DocumentActivated;
            AcadApp.DocumentManager.DocumentToBeDestroyed += Platform3DMainEvent.DocumentManager_DocumentToBeDestroyed;
            AcadApp.DocumentManager.DocumentDestroyed += Platform3DMainEvent.DocumentManager_DocumentDestroyed;
            AcadApp.DocumentManager.DocumentToBeActivated += Platform3DMainEvent.DocumentManager_DocumentToBeActivated;
        }
        private void PlatformRemoveEvents()
        {
            AcadApp.DocumentManager.DocumentActivated -= Platform3DMainEvent.DocumentManager_DocumentActivated;
            AcadApp.DocumentManager.DocumentToBeDestroyed -= Platform3DMainEvent.DocumentManager_DocumentToBeDestroyed;
            AcadApp.DocumentManager.DocumentDestroyed -= Platform3DMainEvent.DocumentManager_DocumentDestroyed;
            AcadApp.DocumentManager.DocumentToBeActivated -= Platform3DMainEvent.DocumentManager_DocumentToBeActivated;
        }
        #endregion
    }
}
