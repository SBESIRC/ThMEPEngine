﻿using System;
using NFox.Cad;
using DotNetARX;
using Linq2Acad;
using System.Linq;
using ThCADExtension;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Geometries.Prepared;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADCore.NTS
{
    /// <summary>
    /// 空间索引DB图元（去除空间重合的DB图元）
    /// </summary>
    public class ThCADCoreNTSSpatialIndex : IDisposable
    {
        private STRtree<Geometry> Engine { get; set; }
        public Dictionary<Geometry, DBObject> Geometries { get; set; }
        public ThCADCoreNTSSpatialIndex(DBObjectCollection objs)
        {
            Geometries = new Dictionary<Geometry, DBObject>();
            Update(objs, new DBObjectCollection());
        }

        public void Dispose()
        {
            //
        }

        private DBObjectCollection CrossingFilter(DBObjectCollection objs, IPreparedGeometry preparedGeometry)
        {
            return objs.Cast<Entity>().Where(o => Intersects(preparedGeometry, o)).ToCollection();
        }

        private DBObjectCollection FenceFilter(DBObjectCollection objs, IPreparedGeometry preparedGeometry)
        {
            return objs.Cast<Entity>().Where(o => Intersects(preparedGeometry, o)).ToCollection();
        }

        private DBObjectCollection WindowFilter(DBObjectCollection objs, IPreparedGeometry preparedGeometry)
        {
            return objs.Cast<Entity>().Where(o => Contains(preparedGeometry, o)).ToCollection();
        }

        private bool Contains(IPreparedGeometry preparedGeometry, Entity entity)
        {
            return preparedGeometry.Contains(ToNTSGeometry(entity));
        }

        private bool Intersects(IPreparedGeometry preparedGeometry, Entity entity)
        {
            return preparedGeometry.Intersects(ToNTSGeometry(entity));
        }

        private Geometry ToNTSGeometry(DBObject obj)
        {
            using (var ov = new ThCADCoreNTSFixedPrecision())
            {
                if (obj is Line line)
                {
                    return line.ToNTSLineString();
                }
                else if (obj is Polyline polyline)
                {
                    return polyline.ToNTSLineString();
                }
                else if (obj is Arc arc)
                {
                    return arc.ToNTSGeometry();
                }
                else if (obj is Circle circle)
                {
                    return circle.ToNTSGeometry();
                }
                else if (obj is MPolygon mPolygon)
                {
                    return mPolygon.ToNTSPolygon();
                }
                else if (obj is Entity entity)
                {
                    try
                    {
                        return entity.GeometricExtents.ToNTSPolygon();
                    }
                    catch
                    {
                        // 若异常抛出，则返回一个“空”的Polygon
                        return ThCADCoreNTSService.Instance.GeometryFactory.CreatePolygon();
                    }
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }

        private Polygon ToNTSPolygon(DBObject obj)
        {
            using (var ov = new ThCADCoreNTSFixedPrecision())
            {
                if (obj is Polyline poly)
                {
                    return poly.ToNTSPolygon();
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }

        /// <summary>
        /// 更新索引
        /// </summary>
        /// <param name="adds"></param>
        /// <param name="removals"></param>
        public void Update(DBObjectCollection adds, DBObjectCollection removals)
        {
            // 添加新的对象
            adds.Cast<DBObject>().ForEachDbObject(o =>
            {
                var geometry = ToNTSGeometry(o);
                if (!Geometries.Keys.Contains(geometry))
                {
                    Geometries.Add(geometry, o);
                }
            });
            // 移除删除对象
            Geometries.RemoveAll((k, v) => removals.Contains(v));

            // 创建新的索引
            Engine = new STRtree<Geometry>();
            Geometries.Keys.ForEach(g => Engine.Insert(g.EnvelopeInternal, g));
        }

        /// <summary>
        /// Crossing selection
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public DBObjectCollection SelectCrossingPolygon(Polyline polyline)
        {
            var geometry = ToNTSPolygon(polyline);
            return CrossingFilter(
                Query(geometry.EnvelopeInternal),
                ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(geometry));
        }

        public DBObjectCollection SelectCrossingPolygon(MPolygon mPolygon)
        {
            /*
             * 线获取MPolygon外圈内所有的物体
             * 减去洞内包括的物体
             */
            var loops = mPolygon.Loops();
            var objs = SelectCrossingPolygon(loops[0]);
            for (int i = 1; i < loops.Count; i++)
            {
                foreach (DBObject innerObj in SelectWindowPolygon(loops[i]))
                {
                    objs.Remove(innerObj);
                }
            }
            return objs;
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
            return SelectCrossingPolygon(extents.ToRectangle());
        }

        /// <summary>
        /// Window selection
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public DBObjectCollection SelectWindowPolygon(Polyline polyline)
        {
            var geometry = ToNTSPolygon(polyline);
            return WindowFilter(Query(geometry.EnvelopeInternal),
                ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(geometry));
        }

        /// <summary>
        /// Fence Selection
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public DBObjectCollection SelectFence(Polyline polyline)
        {
            var geometry = ToNTSGeometry(polyline);
            return FenceFilter(Query(geometry.EnvelopeInternal),
                ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(geometry));
        }

        /// <summary>
        /// Fence Selection
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public DBObjectCollection SelectFence(Line line)
        {
            var geometry = ToNTSGeometry(line);
            return FenceFilter(Query(geometry.EnvelopeInternal),
                ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(geometry));
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

        public void AddTag(DBObject obj, object tag)
        {
            if (Geometries.ContainsValue(obj))
            {
                Geometries.Where(o => o.Value == obj).First().Key.UserData = tag;
            }
        }

        public object Tag(DBObject obj)
        {
            if (!Geometries.ContainsValue(obj))
            {
                return null;
            }
            return Geometries.Where(o => o.Value == obj).First().Key.UserData;
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
            var geometry = ToNTSGeometry(curve);
            var neighbours = Engine.NearestNeighbour(
                geometry.EnvelopeInternal,
                geometry,
                new GeometryItemDistance(),
                num)
                .Where(o => !o.EqualsExact(geometry));
            var objs = new DBObjectCollection();
            foreach (var neighbour in neighbours)
            {
                objs.Add(Geometries[neighbour]);
            }
            return objs;
        }
    }
}
