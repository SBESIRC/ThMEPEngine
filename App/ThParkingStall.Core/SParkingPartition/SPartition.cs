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
            InitLanes(lanes);
        }
        public List<SLane> Lanes { get; set; }
        public List<SPillar> Pillars { get; set; }
        public List<SCar> Cars { get; set; }
        public List<LineString> Walls { get; set; }
        public List<Polygon> Obstacles { get; set; }
        public Polygon Boundary { get; set; }
        public Polygon BoundingBox { get; set; }
        /// <summary>
        /// 根据生成后的车位重新计算的边界
        /// </summary>
        public Polygon ReCaledBoundary { get; set; }

        void InitParams()
        {
            BoundingBox = (Polygon)Boundary.Envelope;
            MaxLength = BoundingBox.Length / 2;
        }
        List<SLane> InitLanes(List<LineSegment> lines)
        {
            Lanes = SLane.ConstructFromLine(lines, Boundary);
            return Lanes;
        }
        public void Dispose()
        {

        }
        public SPartition Clone()
        {
            var spartition = new SPartition();
            return spartition;
        }
        public void Process()
        {
            GenerateParkings();
            ReCalculateBoundary();
        }
        void GenerateParkings()
        {
            SParkingService service = new SParkingService(this);
            service.Process();
        }
        void ReCalculateBoundary()
        {

        }
    }
}
