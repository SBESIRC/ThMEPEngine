﻿using System;
using NFox.Cad;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using NetTopologySuite.Simplify;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Union;
using NetTopologySuite.Operation.Linemerge;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSPolylineExtension
    {
        public static Circle MinimumBoundingCircle(this Polyline polyline)
        {
            var mbc = new MinimumBoundingCircle(polyline.ToNTSLineString());
            return new Circle(mbc.GetCentre().ToAcGePoint3d(), Vector3d.ZAxis, mbc.GetRadius());
        }

        public static Polyline MinimumBoundingBox(this Polyline polyline)
        {
            var geometry = polyline.ToNTSLineString().Envelope;
            if (geometry is LineString lineString)
            {
                return lineString.ToDbPolyline();
            }
            else if (geometry is LinearRing linearRing)
            {
                return linearRing.ToDbPolyline();
            }
            else if (geometry is Polygon polygon)
            {
                return polygon.Shell.ToDbPolyline();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static Polyline GetMinimumRectangle(this Polyline polyline)
        {
            var geom = polyline.ToNTSLineString();
            var rectangle = MinimumDiameter.GetMinimumRectangle(geom);
            if (rectangle is Polygon polygon)
            {
                return polygon.Shell.ToDbPolyline();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static Polyline ConvexHull(this Polyline polyline)
        {
            var convexHull = new ConvexHull(polyline.ToNTSLineString());
            var geometry = convexHull.GetConvexHull();
            if (geometry is Polygon polygon)
            {
                return polygon.Shell.ToDbPolyline();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static Polyline GetOctagonalEnvelope(this Polyline polyline)
        {
            var geometry = OctagonalEnvelope.GetOctagonalEnvelope(polyline.ToNTSLineString());
            if (geometry is Polygon polygon)
            {
                return polygon.Shell.ToDbPolyline();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static bool IsClosed(this Polyline polyline)
        {
            var geometry = polyline.ToNTSLineString() as LineString;
            return geometry.IsClosed;
        }

        public static Polyline Intersect(this Polyline thisPolyline, Polyline polySec)
        {
            var polygonFir = thisPolyline.ToNTSPolygon();

            var polygonSec = polySec.ToNTSPolygon();

            if (polygonFir == null || polygonSec == null)
            {
                return null;
            }

            // 检查是否相交
            if (!polygonFir.Intersects(polygonSec))
            {
                return null;
            }

            // 若相交，则计算相交部分
            var rGeometry = polygonFir.Intersection(polygonSec);
            if (rGeometry is Polygon polygon)
            {
                return polygon.Shell.ToDbPolyline();
            }

            return null;
        }

        /// <summary>
        /// 预处理多段线
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public static DBObjectCollection PreprocessAsLineString(this Polyline polyline)
        {
            // 剔除重复点（在一定公差范围内）
            // 鉴于主要的使用场景是建筑底图，选择1毫米作为公差
            var result = TopologyPreservingSimplifier.Simplify(polyline.ToFixedNTSLineString(), 1.0);

            // 合并线段
            var merger = new LineMerger();
            merger.Add(UnaryUnionOp.Union(result));

            // 返回结果
            var objs = new List<DBObject>();
            merger.GetMergedLineStrings().ForEach(g => objs.AddRange(g.ToDbObjects()));
            return objs.ToCollection<DBObject>();
        }

        /// <summary>
        /// 两个polyline之间的距离
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static double Distance(this Polyline polyline, Polyline other)
        {
            return polyline.ToNTSLineString().Distance(other.ToNTSLineString());
        }
    }
}
