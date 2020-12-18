using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NetTopologySuite.Geometries.Prepared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPLighting.ParkingStall.Model;

namespace ThMEPLighting.ParkingStall.Worker.ParkingGroup
{
    public class ParkingNearGroup
    {
        class RelatedPolyNode
        {
            public Polyline curPoly;
            public List<RelatedPolyNode> RelatedNodes = new List<RelatedPolyNode>();
            public bool IsUse;

            public int Index;

            public RelatedPolyNode(Polyline poly, int index)
            {
                Index = index;
                curPoly = poly;
                IsUse = false;
            }

            public void AddNode(RelatedPolyNode addNode)
            {
                RelatedNodes.Add(addNode);
            }
        }

        private List<Polyline> m_polylines;

        private List<RelatedPolyNode> m_relatedPolyNodes = new List<RelatedPolyNode>();

        public List<NearParks> NearParksGroup
        {
            get;
            set;
        } = new List<NearParks>();
        
        public static List<NearParks> MakeParkingNearGroup(List<Polyline> polylines)
        {
            var parkingNearGroup = new ParkingNearGroup(polylines);
            parkingNearGroup.DoRelatedGroup();
            return parkingNearGroup.NearParksGroup;
        }

        public ParkingNearGroup(List<Polyline> polylines)
        {
            m_polylines = polylines;
        }

        public void DoRelatedGroup()
        {
            CalculateRelatedPolyNodes();
            GenerateGroup();
        }

        private void GenerateGroup()
        {
            for (int i = 0; i < m_relatedPolyNodes.Count; i++)
            {
                var curRelatedPolyNode = m_relatedPolyNodes[i];
                if (curRelatedPolyNode.IsUse)
                    continue;

                CollectFromOneNode(curRelatedPolyNode);
            }
        }

        private void CollectFromOneNode(RelatedPolyNode relatedPolyNode)
        {
            var relatedNodes = new List<RelatedPolyNode>();
            relatedNodes.Add(relatedPolyNode);

            var relatedPolys = new List<Polyline>();

            while (relatedNodes.Count != 0)
            {
                var curNode = relatedNodes.First();
                relatedNodes.Remove(curNode);

                if (curNode.IsUse)
                    continue;

                curNode.IsUse = true;
                relatedPolys.Add(curNode.curPoly);

                foreach (var childNode in curNode.RelatedNodes)
                {
                    if (childNode.IsUse)
                        continue;

                    relatedNodes.Add(childNode);
                }
            }

            NearParksGroup.Add(new NearParks(relatedPolys));
        }

        private void CalculateRelatedPolyNodes()
        {
            for (int i = 0; i < m_polylines.Count; i++)
            {
                m_relatedPolyNodes.Add(new RelatedPolyNode(m_polylines[i], i));
            }

            for (int i = 0; i < m_relatedPolyNodes.Count; i++)
            {
                var curPolyNode = m_relatedPolyNodes[i];
                var curPoly = curPolyNode.curPoly;
                var preparedGeometry = ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(curPoly.ToNTSGeometry());

                for (int j = i + 1; j < m_relatedPolyNodes.Count; j++)
                {
                    var nextPolyNode = m_relatedPolyNodes[j];
                    var nextPoly = nextPolyNode.curPoly;

                    //var pointCollection = new Point3dCollection();

                    //var diffPoly = curPoly.Intersect(nextPoly);
                    //// test
                    //curPoly.IntersectWith(nextPoly, Intersect.OnBothOperands, pointCollection, (IntPtr)0, (IntPtr)0);

                    if (IsGeomInfoNear(preparedGeometry, nextPoly))
                    {
                        curPolyNode.AddNode(nextPolyNode);
                        nextPolyNode.AddNode(curPolyNode);
                    }
                }
            }
        }

        private bool IsGeomInfoNear(IPreparedGeometry preparedGeometry, Polyline secPoly)
        {
            if (preparedGeometry.Intersects(secPoly.ToNTSGeometry()))
                return true;

            return false;
        }
    }
}
