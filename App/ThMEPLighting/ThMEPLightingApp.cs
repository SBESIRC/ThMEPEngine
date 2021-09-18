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
            //获取车位信息
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new CommandManager();
                packageManager.ExtractParkStallProfiles();
            }
        }

        [CommandMethod("TIANHUACAD", "THParkGroup", CommandFlags.Modal)]
        public void THParkGroup()
        {
            //车位分组,没有必要抛出给用户，前期测试可以开放，后期去除
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new CommandManager();
                packageManager.GenerateParkGroup();
            }
        }

        [CommandMethod("TIANHUACAD", "THGroupLight", CommandFlags.Modal)]
        public void THGroupLight()
        {
            //车位灯生成
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new CommandManager();
                packageManager.GenerateGroupLight();
            }
        }

        [CommandMethod("TIANHUACAD", "THSrcLane", CommandFlags.Modal)]
        public void THSrcLane()
        {
            //原始车道线
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new CommandManager();
                packageManager.GenerateSrcLaneInfo();
            }
        }

        [CommandMethod("TIANHUACAD", "THExtendLane", CommandFlags.Modal)]
        public void THExtendLane()
        {
            //延长车道线
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new CommandManager();
                packageManager.ExtendLaneInfo();
            }
        }

        [CommandMethod("TIANHUACAD", "THLaneGroup", CommandFlags.Modal)]
        public void THLaneGroup()
        {
            //根据车位线将车位进行分组
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new CommandManager();
                packageManager.GenerateLaneGroup();
            }
        }

        [CommandMethod("TIANHUACAD", "THSideLaneConnect", CommandFlags.Modal)]
        public void THSideLaneConnect()
        {
            //生成车位灯，并根据车道线进行组内连线，没有考虑避让，传框线问题
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
