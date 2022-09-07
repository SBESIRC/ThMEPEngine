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
    public class GlobalBusiness
    {
        public GlobalBusiness(List<OSubArea> oSubAreas)
        {
            OSubAreas = oSubAreas;
        }
        private List<OSubArea> OSubAreas { get; set; }
        public Polygon CalBound()
        {
            var cars = new List<InfoCar>();
            var pillars = new List<Polygon>();
            var lanes = new List<LineSegment>();
            var obstacles = new List<Polygon>();
            foreach (var subArea in OSubAreas)
            {
                cars.AddRange(subArea.obliqueMPartition.Cars);
                pillars.AddRange(subArea.obliqueMPartition.Pillars);
                lanes.AddRange(subArea.obliqueMPartition.IniLanes.Select(e => e.Line));
                obstacles.AddRange(subArea.obliqueMPartition.Obstacles);
            }
            var newbound = MParkingPartitionPro.CalIntegralBound(pillars, lanes, obstacles, cars);
            return newbound;
        }
    }
}
