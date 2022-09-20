using Autodesk.AutoCAD.Runtime;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace ThPlatform3D
{
    public class ThPlatform3DApp : IExtensionApplication
    {
        /// <summary>
        /// 自定义右键菜单
        /// </summary>
        private ThPlatform3dDefaultContextMenuExtension DefaultContextMenuExtension { get; set; }

        public void Initialize()
        {
            //add code to run when the ExtApp initializes. Here are a few examples:
            //  Checking some host information like build #, a patch or a particular Arx/Dbx/Dll;
            //  Creating/Opening some files to use in the whole life of the assembly, e.g. logs;
            //  Adding some ribbon tabs, panels, and/or buttons, when necessary;
            //  Loading some dependents explicitly which are not taken care of automatically;
            //  Subscribing to some events which are important for the whole session;
            //  Etc.

            // 支持自定义右键菜单
            // https://spiderinnet1.typepad.com/blog/2012/02/autocad-net-attach-application-default-context-menu-extension.html
            DefaultContextMenuExtension = new ThPlatform3dDefaultContextMenuExtension();
            AcadApp.AddDefaultContextMenuExtension(DefaultContextMenuExtension);
        }

        public void Terminate()
        {
            //add code to clean up things when the ExtApp terminates. For example:
            //  Closing the log files;
            //  Deleting the custom ribbon tabs/panels/buttons;
            //  Unloading those dependents;
            //  Un-subscribing to those events;
            //  Etc.

            AcadApp.RemoveDefaultContextMenuExtension(DefaultContextMenuExtension);
        }
    }
}
