using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThMEPEngineCore.Diagnostics;

using ThMEPWSS.SprinklerDim.Model;
using ThMEPWSS.SprinklerDim.Service;

namespace ThMEPWSS.SprinklerDim.Engine
{
    public class ThSprinklerDimEngine
    {
        public static List<ThSprinklerNetGroup> GetSprinklerPtNetwork(List<Point3d> sprinkPts, List<Line> pipeLine, string printTag)
        {
            var netList = GetSprinklerPtOriginalNet(sprinkPts, pipeLine, out var dtSeg, printTag);

            var opNetList = GetSprinklerPtOptimizedNet(netList, dtSeg, printTag);
            return opNetList;
        }
           

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

        private static List<ThSprinklerNetGroup> GetSprinklerPtOptimizedNet(List<ThSprinklerNetGroup> netList, double DTTol, string printTag)
        {
            netList = ThSprinklerDimNetworkService.ChangeToOrthogonalCoordinates(netList);
            ThSprinklerDimNetworkService.CorrectGraphConnection(ref netList);

            var opNetList = new List<ThSprinklerNetGroup>();
            foreach (ThSprinklerNetGroup net in netList)
            {

                var pts = net.Pts;
                var graph = net.PtsGraph[0];

                ThOptimizeGroupService.CutoffLines(pts, ref graph, true, DTTol);
                ThOptimizeGroupService.CutoffLines(pts, ref graph, false, DTTol);

                var remainingLines = graph.Print(pts);
                var newNetGroup = ThSprinklerNetGraphService.CreateNetwork(0.0, remainingLines);

                opNetList.Add(newNetGroup);

            }
            opNetList = ThSprinklerDimNetworkService.SeparateGraph(opNetList);

            for (int i = 0; i < opNetList.Count; i++)
            {
                var net = opNetList[i];
                for (int j = 0; j < net.PtsGraph.Count; j++)
                {
                    var lines = net.PtsGraph[j].Print(net.Pts);
                    DrawUtils.ShowGeometry(lines, string.Format("sss-{2}-{0}-{1}", i, j, printTag), i % 7);
                }
            }

            return opNetList;
        }


    }
}
