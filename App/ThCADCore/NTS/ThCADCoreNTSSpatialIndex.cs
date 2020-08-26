using System;
using System.Linq;
using GeoAPI.Geometries;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using NetTopologySuite.Index.Strtree;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Operation.Predicate;

namespace ThCADCore.NTS
{
    public class ThCADCoreNTSSpatialIndex : IDisposable
    {
        private STRtree<IGeometry> Engine { get; set; }
        private Dictionary<IGeometry, DBObject> Geometries { get; set; }
        public ThCADCoreNTSSpatialIndex(DBObjectCollection objs)
        {
            Engine = new STRtree<IGeometry>();
            Initialize(objs);
        }

        public void Dispose()
        {
            //
        }

        private void Initialize(DBObjectCollection objs)
        {
            Geometries = new Dictionary<IGeometry, DBObject>();
            foreach (Entity obj in objs)
            {
                if (obj is Line line)
                {
                    var geometry = line.ToNTSLineString();
                    if (!Geometries.Keys.Contains(geometry))
                    {
                        Geometries.Add(geometry, line);
                    }
                }
                else if (obj is Polyline polyline)
                {
                    var geometry = polyline.ToNTSLineString();
                    if (!Geometries.Keys.Contains(geometry))
                    {
                        Geometries.Add(geometry, polyline);
                    }
                }
                else if (obj is Circle circle)
                {
                    var geometry = circle.ToNTSPolygon();
                    if (!Geometries.Keys.Contains(geometry))
                    {
                        Geometries.Add(geometry, circle);
                    }
                }
                else if (obj is DBText text)
                {
                    var geometry = text.GeometricExtents.ToNTSPolygon();
                    if (!Geometries.Keys.Contains(geometry))
                    {
                        Geometries.Add(geometry, text);
                    }
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            foreach (var geometry in Geometries.Keys)
            {
                AddGeometry(geometry);
            }
        }

        private void AddGeometry(IGeometry geometry)
        {
            Engine.Insert(geometry.EnvelopeInternal, geometry);
        }

        /// <summary>
        /// Crossing selection
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public DBObjectCollection SelectCrossingPolygon(Polyline polyline)
        {
            var polygon = polyline.ToNTSPolygon();
            var objs = new DBObjectCollection();
            foreach (Polyline item in Query(polygon.EnvelopeInternal))
            {
                if (polygon.Intersects(item.ToNTSLineString()))
                {
                    objs.Add(item);
                }
            }
            return objs;
        }

        /// <summary>
        /// Crossing selection
        /// </summary>
        /// <param name="pt1"></param>
        /// <param name="pt2"></param>
        /// <returns></returns>
        public DBObjectCollection SelectCrossingWindow(Point3d pt1, Point3d pt2)
        {
            var extents = new Extents3d(pt1, pt2);
            var polygon = extents.ToNTSPolygon();
            var objs = new DBObjectCollection();
            foreach (Polyline item in Query(polygon.EnvelopeInternal))
            {
                if (polygon.Intersects(item.ToNTSLineString()))
                {
                    objs.Add(item);
                }
            }
            return objs;
        }

        /// <summary>
        /// Window selection
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public DBObjectCollection SelectWindowPolygon(Polyline polyline)
        {
            var polygon = polyline.ToNTSPolygon();
            var objs = new DBObjectCollection();
            foreach (Polyline item in Query(polygon.EnvelopeInternal))
            {
                if (polygon.Contains(item.ToNTSLineString()))
                {
                    objs.Add(item);
                }
            }
            return objs;
        }

        /// <summary>
        /// Fence Selection
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public DBObjectCollection SelectFence(Polyline polyline)
        {
            return Query(polyline.ToNTSLineString().EnvelopeInternal);
        }

        public DBObjectCollection SelectAll()
        {
            var objs = new DBObjectCollection();
            Geometries.Values.ForEach(o => objs.Add(o));
            return objs;
        }

        private DBObjectCollection Query(Envelope envelope)
        {
            var objs = new DBObjectCollection();
            foreach(var geometry in Engine.Query(envelope))
            {
                if (Geometries.ContainsKey(geometry))
                {
                    objs.Add(Geometries[geometry]);
                }
            }
            return objs;
        }

        /// <summary>
        /// 最近的几个邻居
        /// </summary>
        /// <param name="curve"></param>
        /// <returns></returns>
        public DBObjectCollection NearestNeighbours(Curve curve, int num)
        {
            IGeometry geometry = null;
            if (curve is Line line)
            {
                geometry = line.ToNTSLineString();
            }
            else if (curve is Polyline polyline)
            {
                geometry = polyline.ToNTSLineString();
            }
            else
            {
                throw new NotSupportedException();
            }

            var objs = new DBObjectCollection();
            Engine.NearestNeighbour(
                geometry.EnvelopeInternal,
                geometry,
                new GeometryItemDistance(),
                num)
                .Where(o => !o.EqualsExact(geometry))
                .ForEach(o => objs.Add(Geometries[o]));
            return objs;
        }
    }
}
