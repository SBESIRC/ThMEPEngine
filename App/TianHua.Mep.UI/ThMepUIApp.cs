﻿using Autodesk.AutoCAD.Runtime;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using TianHua.Mep.UI.UI;
using TianHua.Mep.UI.FrameCompare;
using TianHua.Mep.UI.Command;

namespace TianHua.Mep.UI
{
    public class ThMepUIApp : IExtensionApplication
    {
        private RoomOutlineUI uiRoomOutline;
        private ThExtractRoomConfigCmd _extractRoomConfigCmd = new ThExtractRoomConfigCmd();

        public void Initialize()
        {
            //add code to run when the ExtApp initializes. Here are a few examples:
            //  Checking some host information like build #, a patch or a particular Arx/Dbx/Dll;
            //  Creating/Opening some files to use in the whole life of the assembly, e.g. logs;
            //  Adding some ribbon tabs, panels, and/or buttons, when necessary;
            //  Loading some dependents explicitly which are not taken care of automatically;
            //  Subscribing to some events which are important for the whole session;
            //  Etc.

            AcadApp.DocumentManager.DocumentActivated += _extractRoomConfigCmd.DocumentActivated;
            AcadApp.DocumentManager.DocumentToBeDestroyed += _extractRoomConfigCmd.DocumentToBeDestroyed;
            AcadApp.DocumentManager.DocumentDestroyed += _extractRoomConfigCmd.DocumentDestroyed;
        }

        

        public void Terminate()
        {
            //add code to clean up things when the ExtApp terminates. For example:
            //  Closing the log files;
            //  Deleting the custom ribbon tabs/panels/buttons;
            //  Unloading those dependents;
            //  Un-subscribing to those events;
            //  Etc.
        }

        /// <summary>
        /// 房间框线UI
        /// </summary>
        [CommandMethod("TIANHUACAD", "THFJKX2", CommandFlags.Modal)]
        public void THFJKX2()
        {
            if (uiRoomOutline != null && uiRoomOutline.IsLoaded)
                return;
            uiRoomOutline = new RoomOutlineUI();
            uiRoomOutline.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            AcadApp.ShowModelessWindow(uiRoomOutline);
        }

        /// <summary>
        /// 提取房间框线
        /// </summary>
        [CommandMethod("TIANHUACAD", "THEROC", CommandFlags.Modal)]
        public void ThExtractRoomOutlineConfig()
        {
            using (var cmd = new ThExtractRoomConfigCmd())
            {
                cmd.Execute();
            }
        }

        /// <summary>
        /// 梁配置
        /// </summary>
        [CommandMethod("TIANHUACAD", "THLPZ", CommandFlags.Modal)]
        public void ThExtractBeamAreaConfig()
        {
            var config = new ExtractLayerConfigUI();
            config.WindowStartupLocation = System.Windows.
                WindowStartupLocation.CenterScreen;
            AcadApp.ShowModelessWindow(config);
        }

        /// <summary>
        /// 框线对比
        /// </summary>
        [CommandMethod("TIANHUACAD", "THKXBD", CommandFlags.Modal)]
        public void ThFrameCompare()
        {
            var config = new FrameCompareUI();
            config.WindowStartupLocation = System.Windows.
                WindowStartupLocation.CenterScreen;
            AcadApp.ShowModelessWindow(config);
        }
    }
}
