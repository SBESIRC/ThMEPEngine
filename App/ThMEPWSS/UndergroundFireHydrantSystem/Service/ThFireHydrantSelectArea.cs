using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    class ThFireHydrantSelectArea
    {
        public static Point3dCollection CreateArea(Tuple<Point3d, Point3d> tuplePoint)
        {
            var ptNum = new Point3d[5];
            ptNum[0] = tuplePoint.Item1;
            ptNum[2] = tuplePoint.Item2;
            ptNum[4] = tuplePoint.Item1;
            ptNum[1] = new Point3d(ptNum[2].X, ptNum[0].Y, 0);
            ptNum[3] = new Point3d(ptNum[0].X, ptNum[2].Y, 0);

            var ptCollect = new Point3dCollection(ptNum);

            return ptCollect;
        }

        public static Point3dCollection CreateArea(Line line, double len = 200)
        {
            var pt1 = new Point3d();
            var pt2 = new Point3d();

            if (Math.Abs(line.StartPoint.X - line.EndPoint.X) < Math.Abs(line.StartPoint.Y - line.EndPoint.Y))
            {   
                //线是竖着的
                if (line.StartPoint.Y < line.EndPoint.Y)//起点在上面
                {
                    pt1 = new Point3d(line.StartPoint.X - len, line.StartPoint.Y, 0);
                    pt2 = line.EndPoint;
                }
                else//起点在下面
                {
                    pt1 = new Point3d(line.EndPoint.X - len, line.EndPoint.Y, 0);
                    pt2 = line.StartPoint;
                }
            }
            else//线是横着的
            {
                if (line.StartPoint.X < line.EndPoint.X)//起点在左面
                {
                    pt1 = new Point3d(line.StartPoint.X, line.StartPoint.Y + len, 0);
                    pt2 = line.EndPoint;
                }
                else//起点在右面
                {
                    pt1 = new Point3d(line.EndPoint.X, line.EndPoint.Y + len, 0);
                    pt2 = line.StartPoint;
                }
            }
            var tuplePoint = Tuple.Create(pt1, pt2);
            return CreateArea(tuplePoint);
        }
    }
}
