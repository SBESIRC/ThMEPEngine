using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThParkingStall.Core.SuperPartition
{
    public class SPartition
    {
        public SPartition(List<LineString> walls, List<LineSegment> lanes, List<Polygon> obstacles, Polygon boundary)
        {
            Walls = walls;
            Obstacles = obstacles;
            Boundary = boundary;
            BoundingBox = (Polygon)boundary.Envelope;
            LaneInitialization laneInitialization = new LaneInitialization(lanes, boundary);
            Lanes = laneInitialization.InitLanes();
            Cars = new List<Car>();
            Pillars = new List<Pillar>();
        }

        #region parameters
        public List<LineString> Walls { get; set; }
        public List<SLane> Lanes { get; set; }
        public List<Polygon> Obstacles { get; set; }
        public Polygon Boundary { get; set; }
        public Polygon BoundingBox { get; set; }
        public List<Car> Cars { get; set; }
        public List<Pillar> Pillars { get; set; }
        #endregion

        public void Process()
        {
            Preprocessing preprocessing = new Preprocessing();
            preprocessing.Execute();
            LaneGenerateion laneGenerateion = new LaneGenerateion();
            laneGenerateion.Execute();
        }

    }
}
