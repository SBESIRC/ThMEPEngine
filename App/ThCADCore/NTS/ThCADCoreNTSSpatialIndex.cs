﻿using System;
using DotNetARX;
using System.Linq;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Geometries.Prepared;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADCore.NTS
{
    public class ThCADCoreNTSSpatialIndex : IDisposable
    {
        private STRtree<Geometry> Engine { get; set; }
        private PreparedGeometryFactory Factory { get; set; }
        private Dictionary<Geometry, DBObject> Geometries { get; set; }
        public ThCADCoreNTSSpatialIndex(DBObjectCollection objs)
        {
            Engine = new STRtree<Geometry>();
            Factory = new PreparedGeometryFactory();
            Initialize(objs);
        }

        public void Dispose()
        {
            //
        }

        private void Initialize(DBObjectCollection objs)
        {
            Geometries = new Dictionary<Geometry, DBObject>();
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
                else if (obj is Arc arc)
                {
                    var geometry = arc.GeometricExtents.ToNTSPolygon();
                    if (!Geometries.Keys.Contains(geometry))
                    {
                        Geometries.Add(geometry, arc);
                    }
                }
                else if (obj is Circle circle)
                {
                    var geometry = circle.GeometricExtents.ToNTSPolygon();
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
                else if (obj is Hatch hatch)
                {
                    var geometry = hatch.GeometricExtents.ToNTSPolygon();
                    if (!Geometries.Keys.Contains(geometry))
                    {
                        Geometries.Add(geometry, hatch);
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

        private void AddGeometry(Geometry geometry)
        {
            Engine.Insert(geometry.EnvelopeInternal, geometry);
        }

        private DBObjectCollection CrossingFilter(DBObjectCollection objs, IPreparedGeometry preparedGeometry)
        {
            var results = new DBObjectCollection();
            foreach (Entity item in objs)
            {
                if (item is Line line)
                {
                    if (preparedGeometry.Intersects(line.ToNTSLineString()))
                    {
                        results.Add(item);
                    }
                }
                else if (item is Polyline polyline)
                {
                    if (preparedGeometry.Intersects(polyline.ToNTSLineString()))
                    {
                        results.Add(item);
                    }
                }
                else if (item is DBText dBText)
                {
                    if (preparedGeometry.Intersects(dBText.GeometricExtents.ToNTSPolygon()))
                    {
                        results.Add(item);
                    }
                }
                else if (item is Arc arc)
                {
                    if (preparedGeometry.Intersects(arc.GeometricExtents.ToNTSPolygon()))
                    {
                        results.Add(item);
                    }
                }
                else if (item is Circle circle)
                {
                    if (preparedGeometry.Intersects(circle.GeometricExtents.ToNTSPolygon()))
                    {
                        results.Add(item);
                    }
                }
                else if (item is Hatch hatch)
                {
                    if (preparedGeometry.Intersects(hatch.GeometricExtents.ToNTSPolygon()))
                    {
                        results.Add(item);
                    }
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            return results;
        }

        private DBObjectCollection WindowFilter(DBObjectCollection objs, IPreparedGeometry preparedGeometry)
        {
            var results = new DBObjectCollection();
            foreach (Entity item in objs)
            {
                if (item is Line line)
                {
                    if (preparedGeometry.Contains(line.ToNTSLineString()))
                    {
                        results.Add(item);
                    }
                }
                else if (item is Polyline polyline)
                {
                    if (preparedGeometry.Contains(polyline.ToNTSLineString()))
                    {
                        results.Add(item);
                    }
                }
                else if (item is DBText dBText)
                {
                    if (preparedGeometry.Contains(dBText.GeometricExtents.ToNTSPolygon()))
                    {
                        results.Add(item);
                    }
                }
                else if (item is Arc arc)
                {
                    if (preparedGeometry.Contains(arc.GeometricExtents.ToNTSPolygon()))
                    {
                        results.Add(item);
                    }
                }
                else if (item is Circle circle)
                {
                    if (preparedGeometry.Contains(circle.GeometricExtents.ToNTSPolygon()))
                    {
                        results.Add(item);
                    }
                }
                else if (item is Hatch hatch)
                {
                    if (preparedGeometry.Contains(item.GeometricExtents.ToNTSPolygon()))
                    {
                        results.Add(item);
                    }
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            return results;
        }

        private DBObjectCollection FenceFilter(DBObjectCollection objs, IPreparedGeometry preparedGeometry)
        {
            var results = new DBObjectCollection();
            foreach (Entity item in objs)
            {
                if (item is Line line)
                {
                    if (preparedGeometry.Intersects(line.ToNTSLineString()))
                    {
                        results.Add(item);
                    }
                }
                else if (item is Polyline polyline)
                {
                    if (preparedGeometry.Intersects(polyline.ToNTSLineString()))
                    {
                        results.Add(item);
                    }
                }
                else if (item is DBText dBText)
                {
                    if (preparedGeometry.Intersects(dBText.GeometricExtents.ToNTSPolygon()))
                    {
                        results.Add(item);
                    }
                }
                else if (item is Hatch hatch)
                {
                    if (preparedGeometry.Intersects(item.GeometricExtents.ToNTSPolygon()))
                    {
                        results.Add(item);
                    }
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            return results;
        }

        /// <summary>
        /// Crossing selection
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public DBObjectCollection SelectCrossingPolygon(Polyline polyline)
        {
            var geometry = polyline.ToNTSPolygon();
            return CrossingFilter(Query(geometry.EnvelopeInternal), Factory.Create(geometry));
        }

        public DBObjectCollection SelectCrossingPolygon(Point3dCollection polygon)
        {
            var pline = new Polyline()
            {
                Closed = true,
            };
            pline.CreatePolyline(polygon);
            return SelectCrossingPolygon(pline);
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
            var geometry = extents.ToNTSPolygon();
            return CrossingFilter(Query(geometry.EnvelopeInternal), Factory.Create(geometry));
        }

        /// <summary>
        /// Window selection
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public DBObjectCollection SelectWindowPolygon(Polyline polyline)
        {
            var geometry = polyline.ToNTSPolygon();
            return WindowFilter(Query(geometry.EnvelopeInternal), Factory.Create(geometry));
        }

        /// <summary>
        /// Fence Selection
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public DBObjectCollection SelectFence(Polyline polyline)
        {
            var geometry = polyline.ToNTSLineString();
            return FenceFilter(Query(geometry.EnvelopeInternal), Factory.Create(geometry));
        }

        public DBObjectCollection SelectAll()
        {
            var objs = new DBObjectCollection();
            foreach (var item in Geometries.Values)
            {
                objs.Add(item);
            }
            return objs;
        }

        private DBObjectCollection Query(Envelope envelope)
        {
            var objs = new DBObjectCollection();
            foreach (var geometry in Engine.Query(envelope))
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
            Geometry geometry = null;
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
            var neighbours = Engine.NearestNeighbour(
                geometry.EnvelopeInternal,
                geometry,
                new GeometryItemDistance(),
                num)
                .Where(o => !o.EqualsExact(geometry));
            foreach (var neighbour in neighbours)
            {
                objs.Add(Geometries[neighbour]);
            }
            return objs;
        }
    }
}
