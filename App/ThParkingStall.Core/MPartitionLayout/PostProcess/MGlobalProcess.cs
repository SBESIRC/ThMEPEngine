using NetTopologySuite.Geometries;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.InterProcess;
using static ThParkingStall.Core.MPartitionLayout.MGeoUtilities;

namespace ThParkingStall.Core.MPartitionLayout
{
    public static partial class MLayoutPostProcessing
    {
        public static void RemoveInvalidPillars(ref List<Polygon> pillars,List<InfoCar>cars)
        {
            List<Polygon> tmps = new List<Polygon>();
            foreach (var t in pillars)
            {
                var clone = t.Clone();
                clone = clone.Scale(0.5);
                if (ClosestPointInCurveInAllowDistance(clone.Envelope.Centroid.Coordinate, cars.Select(e => e.Polyline).ToList(), MParkingPartitionPro.DisPillarLength + MParkingPartitionPro.DisHalfCarToPillar))
                {
                    tmps.Add(t);
                }
            }
            pillars = tmps;
        }
    }
}
