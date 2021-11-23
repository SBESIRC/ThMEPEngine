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
    public class ThSprinklerPtNetworkEngine
    {
        /// <summary>
        /// 获取点位分片图
        /// </summary>
        /// <param name="sprinklerParameter"></param>
        public static void GetSprinklerPtNetwork(ThSprinklerParameter sprinklerParameter)
        {
            var sprinkPts = sprinklerParameter.SprinklerPt;
            sprinkPts.ForEach(x => DrawUtils.ShowGeometry(x, "l0pt", 4, 30, 125));

            var dtOrthogonalSeg = ThSprinklerNetworkService.FindOrthogonalAngleFromDT(sprinkPts, out var dtSeg);

            var DTTol = ThSprinklerNetworkService.GetDTLength(dtOrthogonalSeg);

            ThSprinklerNetworkService.FilterTooLongSeg(ref dtOrthogonalSeg, DTTol * 3);

            var netList = CreateSegGroup(dtOrthogonalSeg, dtSeg, sprinkPts, DTTol);

            // ThSprinklerNetGraphService.CreatePartGroup(netList[0], sprinklerParameter.MainPipe, sprinklerParameter.SubMainPipe);

        }

        /// <summary>
        /// 点位按角度分组成图
        /// </summary>
        /// <param name="dtOrthogonalSeg"></param>
        /// <param name="dtSeg"></param>
        /// <param name="pts"></param>
        /// <returns></returns>
        private static List<ThSprinklerNetGroup> CreateSegGroup(List<Line> dtOrthogonalSeg, List<Line> dtSeg, List<Point3d> pts, double DTTol)
        {

            var groupList = ThSprinklerNetworkService.ClassifyOrthogonalSeg(dtOrthogonalSeg);
            for (int i = 0; i < groupList.Count; i++)
            {
                DrawUtils.ShowGeometry(groupList[i].Value, string.Format("l1group{0}-{1}", i, groupList[i].Value.Count), i % 7);
            }

            groupList = ThSprinklerNetworkService.FilterGroupByPt(groupList);
            for (int i = 0; i < groupList.Count; i++)
            {
                DrawUtils.ShowGeometry(groupList.ElementAt(i).Value, string.Format("l2filterGroup{0}-{1}", i, groupList.ElementAt(i).Value.Count), i % 7);
            }


            var netList = ConvertToNet(groupList);
            for (int i = 0; i < netList.Count; i++)
            {
                var net = netList[i];
                for (int j = 0; j < net.ptsGraph.Count; j++)
                {
                    var lines = net.ptsGraph[j].print(net.pts);
                    DrawUtils.ShowGeometry(lines, string.Format("l3graph{0}-{1}", i, j), i % 7);
                }
            }

            ThSprinklerNetworkService.FilterGroupNetByConvexHull(ref netList);
            for (int i = 0; i < netList.Count; i++)
            {
                var net = netList[i];
                for (int j = 0; j < net.ptsGraph.Count; j++)
                {
                    var lines = net.ptsGraph[j].print(net.pts);
                    DrawUtils.ShowGeometry(lines, string.Format("l4filterGraph{0}-{1}", i, j), i % 7);
                }
            }

            var sepaGroupList = SeparateNetByDist(netList, DTTol * 1.5);
            for (int i = 0; i < sepaGroupList.Count; i++)
            {
                DrawUtils.ShowGeometry(sepaGroupList[i].Value, string.Format("l5SepaGroup{0}-{1}", i, sepaGroupList[i].Value.Count), i % 7);
            }





            return netList;


            //////
            //过滤多余组，加没加上的线，还没写完+测试
            //删掉和墙交并的线，成新组还没写
            //////









            //ThSprinklerNetworkService.AddSingleDTLineToGroup(dtSeg, groupList, DTTol * 1.5);
            //for (int i = 0; i < groupList.Count; i++)
            //{
            //    DrawUtils.ShowGeometry(groupList[i].Value, string.Format("l1group{0}-{1}", i, groupList[i].Value.Count), i % 7);
            //}

            //ThSprinklerNetworkService.AddSinglePTToGroup(dtSeg, groupList, pts, DTTol * 1.5);
            //for (int i = 0; i < groupList.Count; i++)
            //{
            //    DrawUtils.ShowGeometry(groupList[i].Value, string.Format("l2group{0}-{1}", i, groupList[i].Value.Count), i % 7);
            //}




        }

        /// <summary>
        /// 组转图
        /// </summary>
        /// <param name="groupList"></param>
        /// <returns></returns>
        private static List<ThSprinklerNetGroup> ConvertToNet(List<KeyValuePair<double, List<Line>>> groupList)
        {
            var netList = new List<ThSprinklerNetGroup>();
            for (int i = 0; i < groupList.Count; i++)
            {
                var net = ThSprinklerNetGraphService.CreateNetwork(groupList[i].Key, groupList[i].Value);
                netList.Add(net);
            }

            return netList;
        }

        /// <summary>
        /// 分离一组里面比较远的组
        /// </summary>
        /// <param name="netList"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        private static List<KeyValuePair<double, List<Line >>> SeparateNetByDist(List<ThSprinklerNetGroup> netList, double tol)

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
