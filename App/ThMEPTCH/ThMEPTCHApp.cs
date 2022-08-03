using AcHelper;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using ThMEPTCH.Model;
using ThMEPTCH.TCHDrawServices;

namespace ThMEPTCH
{
    class ThMEPTCHApp : IExtensionApplication
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

        [CommandMethod("TIANHUACAD", "THTZQJ", CommandFlags.Modal)]
        public void THTZQJ()
        {
            using (var docLock = Active.Document.LockDocument())
            using (var acad = AcadDatabase.Active())
            {
                var service = new TCHDrawCableTrayService();
                var telecObject = new ThTCHTelecObject
                {
                    Type = TelecObjectType.CableTray,
                };
                var startInterface = new ThTCHTelecInterface
                {
                    Position = new Point3d(0, 0, 0),
                    Breadth = 300,
                    Normal = new Vector3d(0, 0, 1),
                    Direction = new Vector3d(1, 0, 0),
                };
                var endInterface = new ThTCHTelecInterface
                {
                    Position = new Point3d(3000, 0, 0),
                    Breadth = 300,
                    Normal = new Vector3d(0, 0, 1),
                    Direction = new Vector3d(1, 0, 0),
                };
                var cableTray = new ThTCHCableTray
                {
                    ObjectId = telecObject,
                    Type = "C-01-10-3",
                    Style = CableTrayStyle.Trough,
                    CableTraySystem = CableTraySystem.CableTray,
                    Height = 100,
                    StartInterfaceId = startInterface,
                    EndInterfaceId = endInterface,
                };
                service.CableTrays.Add(cableTray);
                service.DrawExecute(true, false);
            }
        }
    }
}
