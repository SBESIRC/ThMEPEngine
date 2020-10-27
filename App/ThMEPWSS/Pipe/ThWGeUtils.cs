using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using ThCADExtension;
using System.Collections.Generic;
using DotNetARX;

namespace ThMEPWSS.Pipe
{
    public class ThWGeUtils
    {

        public static bool Intersects(Polyline frame, Polyline poly1, Polyline poly2)
        {
            var pts = new Point3dCollection();
            poly2.IntersectWith(poly1, Intersect.ExtendArgument, pts, (IntPtr)0, (IntPtr)0);
            return pts.Count > 0;
        }
        public static Polyline Intersects1(Polyline frame, Polyline poly1, Polyline poly2)
        {
            var pts1 = new Point3dCollection();
            var pts2 = new Point3dCollection();
            frame.IntersectWith(poly1, Intersect.ExtendArgument, pts2, (IntPtr)0, (IntPtr)0);
            if (pts2.Count == 0)
            {
                return null;
            }

            // 计算
            double max_dst = 0;
            double max_dst1 = 0;
            var vertices = poly1.Vertices();
            var vertices2 = poly2.Vertices();
            Point3d max_pointa = Point3d.Origin;
            Point3d max_pointb = Point3d.Origin;
            for (int i = 0; i < pts2.Count; i++)
            {
                for (int j = 0; j < vertices.Count; j++)
                {
                    Point3d a = vertices[j];
                    Point3d b = pts2[i];
                    Line poly3 = new Line(a, b);
                    poly2.IntersectWith(poly3, Intersect.ExtendArgument, pts1, (IntPtr)0, (IntPtr)0);
                    double dst = Math.Sqrt((pts2[i].X - vertices2[j].X) * (pts2[i].X - vertices2[j].X) + (pts2[i].Y - vertices2[j].Y) * (pts2[i].Y - vertices2[j].Y));
                    if (pts1.Count <= 0)
                    {
                        if (dst > max_dst)
                        {
                            max_dst = dst;
                            max_pointb = b;
                            max_pointa = a;
                        }
                    }
                    else
                    {
                        double dst1 = Math.Sqrt((pts1[0].X - pts2[i].X) * (pts1[0].X - pts2[i].X) + (pts1[0].Y - pts2[i].Y) * (pts1[0].Y - pts2[i].Y));
                        if (dst1 > max_dst1)
                        {
                            max_dst1 = dst1;
                            max_pointb = b;
                            max_pointa = a;
                        }
                    }
                }
            }

            // 出结果
            var displacement = max_pointa.GetVectorTo(max_pointb);
            return poly1.GetTransformedCopy(Matrix3d.Displacement(displacement)) as Polyline;
        }
    }
}
