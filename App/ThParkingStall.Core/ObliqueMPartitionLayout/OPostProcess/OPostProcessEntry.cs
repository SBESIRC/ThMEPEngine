using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.InterProcess;
using ThParkingStall.Core.MPartitionLayout;
using ThParkingStall.Core.OInterProcess;
using static ThParkingStall.Core.MPartitionLayout.MGeoUtilities;

namespace ThParkingStall.Core.ObliqueMPartitionLayout.OPostProcess
{
    public partial class OPostProcessEntry
    {
        public OPostProcessEntry(List<OSubArea> oSubAreas)
        {
            OSubAreas = oSubAreas;
        }
        private List<OSubArea> OSubAreas { get; set; }
        public ObliqueMPartition Execute()
        {
            ObliqueMPartition obliqueMPartition = new ObliqueMPartition();
            Init();
            OGenerateCarsOntheEndofLanesByRemoveUnnecessaryLanes(ref Cars, ref Pillars, ref Lanes, Walls, ObstaclesSpacialIndex, Boundary);
            OGenerateCarsOntheEndofLanesByFillTheEndDistrict(ref Cars, ref Pillars, ref Lanes, Walls, ObstaclesSpacialIndex, Boundary);
            OCheckLayoutDirectionInfoBeforePostProcessEndLanes(ref Cars);
            ORemoveInvalidPillars(ref Pillars, Cars);
            return obliqueMPartition;
        }

        private List<LineString> Walls = new List<LineString>();
        private List<InfoCar> Cars = new List<InfoCar>();
        private List<Polygon> Pillars = new List<Polygon>();
        private List<Polygon> IniPillars = new List<Polygon>();
        private List<Coordinate> ObsVertices = new List<Coordinate>();
        private List<LineSegment> Lanes = new List<LineSegment>();
        private Polygon Boundary { get; set; }
        private MNTSSpatialIndex ObstaclesSpacialIndex { get; set; }

        void Init()
        {
            Boundary = OSubAreas.Count > 0 ? (OSubAreas[0].OutBound.Clone() as Polygon).Simplify() : new Polygon(new LinearRing(new Coordinate[0] { }));
            var obs = new List<Polygon>();
            foreach (var subArea in OSubAreas)
            {
                Walls.AddRange(subArea.obliqueMPartition.Walls);
                Cars.AddRange(subArea.obliqueMPartition.Cars);
                Pillars.AddRange(subArea.obliqueMPartition.Pillars);
                IniPillars.AddRange(subArea.obliqueMPartition.IniPillar);
                ObsVertices.AddRange(subArea.obliqueMPartition.ObstacleVertexes);
                Lanes.AddRange(subArea.obliqueMPartition.IniLanes.Select(e => e.Line));
                obs.AddRange(subArea.Buildings);
            }
            RemoveDuplicatedLines(Lanes);
            ObstaclesSpacialIndex = new MNTSSpatialIndex(obs);
        }
    }
}
