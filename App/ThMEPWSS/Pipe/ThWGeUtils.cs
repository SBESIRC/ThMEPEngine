using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using ThCADExtension;
using System.Collections.Generic;
using DotNetARX;
using AcHelper;
using Autodesk.AutoCAD.EditorInput;
using Linq2Acad;
using ThMEPWSS.Pipe.Model;
using Autodesk.AutoCAD.ApplicationServices;
using GeometryExtensions;

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
        public static Tuple<Point3d, Point3d> SelectPoints()
        {
            return ThMEPWSS.Common.Utils.SelectPoints();
        }
        public static Point3d SelectPoint()
        {
            var point1 = Active.Editor.GetPoint("\n请指定环管标记起点");
            if (point1.Status != PromptStatus.OK)
            {
                return new Point3d();
            }
            var resPt = point1.Value.TransformBy(Active.Editor.UCS2WCS());
            return new Point3d(resPt.X, resPt.Y, 0);
        }
        public static Point3dCollection SelectRange()
        {
            var input = SelectPoints();
            var range = new Point3dCollection();
            range.Add(input.Item1);
            range.Add(new Point3d(input.Item1.X, input.Item2.Y, 0));
            range.Add(input.Item2);
            range.Add(new Point3d(input.Item2.X, input.Item1.Y, 0));
            return range;
        }

        //识别出input范围内，所有的集水井
        public static List<Entity> GetWaterWellEntityList(Tuple<Point3d, Point3d> input, WaterWellIdentifyConfigInfo configInfo)
        {
            List<Entity> waterWellEntityList = new List<Entity>();

            using (var database = AcadDatabase.Active())
            {
                var rst = Active.Editor.SelectCrossingWindow(input.Item1, input.Item2);

                if (rst.Status != PromptStatus.OK)
                    return waterWellEntityList;
                //过滤不需要的元素
                var object_ids = rst.Value.GetObjectIds();
                foreach (ObjectId obj in object_ids)
                {
                    //通过白名单和黑名单过滤空间
                    string identity = "集水井";
                    foreach (string label in configInfo.BlackList)
                    {
                        if (identity.Contains(label))
                        {
                            continue;
                        }
                    }
                    foreach (string label in configInfo.WhiteList)
                    {
                        if (identity.Contains(label))
                        {
                            //将该空间添加到list中
                            break;
                        }
                    }
                }
            }
            return waterWellEntityList;
        }

        public static List<Entity> GetEntityFromDatabase(Tuple<Point3d, Point3d> input,string mark)
        {
            List<Entity> entityList = new List<Entity>();

            using (var database = AcadDatabase.Active())
            {
                var rst = Active.Editor.SelectCrossingWindow(input.Item1, input.Item2);
                if (rst.Status != PromptStatus.OK)
                    return entityList;

                var object_ids = rst.Value.GetObjectIds();
                foreach (ObjectId obj in object_ids)
                {
                    //if(图块名称与mark相同,取出来该图块)

                }
            }

            return entityList;
        }

        public static string GetEffectiveName(Entity entity)
        {
            string effName = "";
            if (entity is BlockReference reference)
            {
                effName = reference.GetEffectiveName();
            }
            return effName;
        }
    }
}
