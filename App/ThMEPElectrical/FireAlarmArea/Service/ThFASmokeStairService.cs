using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;

using ThCADExtension;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Config;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Stair;
using ThMEPElectrical.AFAS;
using ThMEPElectrical.AFAS.Model;
using ThMEPElectrical.AFAS.Utils;
using ThMEPElectrical.FireAlarmArea.Model;

namespace ThMEPElectrical.FireAlarmArea.Service
{
    class ThFASmokeStairService
    {
        /// <summary>
        /// 楼梯部分布置
        /// 最终结果点位写到layoutParameter
        /// 返回最终布置点位，方向
        /// </summary>
        /// <param name="dataQuery"></param>
        /// <param name="layoutParameter"></param>
        /// <returns></returns>
        public static List<ThLayoutPt> LayoutStair(ThAFASSmokeLayoutParameter layoutParameter)
        {
            var transformer = layoutParameter.transformer;
            var pts = layoutParameter.framePts;
            var scale = layoutParameter.Scale;
            var stairNormalPts = new List<Point3d>();
            var stairEmgPts = new List<Point3d>();
            var resultPts = new List<ThLayoutPt>();
            var obstacle = new List<Polyline>();

            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                //obstacle
                obstacle.AddRange(layoutParameter.DoorOpenings.Select(x => x.Boundary as Polyline).ToList());
                obstacle.AddRange(layoutParameter.Windows.Select(x => x.Boundary as Polyline).ToList());

                //boundary obstacle 到原位置
                var stairBoundary = layoutParameter.RoomType.Where(x => x.Value == ThFaSmokeCommon.layoutType.stair).Select(x => x.Key).ToList();
                stairBoundary.ForEach(x => transformer.Reset(x));
                obstacle.ForEach(x=>transformer.Reset(x));

                var stairEngine = new ThStairEquimentLayout();
                var stairFireDetector = stairEngine.StairFireDetector(acadDatabase.Database, stairBoundary, obstacle, pts, scale);
                var stairFirePts = stairFireDetector.Select(x => x.Key).ToList();
                foreach (var r in stairFireDetector)
                {
                    resultPts.Add(new ThLayoutPt() { Pt = r.Key, Angle = r.Value, BlkName = layoutParameter.BlkNameSmoke });
                }

                //楼梯间结果，楼梯房间框线转到原点位置
                stairBoundary.ForEach(x => transformer.Transform(x));
                obstacle.ForEach(x => transformer.Transform(x));
                stairFirePts = stairFirePts.Select(x => transformer.Transform(x)).ToList();

                layoutParameter.StairPartResult.AddRange(stairFirePts);
                ////

                return resultPts;
            }
        }
    }
}
