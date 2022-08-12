using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThMEPEngineCore.Diagnostics;
using ThMEPWSS.SprinklerDim.Service;
using ThMEPWSS.SprinklerDim.Model;

namespace ThMEPWSS.SprinklerDim.Service
{
    public static class ThCreateGroupService
    {
        ///// <summary>
        ///// 点位按角度分组成图
        ///// </summary>
        ///// <param name="dtOrthogonalSeg"></param>
        ///// <param name="dtSeg"></param>
        ///// <param name="pts"></param>
        ///// <returns></returns>
        //public static List<ThSprinklerNetGroup> CreateSegGroupOri(List<Line> dtOrthogonalSeg, List<Line> dtSeg, List<Point3d> pts, double DTTol,string printTag)
        //{
        //    var groupList = ThSprinklerNetworkService.ClassifyOrthogonalSeg(dtOrthogonalSeg);
        //    //for (int i = 0; i < groupList.Count; i++)
        //    //{
        //    //    DrawUtils.ShowGeometry(groupList[i].Value, string.Format("l1group{0}-{1}", i, groupList[i].Value.Count), i % 7);
        //    //}

        //    groupList = ThSprinklerNetworkService.FilterGroupByPt(groupList);
        //    //for (int i = 0; i < groupList.Count; i++)
        //    //{
        //    //    DrawUtils.ShowGeometry(groupList.ElementAt(i).Value, string.Format("l2filterShortGroup{0}-{1}", i, groupList.ElementAt(i).Value.Count), i % 7);
        //    //}

        //    // 往组里添加线
        //    ThSprinklerNetworkService.AddSingleDTLineToGroup(dtSeg, groupList, DTTol * 1.5);
        //    //for (int i = 0; i < groupList.Count; i++)
        //    //{
        //    //    DrawUtils.ShowGeometry(groupList[i].Value, string.Format("l3addSameAngleDT{0}-{1}", i, groupList[i].Value.Count), i % 7);
        //    //}

        //    ThSprinklerNetworkService.AddSinglePTToGroup(groupList, pts, DTTol * 1.5);
        //    //for (int i = 0; i < groupList.Count; i++)
        //    //{
        //    //    DrawUtils.ShowGeometry(groupList[i].Value, string.Format("l4addLineInTol{0}-{1}", i, groupList[i].Value.Count), i % 7);
        //    //}

        //    var netList = ThSprinklerNetGraphService.ConvertToNet(groupList);
        //    //for (int i = 0; i < netList.Count; i++)
        //    //{
        //    //    var net = netList[i];
        //    //    for (int j = 0; j < net.PtsGraph.Count; j++)
        //    //    {
        //    //        var lines = net.PtsGraph[j].Print(net.Pts);
        //    //        DrawUtils.ShowGeometry(lines, string.Format("l5graphToNet{0}-{1}", i, j), i % 7);
        //    //    }
        //    //}

        //    //var convexList = ThSprinklerNetworkService.FilterGroupNetByConvexHull(ref netList);
        //    //DrawUtils.ShowGeometry(convexList, "l6Convex", lineWeightNum: 30);
        //    //for (int i = 0; i < netList.Count; i++)
        //    //{
        //    //    var net = netList[i];
        //    //    for (int j = 0; j < net.PtsGraph.Count; j++)
        //    //    {
        //    //        var lines = net.PtsGraph[j].Print(net.Pts);
        //    //        DrawUtils.ShowGeometry(lines, string.Format("l6filterGraphConvex{0}-{1}", i, j), i % 7);
        //    //    }
        //    //}

        //    var separateGraph = ThSprinklerNetGraphService.SeparateGraph(netList);
        //    for (int i = 0; i < separateGraph.Count; i++)
        //    {
        //        var net = separateGraph[i];
        //        for (int j = 0; j < net.PtsGraph.Count; j++)
        //        {
        //            var lines = net.PtsGraph[j].Print(net.Pts);
        //            DrawUtils.ShowGeometry(lines, string.Format("l7separateGraph-{2}-{0}-{1}", i, j, printTag), i % 7);
        //        }
        //    }

        //    return separateGraph;

        //}

        ///// <summary>
        ///// 分离一组里面比较远的组
        ///// </summary>
        ///// <param name="netList"></param>
        ///// <param name="tol"></param>
        ///// <returns></returns>
        //private static List<KeyValuePair<double, List<Line>>> SeparateNetByDist(List<ThSprinklerNetGroup> netList, double tol)

        //{
        //    var sepaGroupList = new List<KeyValuePair<double, List<Line>>>();
        //    for (int i = 0; i < netList.Count; i++)
        //    {
        //        sepaGroupList.AddRange(ThSprinklerNetworkService.SeparateNetByDist(netList[i], tol));
        //    }

        //    return sepaGroupList;
        //}

        /// <summary>
        /// 点位按角度分组成图
        /// </summary>
        /// <param name="dtOrthogonalSeg"></param>
        /// <param name="dtSeg"></param>
        /// <param name="pts"></param>
        /// <returns></returns>
        public static List<ThSprinklerNetGroup> CreateSegGroup(List<Line> dtOrthogonalSeg, string printTag)
        {
            var groupList = ThSprinklerNetworkService.ClassifyOrthogonalSeg(dtOrthogonalSeg);
            for (int i = 0; i < groupList.Count; i++)
            {
                DrawUtils.ShowGeometry(groupList[i].Value, string.Format("l1-{0}-group-{1}-{2}", printTag, i, groupList[i].Value.Count), i % 7);
            }

            //groupList = ThSprinklerNetworkService.FilterGroupByPt(groupList);
            //for (int i = 0; i < groupList.Count; i++)
            //{
            //    DrawUtils.ShowGeometry(groupList.ElementAt(i).Value, string.Format("l2filterShortGroup{0}-{1}-{2}", printTag, i, groupList.ElementAt(i).Value.Count), i % 7);
            //}

            //// 往组里添加线
            //ThSprinklerNetworkService.AddSingleDTLineToGroup(dtSeg, groupList, DTTol * 1.5);
            //for (int i = 0; i < groupList.Count; i++)
            //{
            //    DrawUtils.ShowGeometry(groupList[i].Value, string.Format("l3addSameAngleDT{0}-{1}-{2}", printTag, i, groupList[i].Value.Count), i % 7);
            //}

            //ThSprinklerNetworkService.AddSinglePTToGroup(groupList, pts, DTTol * 1.5);
            //for (int i = 0; i < groupList.Count; i++)
            //{
            //    DrawUtils.ShowGeometry(groupList[i].Value, string.Format("l4addLineInTol{0}-{1}-{2}", printTag, i, groupList[i].Value.Count), i % 7);
            //}

            var netList = ThSprinklerNetGraphService.ConvertToNet(groupList);
            for (int i = 0; i < netList.Count; i++)
            {
                var net = netList[i];
                for (int j = 0; j < net.PtsGraph.Count; j++)
                {
                    var lines = net.PtsGraph[j].Print(net.Pts);
                    DrawUtils.ShowGeometry(lines, string.Format("l5-{0}-graphToNet-{1}-{2}", printTag, i, j), i % 7);
                }
            }

            //var convexList = ThSprinklerNetworkService.FilterGroupNetByConvexHull(ref netList);
            //DrawUtils.ShowGeometry(convexList, "l6Convex", lineWeightNum: 30);
            //for (int i = 0; i < netList.Count; i++)
            //{
            //    var net = netList[i];
            //    for (int j = 0; j < net.PtsGraph.Count; j++)
            //    {
            //        var lines = net.PtsGraph[j].Print(net.Pts);
            //        DrawUtils.ShowGeometry(lines, string.Format("l6filterGraphConvex{0}-{1}", i, j), i % 7);
            //    }
            //}

            //var separateGraph = ThSprinklerDimNetworkService.SeparateGraph(netList);
            //for (int i = 0; i < separateGraph.Count; i++)
            //{
            //    var net = separateGraph[i];
            //    for (int j = 0; j < net.PtsGraph.Count; j++)
            //    {
            //        var lines = net.PtsGraph[j].Print(net.Pts);
            //        DrawUtils.ShowGeometry(lines, string.Format("l7-{0}-separateGraph-{1}-{2}", printTag, i, j), i % 7);
            //    }
            //}

            return netList;

        }

        public static void ThSpinrklerAddSinglePtToNetGroup(ref List<ThSprinklerNetGroup> netGroup, List<Point3d> sprinkler, Dictionary<Point3d, double> ptAngleDict)
        {
            var singlePts = new List<Point3d>();
            singlePts.AddRange(sprinkler.Except(netGroup.SelectMany(x => x.Pts)));

            foreach (var pt in singlePts)
            {
                ptAngleDict.TryGetValue(pt, out var angle);
                if (angle != 0)
                {
                    var group = netGroup.Where(x => ThSprinklerLineService.IsOrthogonalAngle(x.Angle, angle, 2));
                    //先看第一个之后细化：点组外包框内， 如果没有group咋办等
                    if (group.Any())
                    {
                        var selectGroup = group.First();
                        var idxPt = selectGroup.AddPt(pt);
                        var singleGraph = new ThSprinklerGraph();
                        singleGraph.AddVertex(idxPt);
                        selectGroup.PtsGraph.Add(singleGraph);
                    }
                    else
                    {

                    }
                }
            }


        }
    }
}
