using Autodesk.AutoCAD.Runtime;
using ThPlatform3D.Command;

namespace ThPlatform3D
{
    public class ThPlatform3DApp : IExtensionApplication
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

        [CommandMethod("TIANHUACAD", "THBPI", CommandFlags.Modal)]
        public void THBPI()
        {
            using (var cmd = new ThInsertBasePointCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THLGHZ", CommandFlags.Modal)]
        public void THLGHZ()
        {
            using (var cmd = new ThRailDrawCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THQDHZ", CommandFlags.Modal)]
        public void THQDHZ()
        {
            using (var cmd = new ThWallHoleDrawCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THLBHZ", CommandFlags.Modal)]
        public void THLBHZ()
        {
            using (var cmd = new ThSlabDrawCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THJBHZ", CommandFlags.Modal)]
        public void THJBHZ()
        {
            using (var cmd = new ThDropSlabDrawCmd())
            {
                cmd.Execute();
            }
        }
    }
}
