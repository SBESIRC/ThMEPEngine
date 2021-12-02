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
        public static Point3dCollection GetGridPoints(List<Curve> grid)
        {
            Point3dCollection pCollection = new Point3dCollection();
            grid.ForEach(x =>
            {
                pCollection.Add(x.StartPoint);
                pCollection.Add(x.EndPoint);
            });

            return pCollection;
        }

        public static Point3d GetColumnPoint(Polyline column)
        {
            var pt1 = column.GetPoint3dAt(0);
            var pt2 = column.GetPoint3dAt(2);
            return new Point3d((pt1.X + pt2.X) / 2, (pt1.Y + pt2.Y) / 2, 0);
        }
    }
}
