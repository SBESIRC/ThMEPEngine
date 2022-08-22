using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using GeometryExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHVAC.FloorHeatingCoil
{
    public static class FilletUtils
    {
        /// <summary>
        /// 闭合多段线倒圆角
        /// </summary>
        /// <param name="shell">闭合多段线</param>
        /// <param name="start">起点</param>
        /// <param name="end">终点</param>
        /// <param name="radius">圆角推荐半径</param>
        /// <returns></returns>
        public static Polyline FilletPolyline(Polyline shell, Point3d start, Point3d end, double radius = 80)
        {
            // 点集重排列
            var points = PassageWayUtils.GetPolyPoints(shell);
            points = SmoothUtils.SmoothPoints(points);
            var si = PassageWayUtils.GetPointIndex(start, points);
            var ei = PassageWayUtils.GetPointIndex(end, points);
            if ((ei + 1) % points.Count != si)
            {
                var tmp = si;
                si = ei;
                ei = tmp;
            }
            PassageWayUtils.RearrangePoints(ref points, si);
            // 多段线倒圆角
            var ret = PassageWayUtils.BuildPolyline(points);
            List<double> fillet_radius = new List<double>();
            for (int i = 1; i < ret.NumberOfVertices - 1; i++)
            {
                var pre_len = (ret.GetPoint3dAt(i - 1) - ret.GetPoint3dAt(i)).Length;
                var next_len = (ret.GetPoint3dAt(i + 1) - ret.GetPoint3dAt(i)).Length;
                fillet_radius.Add(Math.Min(radius, Math.Min(pre_len, next_len) / 3));
            }
            for (int i = ret.NumberOfVertices - 2; i >= 1; i--)
                ret.FilletAt(i, fillet_radius[i - 1]);
            return ret;
        }
    }
}
