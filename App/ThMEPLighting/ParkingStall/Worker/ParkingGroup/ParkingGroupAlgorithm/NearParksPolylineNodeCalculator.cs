using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPLighting.ParkingStall.Geometry;
using ThMEPLighting.ParkingStall.Model;

namespace ThMEPLighting.ParkingStall.Worker.ParkingGroup
{
    public class NearParksPolylineNodeCalculator
    {
        private List<NearParksPolylineNode> m_nearParksPolylineNodes;
        public List<ParkingRelatedGroup> ParkingRelatedGroups
        {
            get;
            set;
        } = new List<ParkingRelatedGroup>();

        public NearParksPolylineNodeCalculator(List<NearParksPolylineNode> nearParksPolylineNodes)
        {
            m_nearParksPolylineNodes = nearParksPolylineNodes;
        }

        public static List<ParkingRelatedGroup> MakeParkingRelatedGroup(List<NearParksPolylineNode> nearParksPolylineNodes)
        {
            var parkingRelatedGroupCalculator = new NearParksPolylineNodeCalculator(nearParksPolylineNodes);
            parkingRelatedGroupCalculator.Do();
            return parkingRelatedGroupCalculator.ParkingRelatedGroups;
        }

        public void Do()
        {
            foreach (var nearParksPolylineNode in m_nearParksPolylineNodes)
            {
                // 每组细分处理工作
                ParkingRelatedGroups.AddRange(NearParksPolylineNodeIntoParkingRelatedGroup(nearParksPolylineNode));
            }
        }

        /// <summary>
        ///  注意精度损失， 
        /// </summary>
        /// <param name="nearParksPolylineNode"></param>
        /// <returns></returns>
        private List<ParkingRelatedGroup> NearParksPolylineNodeIntoParkingRelatedGroup(NearParksPolylineNode nearParksPolylineNode)
        {
            // 计算关联关系
            CalculatePolylineNodesRelations(nearParksPolylineNode);

            // 细分组
            return DivideNearParksIntoGroups(nearParksPolylineNode);
        }

        private List<ParkingRelatedGroup> DivideNearParksIntoGroups(NearParksPolylineNode nearParksPolylineNode)
        {
            return NearParksDivider.MakeNearParksDivider(nearParksPolylineNode);
        }

        private void CalculatePolylineNodesRelations(NearParksPolylineNode nearParksPolylineNode)
        {
            var polylineNodes = nearParksPolylineNode.ParksPolylineNodes;

            // 计算关联关系
            for (int i = 0; i < polylineNodes.Count; i++)
            {
                var curPolylineNode = polylineNodes[i];

                for (int j = i + 1; j < polylineNodes.Count; j++)
                {
                    var nextPolylineNode = polylineNodes[j];
                    var intersectPoly = IntersectRegionPolyline(curPolylineNode.SrcPoly, nextPolylineNode.SrcPoly);
                    if (intersectPoly == null)
                        continue;

                    // 处理polylineNode 如果和polylineNode 相关联，则计算出polylineNode 和对方的polylineNode中的哪一条边进行关联
                    IntersectParkRelatedNodeCalculator.MakeIntersectParkRelatedNodeCalculator(curPolylineNode, nextPolylineNode, intersectPoly);
                }
            }
        }


        private Polyline IntersectRegionPolyline(Polyline polyFir, Polyline polySec)
        {
            var closedPolys = new List<Polyline>();
            var unclosedPolys = new List<Polyline>();

            foreach (var entity in polyFir.ToNTSPolygon().Intersection(polySec.ToNTSPolygon()).ToDbCollection())
            {
                if (entity is Polyline polyline)
                {
                    if (polyline.Closed)
                        closedPolys.Add(polyline);
                    else
                        unclosedPolys.Add(polyline);
                }
            }

            var closedOrderPolys = closedPolys.OrderBy(p => p.Area).ToList();
            var unclosedOrderPolys = unclosedPolys.OrderBy(p => p.Length).ToList();

            if (closedOrderPolys.Count > 0)
                return closedOrderPolys.Last();

            if (unclosedOrderPolys.Count > 0)
                return unclosedPolys.Last();

            return null;
        }
    }
}
