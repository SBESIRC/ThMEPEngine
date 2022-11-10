using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.Tools;

namespace ThParkingStall.Core.FireZone
{
    public class FireZoneNode//边界也为一个node
    {
        public Polygon polygon;
        public int ObjId = -1;
        private List<Geometry> _Segments = null;
        public List<Geometry> Segments
        {
            get
            {
                if(_Segments == null)
                {
                    _Segments = polygon.Shell.ToLineStrings().OfType<Geometry>().ToList();
                }
                return _Segments;
            }
        }
        public Point Centroid;
        public Coordinate[] Coordinates
        {
            get
            {
                if(polygon == null) return new Coordinate[] {Centroid.Coordinate};
                return polygon.Shell.Coordinates;
            }
        }

        public List<(FireZoneEdge,FireZoneNode)> Branches;//分支

        public int Type;//-1:边界节点，0:普通节点，1:障碍物节点
        public FireZoneNode(Geometry node,  int type = 0)
        {
            if (node is LinearRing ring) polygon = new Polygon(ring);
            else if (node is Polygon p) polygon = p;
            //else polygon = Geometry.DefaultFactory.CreatePolygon();
            Centroid = node.Centroid;
            Type = type;
            Branches = new List<(FireZoneEdge, FireZoneNode)>();
        }
        public void AddBranch(FireZoneEdge edge,FireZoneNode node)
        {
            Branches.Add((edge,node));
        }
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj is FireZoneNode other)
            {
                if(polygon == null) return this.Centroid.Equals(other.Centroid);
                return this.polygon.Equals(other.polygon);
            }
            return false;
        }
        public override int GetHashCode()
        {
            if (polygon == null) return Centroid.GetHashCode();
            return polygon.GetHashCode();
        }
    }
}
