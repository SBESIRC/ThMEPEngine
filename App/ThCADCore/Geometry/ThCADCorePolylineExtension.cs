using ThCADCore.NTS;
using GeoAPI.Geometries;
using NetTopologySuite.Simplify;
using NetTopologySuite.Operation.Union;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Operation.Linemerge;

namespace ThCADCore.Geometry
{
    public static class ThCADCorePolylineExtension
    {
        /// <summary>
        /// 预处理多段线
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public static DBObjectCollection Preprocess(this Polyline polyline)
        {
            // 剔除重复点（在一定公差范围内）
            // 鉴于主要的使用场景是建筑底图，选择1毫米作为公差
            var result = TopologyPreservingSimplifier.Simplify(polyline.ToNTSLineStringEx(), 1.0);

            // 合并线段
            var merger = new LineMerger();
            merger.Add(UnaryUnionOp.Union(result));

            // 返回结果
            return merger.GetMergedLineStrings().ToDBCollection();
        }

        /// <summary>
        /// 预处理封闭多段线
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public static DBObjectCollection PreprocessEx(this Polyline polyline)
        {
            // 剔除重复点（在一定公差范围内）
            // 鉴于主要的使用场景是建筑底图，选择1毫米作为公差
            var result = TopologyPreservingSimplifier.Simplify(polyline.ToNTSLineStringEx(), 1.0);

            // 自相交处理
            var polygons = result.Polygonize();

            // 返回结果
            var objs = new DBObjectCollection();
            foreach (IPolygon polygon in polygons)
            {
                objs.Add(polygon.Shell.ToDbPolyline());
            }
            return objs;
        }
    }
}
