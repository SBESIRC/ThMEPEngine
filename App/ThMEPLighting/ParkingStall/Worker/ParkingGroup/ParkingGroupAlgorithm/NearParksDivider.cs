using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPLighting.ParkingStall.Model;

namespace ThMEPLighting.ParkingStall.Worker.ParkingGroup
{
    /// <summary>
    /// 长边和短边的细分器
    /// </summary>
    public class NearParksDivider
    {
        /// <summary>
        /// inner calculate 
        /// </summary>
        class DivideRelatedNode
        {
            public Polyline SrcPolyline;
            public List<DivideRelatedNode> DivideRelatedNodes = new List<DivideRelatedNode>();

            public bool IsUse;

            public DivideRelatedNode(Polyline polyline)
            {
                SrcPolyline = polyline;
                IsUse = false;
            }
        }

        private List<PolylineNode> m_polylineNodes;

        public List<ParkingRelatedGroup> ParkingRelatedGroups
        {
            get;
            set;
        } = new List<ParkingRelatedGroup>();


        public static List<ParkingRelatedGroup> MakeNearParksDivider(NearParksPolylineNode nearParksPolylineNode)
        {
            var nearParksDivider = new NearParksDivider(nearParksPolylineNode);
            nearParksDivider.DoDivider();
            return nearParksDivider.ParkingRelatedGroups;
        }

        public NearParksDivider(NearParksPolylineNode nearParksPolylineNode)
        {
            m_polylineNodes = nearParksPolylineNode.ParksPolylineNodes;
        }


        public void DoDivider()
        {
            var longEdgeRelatedNodes = CollectRelatedPolylineNodes(LineLengthType.LONG_TYPE);
            var longNearParks = GenerateGroupPolylineNode(longEdgeRelatedNodes);

            if (longNearParks.Count > 0)
            {
                ConvertData(longNearParks);
            }
            else
            {
                var shortEdgeRelatedNodes = CollectRelatedPolylineNodes(LineLengthType.SHORT_TYPE);
                var shortNearParks = GenerateGroupPolylineNode(shortEdgeRelatedNodes);
                ConvertData(shortNearParks);
            }
        }

        private void ConvertData(List<NearParks> nearParks)
        {
            foreach (var nearPark in nearParks)
            {
                var srcPolys = nearPark.Polylines;

                var shrinkPolys = new List<Polyline>();
                foreach (var srcPoly in srcPolys)
                {
                    foreach (var entity in srcPoly.Buffer(-ParkingStallCommon.ParkingPolyEnlargeLength))
                    {
                        if (entity is Polyline poly && poly.Closed)
                            shrinkPolys.Add(poly);
                    }
                }

                ParkingRelatedGroups.Add(new ParkingRelatedGroup(shrinkPolys));
            }
        }

        /// <summary>
        /// 收集长边
        /// </summary>
        private void CollectRelatedPolylineNodesBack()
        {
            foreach (var singlePolylineNode in m_polylineNodes)
            {
                foreach (var lineSegment in singlePolylineNode.LineSegments)
                {
                    if (lineSegment.SegmentLineLengthType == LineLengthType.LONG_TYPE)
                    {
                        singlePolylineNode.RelatedPolylineNodes.AddRange(lineSegment.IntersectPolyNodes);
                    }
                }
            }
        }

        private List<DivideRelatedNode> CollectRelatedPolylineNodes2(LineLengthType lineLengthType)
        {
            var divideRelatedNodes = new List<DivideRelatedNode>();
            foreach (var singlePolylineNode in m_polylineNodes)
            {
                var curDivideRelatedNode = new DivideRelatedNode(singlePolylineNode.SrcPoly);
                divideRelatedNodes.Add(curDivideRelatedNode);

                foreach (var lineSegment in singlePolylineNode.LineSegments)
                {
                    if (lineSegment.SegmentLineLengthType == lineLengthType)
                    {
                        foreach (var longTypeRelateNode in lineSegment.IntersectPolyNodes)
                        {
                            curDivideRelatedNode.DivideRelatedNodes.Add(new DivideRelatedNode(longTypeRelateNode.SrcPoly));
                        }
                    }
                }
            }

            return divideRelatedNodes;
        }

        private List<PolylineNode> CollectRelatedPolylineNodes(LineLengthType lineLengthType)
        {
            foreach (var singlePolylineNode in m_polylineNodes)
            {
                singlePolylineNode.RelatedPolylineNodes.Clear();
                foreach (var lineSegment in singlePolylineNode.LineSegments)
                {
                    if (lineSegment.SegmentLineLengthType == lineLengthType)
                    {
                        singlePolylineNode.RelatedPolylineNodes.AddRange(lineSegment.IntersectPolyNodes);
                    }
                }
            }

            return m_polylineNodes;
        }

        private List<NearParks> GenerateGroupPolylineNode(List<PolylineNode> divideRelatedNodes)
        {
            var nearParksGroup = new List<NearParks>();
            for (int i = 0; i < divideRelatedNodes.Count; i++)
            {
                var curRelatedPolyNode = divideRelatedNodes[i];

                if (curRelatedPolyNode.IsUse)
                    continue;

                var nearPark = CollectFromOneNodePolylineNode(curRelatedPolyNode);
                nearParksGroup.Add(nearPark);
            }

            return nearParksGroup;
        }

        private List<NearParks> GenerateGroup(List<DivideRelatedNode> divideRelatedNodes)
        {
            var nearParksGroup = new List<NearParks>();
            for (int i = 0; i < divideRelatedNodes.Count; i++)
            {
                var curRelatedPolyNode = divideRelatedNodes[i];

                if (curRelatedPolyNode.IsUse)
                    continue;

                var nearPark = CollectFromOneNode(curRelatedPolyNode);
                nearParksGroup.Add(nearPark);
            }

            return nearParksGroup;
        }

        private NearParks CollectFromOneNodePolylineNode(PolylineNode relatedNode)
        {
            var relatedNodes = new List<PolylineNode>();
            relatedNodes.Add(relatedNode);

            var relatedPolys = new List<Polyline>();

            while (relatedNodes.Count != 0)
            {
                var curNode = relatedNodes.First();
                relatedNodes.Remove(curNode);

                if (curNode.IsUse)
                    continue;

                curNode.IsUse = true;
                relatedPolys.Add(curNode.SrcPoly);

                foreach (var childNode in curNode.RelatedPolylineNodes)
                {
                    if (childNode.IsUse)
                        continue;

                    relatedNodes.Add(childNode);
                }
            }

            return new NearParks(relatedPolys);
        }

        private NearParks CollectFromOneNode(DivideRelatedNode relatedNode)
        {
            var relatedNodes = new List<DivideRelatedNode>();
            relatedNodes.Add(relatedNode);

            var relatedPolys = new List<Polyline>();

            while (relatedNodes.Count != 0)
            {
                var curNode = relatedNodes.First();
                relatedNodes.Remove(curNode);

                curNode.IsUse = true;
                relatedPolys.Add(curNode.SrcPolyline);

                foreach (var childNode in curNode.DivideRelatedNodes)
                {
                    if (childNode.IsUse)
                        continue;

                    relatedNodes.Add(childNode);
                }
            }

            return new NearParks(relatedPolys);
        }
    }
}
