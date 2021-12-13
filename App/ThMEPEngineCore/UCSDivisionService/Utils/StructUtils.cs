using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.UCSDivisionService.Utils
{
    public static class StructUtils
    {
        /// <summary>
        /// 获得线上所有的点
        /// </summary>
        /// <param name="grid"></param>
        /// <returns></returns>
        public static Point3dCollection GetCurvePoints(List<Curve> grid)
        {
            Point3dCollection pCollection = new Point3dCollection();
            grid.ForEach(x =>
            {
                pCollection.Add(x.StartPoint);
                pCollection.Add(x.EndPoint);
            });

            return pCollection;
        }

        /// <summary>
        /// 获得柱中点
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public static Point3d GetColumnPoint(Polyline column)
        {
            var pts = new List<Point3d>();
            for (int i = 0; i < column.NumberOfVertices; i++)
            {
                pts.Add(column.GetPoint3dAt(i));
            }
            var pt1 = pts[0];
            var pt2 = pts.OrderByDescending(x => x.DistanceTo(pt1)).First();
            return new Point3d((pt1.X + pt2.X) / 2, (pt1.Y + pt2.Y) / 2, 0);
        }
    }
}
