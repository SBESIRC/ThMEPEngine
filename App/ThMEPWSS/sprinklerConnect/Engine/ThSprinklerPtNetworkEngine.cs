using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AcHelper;
using NFox.Cad;
using Linq2Acad;
using Dreambuild.AutoCAD;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.LaneLine;
using NetTopologySuite.Geometries;

using ThMEPWSS.DrainageSystemDiagram;
using ThMEPWSS.SprinklerConnect.Model;
using ThMEPWSS.SprinklerConnect.Service;
using ThMEPWSS.SprinklerConnect.Engine;

namespace ThMEPWSS.SprinklerConnect.Engine
{
    class ThSprinklerPtNetworkEngine
    {
        public static void getSprinklerPtNetwork(ThSprinklerParameter sprinklerParameter)
        {
            var sprinkPts = sprinklerParameter.SprinklerPt;
            var sprinkPtsCollect = sprinkPts.ToCollection();

            sprinkPts.ForEach(x => DrawUtils.ShowGeometry(x, "l0pt", 4, 30, 125));

            var dtOrthogonalSeg = ThSprinklerNetworkService.FindOrthogonalAngleFromDT(sprinkPtsCollect, out var dtSeg);

            CreateSegGroup(dtOrthogonalSeg, dtSeg, sprinkPts);
        }

        private static void CreateSegGroup(List<Line> dtOrthogonalSeg, List<Line> dtSeg, List<Point3d> pts)
        {
            var DTTol = ThSprinklerNetworkService.GetDTLength(dtOrthogonalSeg);

            var angleGroup = ThSprinklerNetworkService.ClassifyOrthogonalSeg(dtOrthogonalSeg);
            for (int i = 0; i < angleGroup.Count; i++)
            {
                DrawUtils.ShowGeometry(angleGroup[i].Value, string.Format("l0group{0}-{1}", i, angleGroup[i].Value.Count), i % 7);
            }

            ThSprinklerNetworkService.AddSingleDTLineToGroup(dtSeg, angleGroup);
            for (int i = 0; i < angleGroup.Count; i++)
            {
                DrawUtils.ShowGeometry(angleGroup[i].Value, string.Format("l1group{0}-{1}", i, angleGroup[i].Value.Count), i % 7);
            }

            ThSprinklerNetworkService.AddSinglePTToGroup(dtSeg, angleGroup, pts, DTTol * 1.5);
            for (int i = 0; i < angleGroup.Count; i++)
            {
                DrawUtils.ShowGeometry(angleGroup[i].Value, string.Format("l2group{0}-{1}", i, angleGroup[i].Value.Count), i % 7);
            }

            var filterGroup = ThSprinklerNetworkService.FilterMargedGroup(angleGroup);
            for (int i = 0; i < filterGroup.Count; i++)
            {
                DrawUtils.ShowGeometry(filterGroup.ElementAt(i).Value, string.Format("l0filterGroup{0}-{1}", i, filterGroup.ElementAt(i).Value.Count), i % 7);
            }

            //   var saperateGroup = ThSprinklerNetworkService.SeparateGroupDist(angleGroup, DTTol * 2);




        }


    }

}
