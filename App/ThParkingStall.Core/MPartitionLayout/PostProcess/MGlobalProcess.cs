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
        public static void CheckLayoutDirectionInfoBeforePostProcessEndLanes(ref List<InfoCar> cars)
        {
            var vertcars = cars.Where(e => e.CarLayoutMode == 2).ToList();
            var cars_poly = cars.Select(e => e.Polyline).ToList();
            var car_spacial_index = new MNTSSpatialIndex(cars_poly);
            for (int i = 0; i < vertcars.Count; i++)
            {
                var car = vertcars[i].Polyline;
                var edge = car.GetEdges().OrderBy(e => e.Length).Take(2).OrderBy(e => e.MidPoint.Distance(vertcars[i].Point)).First();
                var car_transform = PolyFromLines(edge, edge.Translation(vertcars[i].Vector.Normalize() * ((MParkingPartitionPro.DisVertCarLength-MParkingPartitionPro.DisVertCarLengthBackBack)*2+ MParkingPartitionPro.DisVertCarLengthBackBack)));
                var crossed = car_spacial_index.SelectCrossingGeometry(car_transform.Scale(MParkingPartitionPro.ScareFactorForCollisionCheck));
                if (crossed.Count() < 2)
                {
                    var index=cars_poly.IndexOf(car);
                    if (index >= 0)
                    {
                        cars[index].Polyline = car_transform;
                        cars[index].CarLayoutMode = 0;
                    }
                }
            }
        }
    }
}
