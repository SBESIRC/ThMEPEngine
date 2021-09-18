using AcHelper;
using Linq2Acad;
using DotNetARX;
using GeometryExtensions;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using ThMEPLighting.ParkingStall.Core;
using ThMEPLighting.ParkingStall.Business.UserInteraction;

namespace ThMEPLighting
{
    public class ThMEPParkingStallLightingCmd
    {
        /// <summary>
        /// 车位照明布置
        /// </summary>
        [CommandMethod("TIANHUACAD", "THCWZM", CommandFlags.Modal)]
        public void THCWZM()
        {
            //根据车位分组和车道线生成相应的数据
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new CommandManager();
                packageManager.LaneSubGroupOptimization();
            }
        }

        /// <summary>
        /// 车位照明连线
        /// </summary>
        [CommandMethod("TIANHUACAD", "THCWZMLX", CommandFlags.Modal)]
        public void THCWZMLX()
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
