using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ThParkingStall.Core.SParkingPartition.Sparam;

namespace ThParkingStall.Core.SParkingPartition
{
    public class SPartition
    {
        public SPartition()
        {

        }
        public SPartition(List<LineString> walls, List<LineSegment> lanes, List<Polygon> obstacles, Polygon boundary)
        {
            Walls = walls;
            Obstacles = obstacles;
            Boundary = boundary;
            InitParams();
        }
        public List<SLane> Lanes { get; set; }
        public List<SPillar> Pillars { get; set; }
        public List<SCar> Cars { get; set; }
        public List<LineString> Walls { get; set; }
        public List<Polygon> Obstacles { get; set; }
        public Polygon Boundary { get; set; }
        public Polygon BoundingBox { get; set; }
        
        void InitParams()
        {
            BoundingBox = (Polygon)Boundary.Envelope;
            MaxLength = BoundingBox.Length / 2;
        }
    }
}
