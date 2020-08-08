using System;
using GeoAPI.Geometries;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using System.Linq;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSRegionExtension
    {
        public static Region Union(this Region pRegion, Region sRegion)
        {
            var pGeometry = pRegion.ToNTSPolygon();
            var sGeometry = sRegion.ToNTSPolygon();
            if (pGeometry == null || sGeometry == null)
            {
                return null;
            }

            // 检查是否相交
            if (!pGeometry.Intersects(sGeometry))
            {
                return null;
            }

            // 若相交，则计算共用部分
            var rGeometry = pGeometry.Union(sGeometry);
            if (rGeometry is IPolygon polygon)
            {
                return polygon.ToDbRegion();
            }

            return null;
        }

        public static Region Intersection(this Region pRegion, Region sRegion)
        {
            var pGeometry = pRegion.ToNTSPolygon();
            var sGeometry = sRegion.ToNTSPolygon();
            if (pGeometry == null || sGeometry == null)
            {
                return null;
            }

            // 检查是否相交
            if (!pGeometry.Intersects(sGeometry))
            {
                return null;
            }

            // 若相交，则计算相交部分
            var rGeometry = pGeometry.Intersection(sGeometry);
            if (rGeometry is IPolygon polygon)
            {
                return polygon.ToDbRegion();
            }

            return null;
        }

        public static List<Polyline> Difference(this Region pRegion, Region sRegion)
        {
            var regions = new List<Polyline>();
            var pGeometry = pRegion.ToNTSPolygon();
            var sGeometry = sRegion.ToNTSPolygon();
            if (pGeometry == null || sGeometry == null)
            {
                return regions;
            }

            // 检查是否相交
            if (!pGeometry.Intersects(sGeometry))
            {
                return regions;
            }

            // 若相交，则计算在pRegion，但不在sRegion的部分
            var rGeometry = pGeometry.Difference(sGeometry);
            if (rGeometry is IPolygon polygon)
            {
                regions.Add(polygon.Shell.ToDbPolyline());
            }
            else if (rGeometry is IMultiPolygon mPolygon)
            {
                regions.AddRange(mPolygon.ToDbPolylines());
            }
            else
            {
                // 为止情况，抛出异常
                throw new NotSupportedException();
            }
            return regions;
        }

        public static IGeometry Intersect(this Region pRegion, Region sRegion)
        {
            var pGeometry = pRegion.ToNTSPolygon();
            var sGeometry = sRegion.ToNTSPolygon();
            if (pGeometry == null || sGeometry == null)
            {
                return null;
            }

            // 检查是否相交
            if (!pGeometry.Intersects(sGeometry))
            {
                return null;
            }

            // 若相交，则计算相交部分
            return pGeometry.Intersection(sGeometry);
        }

        public static List<Polyline> Difference(this Region pRegion, DBObjectCollection sRegions)
        {
            var regions = new List<Polyline>();
            try
            {
                var pGeometry = pRegion.ToNTSPolygon();
                var sGeometry = sRegions.ToNTSPolygons();
                if (pGeometry == null || sGeometry == null)
                {
                    return regions;
                }

                // 检查是否相交
                if (!pGeometry.Intersects(sGeometry))
                {
                    return regions;
                }

                // 若相交，则计算在pRegion，但不在sRegion的部分
                var rGeometry = pGeometry.Difference(sGeometry);
                if (rGeometry is IPolygon polygon)
                {
                    regions.Add(polygon.Shell.ToDbPolyline());
                }
                else if (rGeometry is IMultiPolygon mPolygon)
                {
                    regions.AddRange(mPolygon.ToDbPolylines());
                }
                else
                {
                    // 为止情况，抛出异常
                    throw new NotSupportedException();
                }
            }
            catch
            {
                // 在某些情况下，NTS会抛出异常
                // 这里只捕捉异常，不做特殊的处理
            }
            return regions;
        }

        public static List<Polyline> Differences(this Region pRegion, DBObjectCollection sRegions)
        {
            var pGeometrys = new List<IPolygon>();
            try
            {
                pGeometrys.Add(pRegion.ToNTSPolygon());
                foreach (DBObject sGe in sRegions)
                {
                    var sGeometry = new DBObjectCollection() { sGe }.ToNTSPolygons();
                    foreach (var pGeometry in pGeometrys)
                    {
                        if (pGeometry == null || sGeometry == null)
                        {
                            continue;
                        }

                        // 检查是否相交
                        if (!pGeometry.Intersects(sGeometry))
                        {
                            continue;
                        }

                        // 若相交，则计算在pRegion，但不在sRegion的部分
                        var rGeometry = pGeometry.Difference(sGeometry);
                        if (rGeometry is IPolygon polygon)
                        {
                            pGeometrys = new List<IPolygon>() { polygon };
                        }
                        else if (rGeometry is IMultiPolygon mPolygon)
                        {
                            pGeometrys = new List<IPolygon>();
                            foreach (IPolygon rPolygon in mPolygon.Geometries)
                            {
                                pGeometrys.Add(rPolygon);
                            }
                        }
                        else
                        {
                            // 为止情况，抛出异常
                            throw new NotSupportedException();
                        }
                    }
                }
            }
            catch
            {
                // 在某些情况下，NTS会抛出异常
                // 这里只捕捉异常，不做特殊的处理
            }
            return pGeometrys.Select(x => x.Shell.ToDbPolyline()).ToList();
        }
    }
}
