using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.Model
{
    public class WallModel
    {
        public WallModel(Polyline polyline)
        {
            wallPoly = polyline;
            wallCenterPt = GetWallCenter(polyline);
        }

        public Polyline wallPoly { get; set; }

        public Point3d wallCenterPt { get; }

        public Line layoutLine { get; set; }

        /// <summary>
        /// 计算墙几何中点
        /// </summary>
        /// <param name="colums"></param>
        /// <returns></returns>
        private Point3d GetWallCenter(Polyline wall)
        {
            List<Point3d> points = new List<Point3d>();
            for (int i = 0; i < wall.NumberOfVertices; i++)
            {
                points.Add(wall.GetPoint3dAt(i));
            }

            double maxX = points.Max(x => x.X);
            double minX = points.Min(x => x.X);
            double maxY = points.Max(x => x.Y);
            double minY = points.Min(x => x.Y);

            return new Point3d((maxX + minX) / 2, (maxY + minY) / 2, 0);
        }
    }
}
