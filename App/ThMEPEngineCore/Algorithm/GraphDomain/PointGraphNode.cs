using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace ThMEPEngineCore.Algorithm.GraphDomain
{
    public class PointGraphNode : GraphNodeBase
    {
        public PointGraphNode(Point3d point) 
            :this(point,false,1,null,null)
        { }
        public PointGraphNode(Point3d point,bool isEnd)
            : this(point, isEnd, 1, null, null)
        { }
        public PointGraphNode(Point3d point, bool isEnd, double nodeWeight)
            : this(point, isEnd, nodeWeight, null, null)
        { }
        public PointGraphNode(Point3d point, bool isEnd, double nodeWeight, object tag, object graphType)
            :base(point,isEnd,nodeWeight,tag,graphType)
        { }
        public override double NodeDistanceToNode(IGraphNode node)
        {
            if (null == this || this.GraphNode == null || node == null || node.GraphNode ==null)
                return 0.0;
            var point = (Point3d)this.GraphNode;
            var nodePoint = (Point3d)node.GraphNode;
            return point.DistanceTo(nodePoint);
        }
        public override bool NodeIsEqual(IGraphNode node, object precision, object parameter)
        {
            if (null == this || node == null)
                return false;
            double minDis = (double)precision;
            if (this.GraphNode == null || node.GraphNode == null)
                return false;
            Point3d thisPoint = (Point3d)this.GraphNode;
            Point3d otherPoint = (Point3d)node.GraphNode;
            if (null == thisPoint || null == otherPoint)
                return false;
            return thisPoint.DistanceTo(otherPoint) <= minDis;
        }
        public override IGraphNode CenterGraphNode(List<IGraphNode> graphNodes)
        {
            if (null == graphNodes || graphNodes.Count < 1)
                return null;
            var points = graphNodes.Select(c => c.GraphNode).Cast<Point3d>().ToList();
            var centerPoint = ClusterCenter(points);
            return new PointGraphNode(centerPoint);
        }
        Point3d ClusterCenter(List<Point3d> points)
        {
            int countP = 0;
            double sumX = 0;
            double sumY = 0;
            double sumZ = 0;
            foreach (Point3d p in points)
            {
                sumX += p.X;
                sumY += p.Y;
                sumZ += p.Z;
                countP++;
            }
            return new Point3d(sumX / countP, sumY / countP, sumZ / countP);
        }
    }
}
