using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThMEPEngineCore.Diagnostics;
using ThMEPEngineCore.Algorithm.ClusteringAlgorithm;

using ThMEPWSS.SprinklerDim.Model;
using ThMEPWSS.SprinklerDim.Service;

namespace ThMEPWSS.SprinklerDim.Engine
{
    public class ThSprinklerDimEngine
    {
        public static void GetSprinklerPtNetwork(List<Point3d> sprinkPts, List<Line> pipeLine, string printTag)
        {
            var netList = GetSprinklerPtOriginalNet(sprinkPts, pipeLine, out var dtSeg, printTag);

        }

        //private static List<ThSprinklerNetGroup> GetSprinklerPtOriginalNetOri(List<Point3d> sprinkPts, List<Line> pipeLine, out double DTTol, string printTag)
        //{
        //    DTTol = 1600.0;

        //    var ptAngleDict = ThSprinklerNetworkService.GetAngleToPt(sprinkPts, pipeLine);

        //    var dtOrthogonalSeg = ThSprinklerNetworkService.FindOrthogonalAngleFromDT(sprinkPts, out var dtSeg);
        //    DrawUtils.ShowGeometry(dtSeg, string.Format("l0DT-{0}", printTag), 140);
        //    DrawUtils.ShowGeometry(dtOrthogonalSeg, string.Format("l0DTOrtho-{0}", printTag), 241);

        //    DTTol = ThSprinklerNetworkService.GetDTLength(dtOrthogonalSeg);

        //    ThSprinklerNetworkService.FilterTooLongSeg(ref dtOrthogonalSeg, DTTol * 3);

        //    var netList = ThCreateGroupService.CreateSegGroup(dtOrthogonalSeg, dtSeg, sprinkPts, DTTol, printTag);

        //    return netList;
        //}

        private static List<ThSprinklerNetGroup> GetSprinklerPtOriginalNet(List<Point3d> sprinkPts, List<Line> pipeLine, out double DTTol, string printTag)
        {
            DTTol = 1600.0;

            var ptAngleDict = ThSprinklerNetworkService.GetAngleToPt(sprinkPts, pipeLine);

            foreach (var pt in ptAngleDict)
            {
                var dir = Vector3d.XAxis.RotateBy(pt.Value, Vector3d.ZAxis);
                DrawUtils.ShowGeometry(pt.Key, dir, String.Format("l0prDir-{0}", printTag), 3, 30);
            }
            var dtSeg = ThSprinklerNetworkService.GetDTSeg(sprinkPts);

            var dtOrthogonalSeg = ThSprinklerNetworkService.FindOrthogonalAngleFromDT(sprinkPts, dtSeg);
            DrawUtils.ShowGeometry(dtSeg, string.Format("l0DT-{0}", printTag), 140);
            DrawUtils.ShowGeometry(dtOrthogonalSeg, string.Format("l0DTOrtho-{0}", printTag), 241);

            DTTol = ThSprinklerNetworkService.GetDTLength(dtOrthogonalSeg);

            ThSprinklerNetworkService.FilterTooLongSeg(ref dtOrthogonalSeg, DTTol * 3);

            var filterDTAngle = ThSprinklerNetworkService.FilterDTOrthogonalToPipe(dtOrthogonalSeg, ptAngleDict);
            filterDTAngle.ForEach(x => DrawUtils.ShowGeometry(x, string.Format("l0filterAngleDT-{0}", printTag), 140));

            var netList = ThCreateGroupService.CreateSegGroup(filterDTAngle, dtSeg, sprinkPts, ptAngleDict, DTTol, printTag);

            return netList;
        }



    }
}
