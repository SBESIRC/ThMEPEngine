using Linq2Acad;
using Autodesk.AutoCAD.Runtime;
using ThMEPLighting.ParkingStall.Core;
using Autodesk.AutoCAD.EditorInput;
using AcHelper;
using ThMEPLighting.ParkingStall.Business.UserInteraction;
using DotNetARX;
using GeometryExtensions;

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

        [CommandMethod("TIANHUACAD", "THSubGroupAdjustor", CommandFlags.Modal)]
        public void THSubGroupAdjustor()
        {
            //根据车位分组和车道线生成相应的数据
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new CommandManager();
                packageManager.LaneSubGroupOptimization();
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

        [CommandMethod("TIANHUACAD", "THLightConnect", CommandFlags.Modal)]
        public void LightConnect()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var wallPolylines = EntityPicker.MakeUserPickPolys();
                if (wallPolylines.Count == 0)
                    return;
                if (wallPolylines.Count > 1)
                {
                    Active.Database.GetEditor().WriteMessage("选择了多个区域，该功能只支持单个区域，请选择一个区域进行后续操作");
                    return;
                }
                PromptPointOptions pPtOpts = new PromptPointOptions("请选择配电箱所在点");
                var result = Active.Editor.GetPoint(pPtOpts);
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                var point = result.Value;
                point = point.TransformBy(Active.Editor.UCS2WCS());
                var packageManager = new ParkLightConnectCommand(wallPolylines[0], point);
                packageManager.Execute();
                if (packageManager.ErrorMsgs != null && packageManager.ErrorMsgs.Count > 0) 
                {
                    string msg = string.Join("，", packageManager.ErrorMsgs.ToArray());
                    Active.Database.GetEditor().WriteMessage(msg);
                }
            }
        }
    }
}
