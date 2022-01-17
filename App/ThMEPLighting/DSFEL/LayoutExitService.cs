using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Electrical;
using ThMEPLighting.DSFEL.ExitService;
using ThMEPLighting.DSFEL.Model;
using ThMEPLighting.DSFEL.Service;

namespace ThMEPLighting.DSFEL
{
    public class LayoutExitService
    {
        public List<ExitModel> LayoutFELService(List<ThIfcRoom> roomInfo, List<Polyline> door, List<Line> centerLines, List<Polyline> holes,
            ThEStoreys floor, ThMEPOriginTransformer originTransformer)
        {
            //计算块出口
            CalExitService calExitService = new CalExitService();
            var exitInfo = calExitService.CalExit(roomInfo, door, floor);

            return exitInfo;
        }

        /// <summary>
        /// 打印出入口图块
        /// </summary>
        /// <param name="exitModels"></param>
        public void PrintBlock(List<ExitModel> exitModels)
        {
            using (AcadDatabase db = AcadDatabase.Active())
            {
                db.Database.ImportModel(ThMEPLightingCommon.ExitEBlockName, ThMEPLightingCommon.EmgLightLayerName);
            }
            foreach (var model in exitModels)
            {
                double rotateAngle = (-Vector3d.XAxis).GetAngleTo(model.direction, Vector3d.ZAxis);
                if (model.exitType == ExitType.EvacuationExit)
                {
                    InsertBlockService.InsertBlock(ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.ExitEBlockName, model.positin, rotateAngle, 100, new Dictionary<string, string>() { { "T", "E" } });
                }
                else if (model.exitType == ExitType.SafetyExit)
                {
                    InsertBlockService.InsertBlock(ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.ExitSBlockName, model.positin, rotateAngle, 100, new Dictionary<string, string>() { { "T", "S" } });
                }
            }
        }
    }
}
