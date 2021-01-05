using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using ThMEPLighting.ParkingStall.Core;

namespace ThMEPLighting
{
    public class ThMEPLightingApp : IExtensionApplication
    {
        public void Initialize()
        {
            //throw new System.NotImplementedException();
        }

        public void Terminate()
        {
            //throw new System.NotImplementedException();
        }

        [CommandMethod("TIANHUACAD", "THParkProfile", CommandFlags.Modal)]
        public void THParkProfile()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new CommandManager();
                packageManager.ExtractParkStallProfiles();
            }
        }

        [CommandMethod("TIANHUACAD", "THParkGroup", CommandFlags.Modal)]
        public void THParkGroup()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new CommandManager();
                packageManager.GenerateParkGroup();
            }
        }

        [CommandMethod("TIANHUACAD", "THGroupLight", CommandFlags.Modal)]
        public void THGroupLight()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new CommandManager();
                packageManager.GenerateGroupLight();
            }
        }

        [CommandMethod("TIANHUACAD", "THSrcLane", CommandFlags.Modal)]
        public void THSrcLane()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new CommandManager();
                packageManager.GenerateSrcLaneInfo();
            }
        }

        [CommandMethod("TIANHUACAD", "THExtendLane", CommandFlags.Modal)]
        public void THExtendLane()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new CommandManager();
                packageManager.ExtendLaneInfo();
            }
        }

        [CommandMethod("TIANHUACAD", "THLaneGroup", CommandFlags.Modal)]
        public void THLaneGroup()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new CommandManager();
                packageManager.GenerateLaneGroup();
            }
        }
    }
}
