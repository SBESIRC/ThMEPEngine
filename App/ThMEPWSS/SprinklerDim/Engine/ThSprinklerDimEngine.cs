using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThCADCore.NTS;

using ThMEPEngineCore.Diagnostics;

using ThMEPWSS.SprinklerDim.Model;
using ThMEPWSS.SprinklerDim.Service;

namespace ThMEPWSS.SprinklerDim.Engine
{
    public class ThSprinklerDimEngine
    {
        public static List<ThSprinklerNetGroup> GetSprinklerPtNetwork(List<Point3d> sprinkPts, List<Line> pipeLine, string printTag, out double dtSeg)
        {
            var netList = GetSprinklerPtOriginalNet(sprinkPts, pipeLine, out dtSeg, printTag);

            var opNetList = GetSprinklerPtOptimizedNet(netList, dtSeg, printTag);
            return opNetList;

            //// test
        }


        private static List<ThSprinklerNetGroup> GetSprinklerPtOriginalNet(List<Point3d> sprinkPts, List<Line> pipeLine, out double DTTol, string printTag)
        {
            DTTol = 1600.0;

            var ptAngleDict = ThSprinklerNetworkService.GetAngleToPt(sprinkPts, pipeLine);

            foreach (var pt in ptAngleDict)
            {
                var dir = Vector3d.XAxis.RotateBy(pt.Value, Vector3d.ZAxis);
                DrawUtils.ShowGeometry(pt.Key, dir, String.Format("l0-{0}-prDir", printTag), 3, 30);
            }

            var dtSeg = ThSprinklerNetworkService.GetDTSeg(sprinkPts);
            DrawUtils.ShowGeometry(dtSeg, string.Format("l0-{0}-DT", printTag), 180);

            var ptDtOriDict = ThSprinklerNetworkService.GetConnectPtDict(sprinkPts, dtSeg);

            var ptDtDict = ThSprinklerNetworkService.FilterDTOrthogonalToPipeAngle(ptDtOriDict, ptAngleDict);
            var allLine = ptDtDict.SelectMany(x => x.Value).Distinct().ToList();
            DrawUtils.ShowGeometry(allLine, string.Format("l0-{0}-ptDTOrtho", printTag), 241, 30);

            ThSprinklerNetworkService.AddOrthoDTIfNoLine(ref ptDtDict, ptDtOriDict);
            var allLine2 = ptDtDict.SelectMany(x => x.Value).Distinct().ToList();
            DrawUtils.ShowGeometry(allLine2, string.Format("l0-{0}-ptDTOrthoAdd", printTag), 141, 30);

            DTTol = ThSprinklerNetworkService.GetDTLength(allLine2);
            DrawUtils.ShowGeometry(sprinkPts[0], String.Format("dtLeng:{0}", DTTol), String.Format("l0-{0}-dtLen", printTag), hight: 200);

            //DTTol = 2500;
            ThSprinklerNetworkService.AddSinglePTToGroup(ref ptDtDict, ptAngleDict, DTTol * 1.5);
            var allLineFinal = ptDtDict.SelectMany(x => x.Value).ToList();
            ThSprinklerNetworkService.RemoveDuplicate(ref allLineFinal);
            DrawUtils.ShowGeometry(allLineFinal, string.Format("l0-{0}-ptDTOrthoAdd2", printTag), 41, 30);

            ThSprinklerNetworkService.FilterTooLongSeg(ref allLineFinal, DTTol * 3);
            DrawUtils.ShowGeometry(allLineFinal, string.Format("l0-{0}-ptDTRemoveTooLong", printTag), 171, 30);

            //return new List<ThSprinklerNetGroup>();

            var netList = ThCreateGroupService.CreateSegGroup(allLineFinal, printTag);

            return netList;
        }





        private static List<ThSprinklerNetGroup> GetSprinklerPtOptimizedNet(List<ThSprinklerNetGroup> netList, double DTTol, string printTag)
        {
            List<ThSprinklerNetGroup> transNetList = ThSprinklerDimNetworkService.ChangeToOrthogonalCoordinates(netList);
            ThSprinklerDimNetworkService.CorrectGraphConnection(ref transNetList, 45.0);
            ThSprinklerDimNetworkService.GenerateCollineationGroup(ref transNetList);

            List<ThSprinklerNetGroup> opNetList = new List<ThSprinklerNetGroup>();
            foreach (ThSprinklerNetGroup netGroup in transNetList)
            {
                var pts = netGroup.Pts;
                for(int i = 0; i < netGroup.PtsGraph.Count; i++)
                {
                    ThSprinklerGraph graph = netGroup.PtsGraph[i];
                    ThOptimizeGroupService.CutoffLines(pts, ref graph, netGroup.XCollineationGroup[i], true);
                    ThOptimizeGroupService.CutoffLines(pts, ref graph, netGroup.YCollineationGroup[i], false);

                    List<Line> remainingLines = graph.Print(pts);
                    ThSprinklerNetGroup newNetGroup = ThSprinklerNetGraphService.CreateNetwork(netGroup.Angle, remainingLines);
                    newNetGroup.Transformer = netGroup.Transformer;
                    newNetGroup.XCollineationGroup = netGroup.XCollineationGroup;
                    newNetGroup.YCollineationGroup = netGroup.YCollineationGroup;
                    opNetList.Add(newNetGroup);
                }
            }

            for (int i = 0; i < opNetList.Count; i++)
            {
                var net = opNetList[i];
                for (int j = 0; j < net.PtsGraph.Count; j++)
                {
                    List<Point3d> pts = ThChangeCoordinateService.MakeTransformation(net.Pts, net.Transformer.Inverse());
                    var lines = net.PtsGraph[j].Print(pts);
                    DrawUtils.ShowGeometry(lines, string.Format("SSS-{2}-{0}-{1}", i, j, printTag), i % 7);
                }
            }

            return opNetList;
        }


    }
}
