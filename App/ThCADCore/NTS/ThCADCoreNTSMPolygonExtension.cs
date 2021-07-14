using System;
using System.Linq;
using ThCADExtension;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Buffer;
using NetTopologySuite.Algorithm.Locate;
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
                var holes = new List<Curve>();
                var shell = polygon.Shell.ToDbPolyline();
                if(shell.Area<=1.0) 
                {
                    //防止近视与线的闭合Polyline的问题
                    return new MPolygon();
                }
                holes = polygon.Holes.Select(o=>o.ToDbPolyline()).Where(o=>o.Area>1.0).Cast<Curve>().ToList();
                return ThMPolygonTool.CreateMPolygon(shell, holes);
            }
            return new MPolygon();
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
                List<LinearRing> holeRings = new List<LinearRing>();
                holes.ForEach(o =>
                {
                    holeRings.Add(o.ToNTSLineString() as LinearRing);
                });
                LinearRing shellLinearRing = shell.ToNTSLineString() as LinearRing;
                return ThCADCoreNTSService.Instance.GeometryFactory.CreatePolygon(shellLinearRing, holeRings.ToArray());
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
        public static DBObjectCollection MakeValid(this MPolygon mPolygon,bool keepHoles=false)
        {
            // zero-width buffer trick:
            //  http://lin-ear-th-inking.blogspot.com/2020/12/fixing-buffer-for-fixing-polygons.html
            // self-union trick:
            //  http://lin-ear-th-inking.blogspot.com/2020/06/jts-overlayng-tolerant-topology.html
            return mPolygon.ToNTSPolygon().Buffer(0).ToDbCollection(keepHoles);
        }

        public static DBObjectCollection Buffer(this MPolygon mPolygon, double length)
        {
            return mPolygon.ToNTSPolygon().Buffer(length, new BufferParameters()
            {
                JoinStyle = NTSJoinStyle.Mitre,
            }).ToDbCollection();
        }
    }
}
