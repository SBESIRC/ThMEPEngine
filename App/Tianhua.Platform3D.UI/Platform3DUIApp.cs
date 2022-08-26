using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using Tianhua.Platform3D.UI.UI;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Tianhua.Platform3D.UI
{
    public class Platform3DUIApp : IExtensionApplication
    {
        PaletteSet mainPaletteSet = null;
        public void Initialize()
        {
            //add code to run when the ExtApp initializes. Here are a few examples:
            //  Checking some host information like build #, a patch or a particular Arx/Dbx/Dll;
            //  Creating/Opening some files to use in the whole life of the assembly, e.g. logs;
            //  Adding some ribbon tabs, panels, and/or buttons, when necessary;
            //  Loading some dependents explicitly which are not taken care of automatically;
            //  Subscribing to some events which are important for the whole session;
            //  Etc.
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
        [CommandMethod("TIANHUACAD", "TH3D", CommandFlags.Modal)]
        public void ThTH3D()
        {
            if (mainPaletteSet == null)
            {
                mainPaletteSet = new PaletteSet("天华三维设计面板");
                var mainUControl = new PlatformMainUI();
                mainPaletteSet.AddVisual("", mainUControl);
            }
            mainPaletteSet.KeepFocus = true;
            mainPaletteSet.Visible = true;
            mainPaletteSet.DockEnabled = DockSides.Left;
            mainPaletteSet.Dock = DockSides.Left;
        }
    }
}
