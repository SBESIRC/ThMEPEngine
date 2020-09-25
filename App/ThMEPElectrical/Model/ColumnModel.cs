using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.Model
{
    public class ColumnModel
    {
        public ColumnModel(Polyline polyline)
        {
            columnPoly = polyline;
            columnCenterPt = GetColumCenter(columnPoly);
        }

        public Polyline columnPoly { get; set; }

        public Point3d columnCenterPt { get; }

        public Point3d layoutPoint { get; set; }

        public Vector3d layoutDirection { get; set; }

        public Circle protectRadius { get; set; }

        /// <summary>
        /// 计算柱中点
        /// </summary>
        /// <param name="colums"></param>
        /// <returns></returns>
        private Point3d GetColumCenter(Polyline column)
        {
            List<Point3d> points = new List<Point3d>();
            for (int i = 0; i < column.NumberOfVertices; i++)
            {
                points.Add(column.GetPoint3dAt(i));
            }

            double maxX = points.Max(x => x.X);
            double minX = points.Min(x => x.X);
            double maxY = points.Max(x => x.Y);
            double minY = points.Min(x => x.Y);

            return new Point3d((maxX + minX) / 2, (maxY + minY) / 2, 0);
        }
    }
}
