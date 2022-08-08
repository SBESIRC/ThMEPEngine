using System;
using System.Linq;
using ThCADExtension;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Buffer;
using NetTopologySuite.Algorithm.Locate;
using NetTopologySuite.Operation.Overlay;
using NetTopologySuite.Operation.OverlayNG;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using AcPolygon = Autodesk.AutoCAD.DatabaseServices.Polyline;
using NTSJoinStyle = NetTopologySuite.Operation.Buffer.JoinStyle;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSMPolygonExtension
    {
        public static MPolygon ToDbMPolygon(this Polygon polygon)
        {
            if (polygon.IsValid)
            {
                var shell = polygon.Shell.ToDbPolyline();
                if(shell.Area<=1.0) 
                {
                    //防止近似与线的闭合Polyline的问题
                    shell.Dispose();
                    return new MPolygon();
                }

                var holes = polygon.Holes.Select(o=>o.ToDbPolyline()).Where(o=>o.Area>1.0).Cast<Curve>().ToList();

                var mpolygon = ThMPolygonTool.CreateMPolygon(shell, holes);
                shell.Dispose();
                holes.ForEach(h => h.Dispose());
                holes.Clear();

                return mpolygon;
            }
            return new MPolygon();
        }

        public static DBObjectCollection ToDbMPolygonEx(this Polygon polygon, double tolerance = 1.0)
        {
            var objs = new DBObjectCollection();
            var shell = polygon.Shell.ToDbPolyline();
            if (shell.Area <= 1.0)
            {
                //防止近似与线的闭合Polyline的问题
                return objs;
            }
            var holes = new List<Curve>();
            polygon.Holes
                .Select(o => o.ToDbPolyline())
                .Where(o => o.Area > 1.0)
                .ForEach(o =>
                {
                    // 如果洞口和边界几乎一样，忽略此洞口
                    if (o.SimilarityMeasure(shell) > 0.99) 
                    {
                        return;
                    }

                    // 若Hole和Shell的距离很近，拆分成两个区域
                    if (o.Distance(shell) <= tolerance)
                    {
                        objs.Add(o);
                        shell = shell.Difference(o.Buffer(tolerance).OfType<Polyline>().First()).OfType<Polyline>().First();
                    }
                    else
                    {
                        holes.Add(o);
                    }
                });
            if (holes.Count > 0)
            {
                objs.Add(ThMPolygonTool.CreateMPolygon(shell, holes));
            }
            else
            {
                objs.Add(shell);
            }
            return objs;
        }

        public static Geometry ToNTSGeometry(this MPolygon mPolygon)
        {
            var exteriorLoops = new IntegerCollection();
            for (int i = 0; i < mPolygon.NumMPolygonLoops; i++)
            {
                MPolygonLoop loop = mPolygon.GetMPolygonLoopAt(i);
                if (mPolygon.GetLoopDirection(i) == LoopDirection.Exterior)
                {
                    exteriorLoops.Add(i);
                }
            }
            if (exteriorLoops.Count == 1)
            {
                return mPolygon.ToNTSPolygon(exteriorLoops[0]);
            }
            else if (exteriorLoops.Count > 0)
            {
                var ploygons = new List<Polygon>();
                exteriorLoops.Cast<int>().ForEach(i => ploygons.Add(mPolygon.ToNTSPolygon(i)));
                return ThCADCoreNTSService.Instance.GeometryFactory.CreateMultiPolygon(ploygons.ToArray());
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private static Polygon ToNTSPolygon(this MPolygon mPolygon, int loopIndex)
        {
            if (mPolygon.GetLoopDirection(loopIndex) == LoopDirection.Exterior)
            {
                var holes = new List<LinearRing>();
                foreach (int index in mPolygon.GetChildLoops(loopIndex))
                {
                    holes.Add(mPolygon.GetMPolygonLoopAt(index).ToDbPolyline().ToNTSLineString() as LinearRing);
                }
                var shell = mPolygon.GetMPolygonLoopAt(loopIndex).ToDbPolyline().ToNTSLineString() as LinearRing;
                return ThCADCoreNTSService.Instance.GeometryFactory.CreatePolygon(shell, holes.ToArray());
            }
            throw new ArgumentException();
        }

        public static Polygon ToNTSPolygon(this MPolygon mPolygon)
        {
            if(mPolygon==null || mPolygon.Area<=1e-6)
            {
                return ThCADCoreNTSService.Instance.GeometryFactory.CreatePolygon();
            }
            Polyline shell = null;
            List<Polyline> holes = new List<Polyline>();
            for (int i = 0; i < mPolygon.NumMPolygonLoops; i++)
            {
                LoopDirection direction = mPolygon.GetLoopDirection(i);
                MPolygonLoop mPolygonLoop = mPolygon.GetMPolygonLoopAt(i);
                Polyline polyline = mPolygonLoop.ToDbPolyline();
                if (LoopDirection.Exterior == direction)
                {
                    shell = polyline;
                }
                else if (LoopDirection.Interior == direction)
                {
                    holes.Add(polyline);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            if (shell == null && holes.Count == 1)
            {
                return holes[0].ToNTSPolygon();
            }
            else if (shell != null && holes.Count == 0)
            {
                return shell.ToNTSPolygon();
            }
            else if (shell != null && holes.Count > 0)
            {
                if (shell.ToNTSLineString() is LinearRing shellRing)
                {
                    var holeRings = holes.Select(h => h.ToNTSLineString()).OfType<LinearRing>().ToArray();
                    return ThCADCoreNTSService.Instance.GeometryFactory.CreatePolygon(shellRing, holeRings);
                }
                else
                {
                    return ThCADCoreNTSService.Instance.GeometryFactory.CreatePolygon();
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static MPolygon ToDbMPolygon(this MultiPolygon multiPolygon)
        {
            var loops = multiPolygon.Geometries.Cast<Polygon>().Select(o => o.ToDbCollection()).ToList();
            return ThMPolygonTool.CreateMPolygon(loops);
        }

        public static Point3d GetMaximumInscribedCircleCenter(this MPolygon shell)
        {
            return shell.ToNTSPolygon().GetMaximumInscribedCircleCenter();
        }

        public static AcPolygon Outline(this MPolygon mPolygon)
        {
            return mPolygon.ToNTSPolygon().ExteriorRing.ToDbPolyline();
        }

        public static bool Contains(this MPolygon polygon, Point3d pt)
        {
            var locator = new SimplePointInAreaLocator(polygon.ToNTSGeometry());
            return locator.Locate(pt.ToNTSCoordinate()) == Location.Interior;
        }

        public static bool Contains(this MPolygon mPolygon, Curve curve)
        {
            return mPolygon.ToNTSPolygon().Contains(curve.ToNTSGeometry());
        }

        public static bool Contains(this MPolygon mPolygon, MPolygon mPolygon2)
        {
            return mPolygon.ToNTSPolygon().Contains(mPolygon2.ToNTSPolygon());
        }

        public static bool Intersects(this MPolygon mPolygon, Entity entity)
        {
            return mPolygon.ToNTSPolygon().Intersects(entity.ToNTSGeometry());
        }

        public static DBObjectCollection MakeValid(this MPolygon mPolygon,bool keepHoles=false)
        {
            // zero-width buffer trick:
            //  http://lin-ear-th-inking.blogspot.com/2020/12/fixing-buffer-for-fixing-polygons.html
            // self-union trick:
            //  http://lin-ear-th-inking.blogspot.com/2020/06/jts-overlayng-tolerant-topology.html
            return mPolygon.ToNTSPolygon().Buffer(0).ToDbCollection(keepHoles);
        }

        public static DBObjectCollection Buffer(this MPolygon mPolygon, double length,bool keepHoles=false)
        {
            return mPolygon.ToNTSPolygon().Buffer(length, new BufferParameters()
            {
                JoinStyle = NTSJoinStyle.Mitre,
            }).ToDbCollection(keepHoles);
        }

        public static DBObjectCollection Difference(this MPolygon mPolygon, DBObjectCollection objs)
        {
            return mPolygon.ToNTSPolygon().Difference(objs.UnionGeometries()).ToDbCollection();
        }

        public static DBObjectCollection DifferenceMP(this MPolygon mPolygon, DBObjectCollection objs)
        {
            return OverlayNGRobust.Overlay(
                mPolygon.ToNTSPolygon(), 
                objs.UnionGeometries(), 
                SpatialFunction.Difference).ToDbCollection(true);
        }
    }
}
