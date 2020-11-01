using System;
using System.Linq;
using ThCADCore.NTS;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace TianHua.FanSelection.Function
{
    public static class FanParametersExtension
    {
        public static List<Geometry> ToGeometries(this List<FanParameters> models, IEqualityComparer<FanParameters> comparer, string geartypefilter)
        {
            var fanpoints = new Dictionary<string, List<Coordinate>>();
            foreach (var group in models.GroupBy(d => d, comparer))
            {
                var key = MakeGroupKey(group.First());
                fanpoints.Add(key, new List<Coordinate>());
                foreach (var item in group)
                {
                    if (!string.IsNullOrEmpty(item.Gears) && item.Gears == geartypefilter)
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(item.AirVolume) || string.IsNullOrEmpty(item.Pa))
                    {
                        continue;
                    }

                    fanpoints[key].Add(MakeCoordinate(item.AirVolume, item.Pa));
                }
            }
            var typepolylines = new List<Geometry>();
            foreach (var item in fanpoints)
            {
                var coordinates = item.Value.OrderBy(p => p.X).ToArray();
                var geometry = ThCADCoreNTSService.Instance.GeometryFactory.CreateLineString(coordinates);
                geometry.UserData = item.Key;
                typepolylines.Add(geometry);
            }
            return typepolylines;
        }

        private static string MakeGroupKey(FanParameters parameters)
        {
            return string.Format("{0}@{1}", parameters.CCCF_Spec, parameters.Rpm);
        }

        private static Coordinate MakeCoordinate(string  x, string y)
        {
            return new Coordinate(
                ThCADCoreNTSService.Instance.PrecisionModel.MakePrecise(Convert.ToDouble(x)),
                ThCADCoreNTSService.Instance.PrecisionModel.MakePrecise(Convert.ToDouble(y)));
        }

        public static List<Geometry> ToGeometries(this List<AxialFanParameters> models, IEqualityComparer<AxialFanParameters> comparer, string geartypefilter)
        {
            var typepolylines = new List<Geometry>();
            var fanpoints = new Dictionary<string, List<Coordinate>>();
            foreach (var group in models.GroupBy(d => d, comparer))
            {
                var key = group.First().ModelNum;
                fanpoints.Add(key, new List<Coordinate>());
                foreach (var item in group)
                {
                    if (!string.IsNullOrEmpty(item.Gears) && item.Gears == geartypefilter)
                    {
                        continue;
                    }
                    
                    if (string.IsNullOrEmpty(item.AirVolume) || string.IsNullOrEmpty(item.Pa))
                    {
                        continue;
                    }

                    fanpoints[key].Add(MakeCoordinate(item.AirVolume, item.Pa));
                }
            }
            foreach (var item in fanpoints.Where(o => o.Value.Count > 0))
            {
                var coordinates = item.Value.OrderBy(p => p.X).ToArray();
                var geometry = ThCADCoreNTSService.Instance.GeometryFactory.CreateLineString(coordinates);
                geometry.UserData = item.Key;
                typepolylines.Add(geometry);
            }
            return typepolylines;
        }
    }
}
