using NetTopologySuite.Geometries;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThParkingStall.Core.SuperPartition
{
    public class SLane
    {
        public SLane(LineSegment line, Vector2D vec)
        {
            Line = line;
            Vec = vec;
        }
        public LineSegment Line { get; set; }
        public Vector2D Vec { get; set; }
    }
    public class Car
    {

    }
    public class Pillar
    {

    }
}
