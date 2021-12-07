using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;

using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Stair;
using ThMEPElectrical.AFAS.Model;
using ThMEPLighting.IlluminationLighting.Model;

namespace ThMEPLighting.IlluminationLighting.Service
{
    class ThIlluminationStairService
    {
        /// <summary>
        /// 楼梯部分布置
        /// 最终结果点位写到layoutParameter
        /// 返回最终布置点位，方向
        /// </summary>
        /// <param name="dataQuery"></param>
        /// <param name="layoutParameter"></param>
        /// <returns></returns>
        public static List<ThLayoutPt> LayoutStair(ThAFASIlluminationLayoutParameter layoutParameter)
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

                // boundary 到原位置
                var stairBoundary = layoutParameter.roomType.Where(x => x.Value == ThIlluminationCommon.LayoutType.stair).Select(x => x.Key).ToList();
                stairBoundary.ForEach(x => transformer.Reset(x));
                obstacle.ForEach(x => transformer.Reset(x));

                var stairEngine = new ThStairEquimentLayout();
                var stairNormalLight = stairEngine.StairNormalLighting(acadDatabase.Database, stairBoundary, obstacle, pts, scale);
                stairNormalPts = stairNormalLight.Select(x => x.Key).ToList();
                foreach (var r in stairNormalLight)
                {
                    resultPts.Add(new ThLayoutPt() { Pt = r.Key, Angle = r.Value, BlkName = layoutParameter.BlkNameN });
                }

                if (layoutParameter.ifLayoutEmg)
                {
                    var stairEmgEngine = new ThStairEquimentLayout();
                    var stairEmg = stairEmgEngine.StairEvacuationLighting(acadDatabase.Database, stairBoundary, obstacle, pts, scale);
                    stairEmgPts = stairEmg.Select(x => x.Key).ToList();
                    foreach (var r in stairEmg)
                    {
                        resultPts.Add(new ThLayoutPt() { Pt = r.Key, Angle = r.Value, BlkName = layoutParameter.BlkNameE });
                    }
                }

                //楼梯间结果，楼梯房间框线转到原点位置
                stairBoundary.ForEach(x => transformer.Transform(x));
                obstacle.ForEach(x => transformer.Transform(x));
                stairNormalPts = stairNormalPts.Select(x => transformer.Transform(x)).ToList();
                stairEmgPts = stairEmgPts.Select(x => transformer.Transform(x)).ToList();

                layoutParameter.stairPartResult.AddRange(stairNormalPts);
                layoutParameter.stairPartResult.AddRange(stairEmgPts);

                return resultPts;
            }
        }
    }
}
