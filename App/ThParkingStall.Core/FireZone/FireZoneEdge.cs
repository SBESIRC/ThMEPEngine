using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThParkingStall.Core.FireZone
{
    public class FireZoneEdge
    {
        public LineString Path;
        public Coordinate P0 { get { return Path.StartPoint.Coordinate; } }
        public Coordinate P1 { get { return Path.EndPoint.Coordinate; } }
        public int ObjId = -1;
        public double Cost;
        public FireZoneEdge(LineString path, double cost = -1)
        {
            Path = path;
            if (cost < 0) Cost = path.Length;
            else Cost = cost;
        }
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj is FireZoneEdge other)
            {
                return this.Path.Equals(other.Path);
            }
            return false;
        }
        public override int GetHashCode()
        {
            return Path.GetHashCode();
        }
    }
}
