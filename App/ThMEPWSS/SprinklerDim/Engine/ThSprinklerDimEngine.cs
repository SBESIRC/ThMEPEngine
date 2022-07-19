using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThMEPEngineCore.Diagnostics;
using ThMEPEngineCore.Algorithm.ClusteringAlgorithm;

using ThMEPWSS.SprinklerConnect.Service;
using ThMEPWSS.SprinklerConnect.Model;
using ThMEPWSS.ThSprinklerDim.Service;

namespace ThMEPWSS.ThSprinklerDim.Engine
{
    public class ThSprinklerDimEngine
    {
        public static void GetSprinklerPtNetwork(List<Point3d> sprinkPts, string printTag)
        {
            var netList = GetSprinklerPtOriginalNet(sprinkPts, out var dtSeg, printTag);


            var pointDBSacn = new PointDBScan(netList[0].Pts, new List<Line>());
            var clusters = pointDBSacn.ClusterResult(3400, 1, 1000);

            for (int i = 0; i < clusters.Count(); i++)
            {
                for (int j = 0; j < clusters[i].Count(); j++)
                {
                    DrawUtils.ShowGeometry(clusters[i][j], "l0cluster", i % 7, r: 30);
                }
            }
        }

        private static List<ThSprinklerNetGroup> GetSprinklerPtOriginalNet(List<Point3d> sprinkPts, out double DTTol, string printTag)
        {

            var dtOrthogonalSeg = ThSprinklerNetworkService.FindOrthogonalAngleFromDT(sprinkPts, out var dtSeg);
            //DrawUtils.ShowGeometry(dtOrthogonalSeg, "l0DTO", 241);

            if (dtOrthogonalSeg.Count == 0)
            {
                DTTol = 1600.0;
                return new List<ThSprinklerNetGroup>();
            }

            DTTol = ThSprinklerNetworkService.GetDTLength(dtOrthogonalSeg);
            ThSprinklerNetworkService.FilterTooLongSeg(ref dtOrthogonalSeg, DTTol * 3);
            var netList = ThCreateGroupService.CreateSegGroup(dtOrthogonalSeg, dtSeg, sprinkPts, DTTol, printTag);

            return netList;
        }




    }
}
