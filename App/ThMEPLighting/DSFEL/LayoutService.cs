using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Model;
using ThMEPLighting.DSFEL.ExitService;
using ThMEPLighting.DSFEL.Model;
using ThMEPLighting.DSFEL.Service;

namespace ThMEPLighting.DSFEL
{
    public class LayoutService
    {
        public List<RoomInfoModel> LayoutFELService(List<ThIfcRoom> roomInfo, List<Polyline> door, List<Line> centerLines, List<Polyline> holes, ThMEPOriginTransformer originTransformer)
        {
            //计算块出口
            CalExitService calExitService = new CalExitService();
            var exitInfo = calExitService.CalExit(roomInfo, door);
            
            //创建疏散路径
            CreateEvacuationPathService evacuationPath = new CreateEvacuationPathService();
            var evaPaths = evacuationPath.CreatePath(exitInfo, centerLines, holes);

            //打印出入口图块
            exitInfo.ForEach(x => x.positin = originTransformer.Reset(x.positin));
            PrintBlock(exitInfo);

            //打印路径
            //PrintPathService printService = new PrintPathService();
            //printService.PrintPath(evaPaths.SelectMany(x => x.evacuationPaths).ToList(), centerLines, originTransformer);

            return evaPaths;
        }

        /// <summary>
        /// 打印出入口图块
        /// </summary>
        /// <param name="exitModels"></param>
        private void PrintBlock(List<ExitModel> exitModels)
        {
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
