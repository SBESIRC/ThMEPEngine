using System;
using System.Linq;
using ThCADCore.NTS;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using NetTopologySuite.LinearReferencing;

namespace TianHua.FanSelection.Function
{
    public static class ThFanSelectionModelPick
    {
        public static bool IsOptimalModel(this Geometry model, Point point)
        {
            return model.Envelope.Contains(point);
        }

        public static List<Point> ReferenceModelPoint(this Geometry model, List<double> point, Geometry refModel)
        {
            var coordinate = new Coordinate(
                ThCADCoreNTSService.Instance.PrecisionModel.MakePrecise(point[0]),
                ThCADCoreNTSService.Instance.PrecisionModel.MakePrecise(point[1])
                );
            Point Point = ThCADCoreNTSService.Instance.GeometryFactory.CreatePoint(coordinate);

            var refPoints = new List<Point>();
            var locator = new LocationIndexedLine(model);
            var refLocator = new LocationIndexedLine(refModel);
            foreach (var modelPoint in model.IntersectionPoint(Point))
            {
                var location = locator.IndexOf(modelPoint.Coordinate);
                var refModelPoint = refLocator.ExtractPoint(location);
                refPoints.Add(ThCADCoreNTSService.Instance.GeometryFactory.CreatePoint(refModelPoint));
            }
            return refPoints;
        }

        public static Dictionary<Geometry, Point> ModelPick(this List<Geometry> models, Point point)
        {
            var points = new List<Point>();
            foreach (var model in models)
            {
                points.AddRange(model.IntersectionPoint(point));
            }

            // 寻找探测点上方最近的交点作为热点
            var pickedModel = new Dictionary<Geometry, Point>();
            var filterPoints = points.Where(o => o.Y >= point.Y).OrderBy(o => o.Y);
            if (filterPoints.Any())
            {
                // 寻找穿过热点的模型线
                var hotspot = filterPoints.First();
                foreach (var model in models)
                {
                    if (model.Distance(hotspot) < 1E-10)
                    {
                        if (!model.IsEmpty)
                        {
                            pickedModel.Add(model, model.GetClosestVertexTo(hotspot));
                        }
                    }
                }
            }
            return pickedModel;
        }
        private static List<Point> IntersectionPoint(this Geometry model, Point point)
        {
            var points = new List<Point>();

            // 先取信封
            var envelope = model.EnvelopeInternal;
            // 计算点和信封的交线
            if (point.X <= envelope.MaxX &&
                point.X >= envelope.MinX &&
                point.Y <= envelope.MaxY)
            {
                var coordinates = new List<Coordinate>()
                    {
                        new Coordinate(
                            ThCADCoreNTSService.Instance.PrecisionModel.MakePrecise(point.X),
                            ThCADCoreNTSService.Instance.PrecisionModel.MakePrecise(envelope.MinY)),
                        new Coordinate(
                            ThCADCoreNTSService.Instance.PrecisionModel.MakePrecise(point.X),
                            ThCADCoreNTSService.Instance.PrecisionModel.MakePrecise(envelope.MaxY)),

                    };
                // 计算模型线和探测线的交点
                var intersectPoints = model.Intersection(ThCADCoreNTSService.Instance.GeometryFactory.CreateLineString(coordinates.ToArray()));
                if (intersectPoints is Point pt)
                {
                    points.Add(pt);
                }
                else if (intersectPoints is MultiPoint pts)
                {
                    foreach (Point po in pts.Geometries)
                    {
                        points.Add(po);
                    }
                }
                else
                {
                    throw new NotSupportedException();
                }
            }

            return points;
        }

        private static Point GetClosestVertexTo(this Geometry geometry, Point point)
        {
            var line = new LocationIndexedLine(geometry);
            var location = line.Project(point.Coordinate);
            var segment = location.GetSegment(geometry);
            if (segment.P0.X >= point.X)
            {
                return ThCADCoreNTSService.Instance.GeometryFactory.CreatePoint(segment.P0);
            }
            else if (segment.P1.X >= point.X)
            {
                return ThCADCoreNTSService.Instance.GeometryFactory.CreatePoint(segment.P1);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
