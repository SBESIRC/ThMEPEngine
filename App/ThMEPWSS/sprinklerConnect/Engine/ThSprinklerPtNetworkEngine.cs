using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThMEPWSS.SprinklerConnect.Model;
using ThMEPWSS.SprinklerConnect.Service;
using ThMEPEngineCore.Diagnostics;
using ThMEPEngineCore.Algorithm;

namespace ThMEPWSS.SprinklerConnect.Engine
{
    public class ThSprinklerPtNetworkEngine
    {
        /// <summary> 
        /// 获取点位分片图
        /// </summary>
        /// <param name="sprinklerParameter"></param>
        public static List<ThSprinklerNetGroup> GetSprinklerPtNetwork(ThSprinklerParameter sprinklerParameter, List<Polyline> geometry, out double DTTol)
        {
            var sprinkPts = sprinklerParameter.SprinklerPt;
            var dtOrthogonalSeg = ThSprinklerNetworkService.FindOrthogonalAngleFromDT(sprinkPts, out var dtSeg);
            if (dtOrthogonalSeg.Count == 0)
            {
                DTTol = 1600.0;
                return new List<ThSprinklerNetGroup>();
            }
            DTTol = ThSprinklerNetworkService.GetDTLength(dtOrthogonalSeg);
            ThSprinklerNetworkService.FilterTooLongSeg(ref dtOrthogonalSeg, DTTol * 3);
            var netList = CreateSegGroup(dtOrthogonalSeg, dtSeg, sprinkPts, sprinklerParameter.SubMainPipe, DTTol, geometry);
            var netGroups = netList.Select(group => ThSprinklerNetGraphService
                .CreatePartGroup(group, sprinklerParameter.MainPipe, sprinklerParameter.SubMainPipe)).ToList();

            // 测试使用
            //var transformer = new ThMEPOriginTransformer(TransformerPt);
            //for (int i = 0; i < netGroups.Count; i++)
            //{
            //    var net = netGroups[i];
            //    for (int j = 0; j < net.PtsGraph.Count; j++)
            //    {
            //        var lines = net.PtsGraph[j].Print(net.Pts);
            //        lines.ForEach(line => transformer.Reset(line));
            //        DrawUtils.ShowGeometry(lines, string.Format("l3graph{0}-{1}", i, j), i % 7);
            //    }
            //    //net.Pts.ForEach(p => DrawUtils.ShowGeometry(transformer.Reset(p), "sprinkler", 0));
            //}

            return netGroups;
        }

        /// <summary>
        /// 点位按角度分组成图
        /// </summary>
        /// <param name="dtOrthogonalSeg"></param>
        /// <param name="dtSeg"></param>
        /// <param name="pts"></param>
        /// <returns></returns>
        private static List<ThSprinklerNetGroup> CreateSegGroup(List<Line> dtOrthogonalSeg, List<Line> dtSeg, List<Point3d> pts,
            List<Line> subMainPipe, double DTTol, List<Polyline> geometry)
        {
            var groupList = ThSprinklerNetworkService.ClassifyOrthogonalSeg(dtOrthogonalSeg);
            groupList = ThSprinklerNetworkService.FilterGroupByPt(groupList);

            // 往组里添加线
            ThSprinklerNetworkService.AddSingleDTLineToGroup(dtSeg, groupList, DTTol * 1.5);
            ThSprinklerNetworkService.AddSinglePTToGroup(groupList, pts, DTTol * 1.5);

            // 删除穿墙的线
            groupList = ThSprinklerNetworkService.DeleteWallLine(groupList, geometry);

            ThSprinklerNetworkService.AddShortLineToGroup(groupList, pts, subMainPipe, DTTol * 0.8);
            groupList = ThSprinklerNetworkService.DeleteWallLine(groupList, geometry);
            var netList = ThSprinklerNetGraphService.ConvertToNet(groupList);
            ThSprinklerNetworkService.FilterGroupNetByConvexHull(ref netList);
            return netList;

            //var sepaGroupList = SeparateNetByDist(netList, DTTol * 1.5);
        }

        ///// <summary>
        ///// 组转图
        ///// </summary>
        ///// <param name="groupList"></param>
        ///// <returns></returns>
        //private static List<ThSprinklerNetGroup> ConvertToNet(List<KeyValuePair<double, List<Line>>> groupList)
        //{
        //    var netList = new List<ThSprinklerNetGroup>();
        //    for (int i = 0; i < groupList.Count; i++)
        //    {
        //        var net = ThSprinklerNetGraphService.CreateNetwork(groupList[i].Key, groupList[i].Value);
        //        netList.Add(net);
        //    }

        //    return netList;
        //}

        /// <summary>
        /// 分离一组里面比较远的组
        /// </summary>
        /// <param name="netList"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        private List<KeyValuePair<double, List<Line>>> SeparateNetByDist(List<ThSprinklerNetGroup> netList, double tol)

        {
            var sepaGroupList = new List<KeyValuePair<double, List<Line>>>();
            for (int i = 0; i < netList.Count; i++)
            {
                sepaGroupList.AddRange(ThSprinklerNetworkService.SeparateNetByDist(netList[i], tol));
            }

            return sepaGroupList;
        }
    }
}
