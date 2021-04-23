using Linq2Acad;
using Autodesk.AutoCAD.Runtime;
using ThMEPLighting.ParkingStall.Core;

namespace ThMEPLighting
{
    public class ThMEPLightingApp : IExtensionApplication
    {
        public void Initialize()
        {
            //
        }

        public void Terminate()
        {
            //
        }

        [CommandMethod("TIANHUACAD", "THPARKPROFILE", CommandFlags.Modal)]
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

        [CommandMethod("TIANHUACAD", "THSubGroupAdjustor", CommandFlags.Modal)]
        public void THSubGroupAdjustor()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new CommandManager();
                packageManager.LaneSubGroupOptimization();
            }
        }

        [CommandMethod("TIANHUACAD", "THSideLaneConnect", CommandFlags.Modal)]
        public void THSideLaneConnect()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new CommandManager();
                packageManager.SideLaneConnect();
            }
        }

        [CommandMethod("TIANHUACAD", "THLaneConnect", CommandFlags.Modal)]
        public void LaneConnect()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new CommandManager();
                packageManager.THLaneConnect();
            }
        }
    }
}
