using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.CAD;
using NFox.Cad;
using Linq2Acad;
using ThMEPEngineCore.Diagnostics;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Algorithm;

using ThMEPEngineCore.Model.Hvac;
using ThMEPHVAC.FloorHeatingCoil.Heating;

namespace ThMEPHVAC.FloorHeatingCoil.Heating
{
    class PolylineProcessService
    {
        //基础Polyline操作 
        public static Polyline CreateBoundary(Point3d center, double shortSide, double longSide, Vector3d dir)
        {
            var tol = new Tolerance(10, 10);
            var shortDir = dir.GetNormal();
            var longDir = new Vector3d(-shortDir.Y, shortDir.X, 0); //270

            var pt1 = center + 0.5 * shortSide * shortDir + 0.5 * longDir * longSide;  //左上
            var pt2 = pt1 - shortDir * shortSide; //左下
            var pt3 = pt2 - longDir * longSide;   //右下
            var pt4 = pt3 + shortDir * shortSide;  //右上
            var boundary = new Polyline();
            boundary.Closed = true;

            boundary.AddVertexAt(0, pt4.ToPoint2D(), 0, 0, 0);
            boundary.AddVertexAt(1, pt1.ToPoint2D(), 0, 0, 0);
            boundary.AddVertexAt(2, pt2.ToPoint2D(), 0, 0, 0);
            boundary.AddVertexAt(3, pt3.ToPoint2D(), 0, 0, 0);

            return boundary;
        }

        public static void GetRecInfo(Polyline rec, ref Point3d center, ref Vector3d ShortDir, ref Vector3d LongSide, ref Vector3d ShortSide)
        {
            var tol = new Tolerance(10, 10);
            Point3d pt1 = rec.GetPoint3dAt(0);
            Point3d pt2 = rec.GetPoint3dAt(1);
            Point3d pt3 = rec.GetPoint3dAt(2);

            //Vector3d longSide = new Vector3d();
            //Vector3d shortSide = new Vector3d();
            Vector3d vec1 = pt2 - pt1;
            Vector3d vec2 = pt3 - pt2;
            center = pt1 + 0.5 * vec1 + 0.5 * vec2;

            if (vec1.Length > vec2.Length)
            {
                LongSide = vec1;
                ShortSide = vec2;

            }
            else
            {
                LongSide = vec2;
                ShortSide = vec1;
            }

            ShortDir = ShortSide.GetNormal();
        }

        //单侧延伸
        public static Polyline CreateRectangle2(Point3d pt0, Point3d pt1, double length)
        {
            Vector3d dir = pt1 - pt0;
            Vector3d clockwise270 = new Vector3d(-dir.Y, dir.X, dir.Z).GetNormal();

            Point3d pt2 = pt1 + clockwise270 * length;
            Point3d pt3 = pt0 + clockwise270 * length;

            var boundary = new Polyline();
            boundary.Closed = true;
            boundary.AddVertexAt(0, pt0.ToPoint2D(), 0, 0, 0);
            boundary.AddVertexAt(1, pt1.ToPoint2D(), 0, 0, 0);
            boundary.AddVertexAt(2, pt2.ToPoint2D(), 0, 0, 0);
            boundary.AddVertexAt(3, pt3.ToPoint2D(), 0, 0, 0);

            return boundary;
        }

        //双侧延伸
        public static Polyline CreateRectangle3(Point3d pt0, Point3d pt1, double length, double length2)
        {
            //左侧length，右侧length2
            Vector3d dir = pt1 - pt0;
            Vector3d clockwise270 = new Vector3d(-dir.Y, dir.X, dir.Z).GetNormal();

            Point3d pt2 = pt1 + clockwise270 * length;
            Point3d pt3 = pt0 + clockwise270 * length;
            Point3d pt4 = pt0 - clockwise270 * length2;
            Point3d pt5 = pt1 - clockwise270 * length2;


            var boundary = new Polyline();
            boundary.Closed = true;
            boundary.AddVertexAt(0, pt4.ToPoint2D(), 0, 0, 0);
            boundary.AddVertexAt(1, pt5.ToPoint2D(), 0, 0, 0);
            boundary.AddVertexAt(2, pt2.ToPoint2D(), 0, 0, 0);
            boundary.AddVertexAt(3, pt3.ToPoint2D(), 0, 0, 0);

            return boundary;
        }

        //清理polyline
        static public Polyline PlRegularization2(Polyline originPl, double value)
        {
            //Polyline ClearedPl = OriginPl.Clone() as Polyline;
            var originalArea = originPl.Area;
            Polyline ClearedPl = originPl.Buffer(-value / 2).OfType<Polyline>().ToList().FindByMax(x => x.Area);
            DrawUtils.ShowGeometry(ClearedPl, "l1bBufferedClearedPl", 4, lineWeightNum: 30);

            Polyline newClearedPl = ClearedPl.Buffer(value / 2).OfType<Polyline>().ToList().FindByMax(x => x.Area);
            var newArea = newClearedPl.Area;

            var proportion = newArea / originalArea;

            if (proportion < 0.8)
            {
                int i = 0;
            }

            if (!newClearedPl.IsCCW())
            {
                newClearedPl.ReverseCurve();
            }

            List<int> deleteList = new List<int>();
            int num = newClearedPl.NumberOfVertices;

            //清理Polyline 
            ClearPolyline(ref newClearedPl);

            //第三筛 筛除外凸耳朵
            deleteList = new List<int>();
            num = newClearedPl.NumberOfVertices;
            for (int i = 0; i < num;)
            {
                Point3d pt1 = newClearedPl.GetPoint3dAt(i);
                var pt0 = newClearedPl.GetPoint3dAt((i + num - 1) % num);
                var pt2 = newClearedPl.GetPoint3dAt((i + 1) % num);
                var pt3 = newClearedPl.GetPoint3dAt((i + 2) % num);
                var ptf1 = newClearedPl.GetPoint3dAt((i + num - 2) % num);

                Vector3d line1 = pt1 - pt0;
                Vector3d line2 = pt2 - pt1;
                Vector3d line3 = pt3 - pt2;

                Vector3d line0 = pt0 - ptf1;

                double dProduct = line1.DotProduct(line3);

                double angle = line0.GetAngleTo(line1, Vector3d.ZAxis);

                if (angle < 1.5 * Math.PI + 0.1 && angle > 1.5 * Math.PI - 0.1)
                {
                    if (line1.Length < value && line3.Length < value && dProduct < 0)
                    {
                        if (Math.Abs(line1.Length - line3.Length) < 10)
                        {
                            deleteList.Add(i);
                            deleteList.Add((i + num - 1) % num);
                            deleteList.Add((i + 1) % num);
                            deleteList.Add((i + 2) % num);
                            i = i + 3;
                            continue;
                        }
                    }
                }
                i++;
            }
            for (int i = num - 1; i >= 0; i--)
            {
                if (deleteList.Contains(i))
                {
                    newClearedPl.RemoveVertexAt(i);
                }
            }

            //第四筛
            deleteList.Clear();
            num = newClearedPl.NumberOfVertices;
            Dictionary<int, Point3d> insertMap = new Dictionary<int, Point3d>();
            for (int i = 0; i < num;)
            {
                Point3d pt0 = newClearedPl.GetPoint3dAt(i);
                var pt1 = newClearedPl.GetPoint3dAt((i + 1) % num);
                var pt2 = newClearedPl.GetPoint3dAt((i + 2) % num);
                var pt3 = newClearedPl.GetPoint3dAt((i + 3) % num);
                var pt4 = newClearedPl.GetPoint3dAt((i + 4) % num);

                var pt5 = newClearedPl.GetPoint3dAt((i + 5) % num);
                var ptf1 = newClearedPl.GetPoint3dAt((i + num - 1) % num);

                Vector3d vec1 = pt1 - pt0;
                Vector3d vec2 = pt2 - pt1;
                Vector3d vec3 = pt3 - pt2;
                Vector3d vec4 = pt4 - pt3;

                int find = 0;

                if (vec2.Length < value && vec3.Length < value)
                {
                    double angle = vec2.GetAngleTo(vec3, Vector3d.ZAxis);
                    if (angle < 1.5 * Math.PI + 0.1 && angle > 1.5 * Math.PI - 0.1)
                    {
                        if (vec1.Length > vec4.Length)
                        {
                            Line lineStart = new Line(pt1, pt1 + vec2.GetNormal() * Parameter.ClearExtendLength);
                            Vector3d endVec = pt5 - pt4;
                            Line lineEnd = new Line(pt4 - endVec.GetNormal() * value * 2, pt5 + endVec.GetNormal() * value * 2);
                            List<Point3d> intersectionList = lineStart.Intersect(lineEnd, Intersect.OnBothOperands);
                            if (intersectionList.Count > 0)
                            {
                                Point3d intersectionPoint = intersectionList.FindByMin(x => x.DistanceTo(pt1));

                                deleteList.Add((i + 2) % num);
                                deleteList.Add((i + 3) % num);
                                deleteList.Add((i + 4) % num);

                                insertMap.Add(i + 2, intersectionPoint);
                                find = 1;
                            }
                        }
                        else   //vec1.Length < vec4.Length
                        {
                            Line lineStart = new Line(pt3, pt3 - vec3.GetNormal() * Parameter.ClearExtendLength);
                            Vector3d endVec = pt0 - ptf1;
                            Line lineEnd = new Line(ptf1 - endVec.GetNormal() * value * 2, pt0 + endVec.GetNormal() * value * 2);
                            List<Point3d> intersectionList = lineStart.Intersect(lineEnd, Intersect.OnBothOperands);
                            if (intersectionList.Count > 0)
                            {
                                Point3d intersectionPoint = intersectionList.FindByMin(x => x.DistanceTo(pt3));

                                deleteList.Add((i) % num);
                                deleteList.Add((i + 1) % num);
                                deleteList.Add((i + 2) % num);

                                insertMap.Add((i + num) % num, intersectionPoint);
                                find = 1;
                            }
                        }
                    }
                }

                if (find == 1) i = i + 4;
                else i++;
            }
            List<int> insertList = insertMap.Keys.ToList();
            for (int i = num - 1; i >= 0; i--)
            {
                if (deleteList.Contains(i))
                {
                    newClearedPl.RemoveVertexAt(i);
                }
                if (insertList.Contains(i))
                {
                    newClearedPl.AddVertexAt(i, insertMap[i].ToPoint2D(), 0, 0, 0);
                }
            }


            //第五筛
            deleteList.Clear();
            insertMap.Clear();
            insertList.Clear();
            num = newClearedPl.NumberOfVertices;
            for (int i = 0; i < num;)
            {
                Point3d pt0 = newClearedPl.GetPoint3dAt(i);
                var pt1 = newClearedPl.GetPoint3dAt((i + 1) % num);
                var pt2 = newClearedPl.GetPoint3dAt((i + 2) % num);
                var pt3 = newClearedPl.GetPoint3dAt((i + 3) % num);
                var pt4 = newClearedPl.GetPoint3dAt((i + 4) % num);

                Vector3d vec1 = pt1 - pt0;
                Vector3d vec2 = pt2 - pt1;
                Vector3d vec3 = pt3 - pt2;
                Vector3d vec4 = pt4 - pt3;

                double angle1 = vec1.GetAngleTo(vec3, Vector3d.ZAxis);
                bool flag = false;
                if (angle1 < 0.3 || angle1 > Math.PI * 2 - 0.3) flag = true;

                int find = 0;
                double angle = vec1.GetAngleTo(vec2, Vector3d.ZAxis);

                if (vec2.Length < value && angle < 1.5 * Math.PI + 0.1 && angle > 1.5 * Math.PI - 0.1 && flag)
                {
                    Line lineStart = new Line(pt0, pt0 + vec1.GetNormal() * Parameter.ClearExtendLength);
                    Line lineEnd = new Line(pt3 - vec4.GetNormal() * value * 2, pt4 + vec4.GetNormal() * value * 2);
                    List<Point3d> intersectionList = lineStart.Intersect(lineEnd, Intersect.OnBothOperands);
                    if (intersectionList.Count > 0)
                    {
                        Point3d intersectionPoint = intersectionList.FindByMin(x => x.DistanceTo(pt1));

                        deleteList.Add((i + 1) % num);
                        deleteList.Add((i + 2) % num);
                        deleteList.Add((i + 3) % num);

                        insertMap.Add((i + 1) % num, intersectionPoint);
                        find = 1;
                    }
                }
                if (find == 1) i = i + 3;
                else i++;
            }
            insertList = insertMap.Keys.ToList();
            for (int i = num - 1; i >= 0; i--)
            {
                if (deleteList.Contains(i))
                {
                    newClearedPl.RemoveVertexAt(i);
                }
                if (insertList.Contains(i))
                {
                    newClearedPl.AddVertexAt(i, insertMap[i].ToPoint2D(), 0, 0, 0);
                }
            }

            //第六筛
            deleteList.Clear();
            insertMap.Clear();
            insertList.Clear();
            num = newClearedPl.NumberOfVertices;
            for (int i = num - 1; i >= 0;)
            {
                Point3d pt0 = newClearedPl.GetPoint3dAt(i);
                var pt1 = newClearedPl.GetPoint3dAt((i - 1 + num) % num);
                var pt2 = newClearedPl.GetPoint3dAt((i - 2 + num) % num);
                var pt3 = newClearedPl.GetPoint3dAt((i - 3 + num) % num);
                var pt4 = newClearedPl.GetPoint3dAt((i - 4 + num) % num);

                Vector3d vec1 = pt1 - pt0;
                Vector3d vec2 = pt2 - pt1;
                Vector3d vec3 = pt3 - pt2;
                Vector3d vec4 = pt4 - pt3;

                double angle1 = vec1.GetAngleTo(vec3, Vector3d.ZAxis);
                bool flag = false;
                if (angle1 < 0.3 || angle1 > Math.PI * 2 - 0.3) flag = true;

                int find = 0;
                double angle = vec1.GetAngleTo(vec2, Vector3d.ZAxis);

                if (vec2.Length < value && angle < 0.5 * Math.PI + 0.1 && angle > 0.5 * Math.PI - 0.1 && flag)
                {
                    Line lineStart = new Line(pt0, pt0 + vec1.GetNormal() * Parameter.ClearExtendLength);
                    Line lineEnd = new Line(pt3 - vec4.GetNormal() * value * 2, pt4 + vec4.GetNormal() * value * 2);
                    List<Point3d> intersectionList = lineStart.Intersect(lineEnd, Intersect.OnBothOperands);
                    if (intersectionList.Count > 0)
                    {
                        Point3d intersectionPoint = intersectionList.FindByMin(x => x.DistanceTo(pt1));

                        deleteList.Add((i - 1 + num) % num);
                        deleteList.Add((i - 2 + num) % num);
                        deleteList.Add((i - 3 + num) % num);

                        insertMap.Add((i - 3 + num) % num, intersectionPoint);
                        find = 1;
                    }
                }
                if (find == 1) i = i - 3;
                else i--;
            }
            insertList = insertMap.Keys.ToList();
            for (int i = num - 1; i >= 0; i--)
            {
                if (deleteList.Contains(i))
                {
                    newClearedPl.RemoveVertexAt(i);
                }
                if (insertList.Contains(i))
                {
                    newClearedPl.AddVertexAt(i, insertMap[i].ToPoint2D(), 0, 0, 0);
                }
            }

            //
            newClearedPl = ConvertToIntegerPolyline(newClearedPl);

            //return
            return newClearedPl;
        }

        static public void ClearPolyline(ref Polyline newClearedPl, double value = 5)
        {
            if (!newClearedPl.IsCCW()) newClearedPl.ReverseCurve();

            List<int> deleteList = new List<int>();
            int num = newClearedPl.NumberOfVertices;

            //第一筛
            //筛除 重合点+距离过段线段
            for (int i = 0; i < num; i++)
            {
                var pt1 = newClearedPl.GetPoint3dAt(i);
                //如果超出距离就跳出

                var pt2 = newClearedPl.GetPoint3dAt((i + 1) % num);
                Vector3d dir2 = pt2 - pt1;
                if (dir2.Length < value)
                {
                    deleteList.Add(i);
                }
            }
            for (int i = num - 1; i >= 0; i--)
            {
                if (deleteList.Contains(i))
                {
                    //if (newClearedPl.NumberOfVertices >= 4)
                    //{

                    //}
                    //else break;

                    newClearedPl.RemoveVertexAt(i);
                }
            }

            //第二筛
            //筛除 直线多余点
            deleteList = new List<int>();
            num = newClearedPl.NumberOfVertices;
            for (int i = 0; i < num; i++)
            {
                var pt1 = newClearedPl.GetPoint3dAt(i);
                //如果超出距离就跳出
                var pt0 = newClearedPl.GetPoint3dAt((i + num - 1) % num);
                var pt2 = newClearedPl.GetPoint3dAt((i + 1) % num);
                Vector3d dir1 = pt1 - pt0;
                Vector3d dir2 = pt2 - pt1;
                double angle = dir2.GetAngleTo(dir1, Vector3d.ZAxis);
                if (angle < 0.1 || angle > 2 * Math.PI - 0.1)
                {
                    deleteList.Add(i);
                }
            }
            for (int i = num - 1; i >= 0; i--)
            {
                if (deleteList.Contains(i))
                {
                    newClearedPl.RemoveVertexAt(i);
                }
            }
        }

        static public Polyline ClearPolylineUnclosed(Polyline originalPl)
        {
            Polyline newPl = originalPl.Clone() as Polyline;
            int num = newPl.NumberOfVertices;

            //第一筛
            //筛除 重合点+距离过段线段
            List<int> deleteList = new List<int>();
            for (int i = 0; i < num - 1; i++)
            {
                Point3d pt0 = newPl.GetPoint3dAt(i);
                Point3d pt1 = newPl.GetPoint3dAt(i + 1);
                if ((pt1 - pt0).Length < 0.5)
                {
                    deleteList.Add(i);
                }
            }

            for (int i = num - 1; i >= 0; i--)
            {
                if (deleteList.Contains(i))
                {
                    newPl.RemoveVertexAt(i);
                }
            }

            //第二筛
            //筛除 直线多余点
            deleteList = new List<int>();
            num = newPl.NumberOfVertices;
            for (int i = 0; i < num - 2; i++)
            {
                var pt0 = newPl.GetPoint3dAt(i);
                var pt1 = newPl.GetPoint3dAt((i + 1) % num);
                var pt2 = newPl.GetPoint3dAt((i + 2) % num);
                Vector3d dir1 = pt1 - pt0;
                Vector3d dir2 = pt2 - pt1;
                double angle = dir2.GetAngleTo(dir1, Vector3d.ZAxis);
                if (angle < 0.1 || angle > 2 * Math.PI - 0.1)
                {
                    deleteList.Add(i + 1);
                }
            }
            for (int i = num - 1; i >= 0; i--)
            {
                if (deleteList.Contains(i))
                {
                    newPl.RemoveVertexAt(i);
                }
            }

            return newPl;
        }
        //
        static public Polyline ClearBends(Polyline originalPl, Polyline boundary, double dis)
        {
            Polyline newPl = originalPl.Clone() as Polyline;
            newPl = ClearPolylineUnclosed(newPl);

            Polyline newBoundary = boundary.Buffer(-50).OfType<Polyline>().ToList().OrderByDescending(x => x.Area).First();
            int num = newPl.NumberOfVertices;
            for (int i = num - 3; i >= 0; i--)
            {
                if (i + 2 >= newPl.NumberOfVertices) continue;
                //if (i == 0) continue;
                Point3d pt0 = newPl.GetPoint3dAt(i);
                Point3d pt1 = newPl.GetPoint3dAt(i + 1);
                Point3d pt2 = newPl.GetPoint3dAt(i + 2);

                if ((pt1 - pt0).Length < dis)
                {
                    Point3d newPt = FindDiagonalPoint(pt0, pt1, pt2);
                    if (newBoundary.Contains(newPt))
                    {
                        newPl.AddVertexAt(i, newPt.ToPoint2D(), 0, 0, 0);
                        newPl.RemoveVertexAt(i + 1);
                        newPl.RemoveVertexAt(i + 1);
                        newPl.RemoveVertexAt(i + 1);

                    }
                }
            }

            return newPl;
        }

        static public void ClearBendsTest(Polyline originalPl, Polyline boundary, double dis, out Polyline newPl)
        {
            newPl = originalPl.Clone() as Polyline;
            newPl = ClearPolylineUnclosed(newPl);

            Polyline newBoundary = boundary.Buffer(-50).OfType<Polyline>().ToList().OrderByDescending(x => x.Area).First();
            int num = newPl.NumberOfVertices;
            for (int i = num - 3; i >= 0; i--)
            {
                if (i + 2 >= newPl.NumberOfVertices) continue;
                //if (i == 0) continue;
                Point3d pt0 = newPl.GetPoint3dAt(i);
                Point3d pt1 = newPl.GetPoint3dAt(i + 1);
                Point3d pt2 = newPl.GetPoint3dAt(i + 2);

                if ((pt1 - pt0).Length < dis)
                {
                    Point3d newPt = FindDiagonalPoint(pt0, pt1, pt2);
                    if (newBoundary.Contains(newPt))
                    {
                        newPl.AddVertexAt(i, newPt.ToPoint2D(), 0, 0, 0);
                        newPl.RemoveVertexAt(i + 1);
                        newPl.RemoveVertexAt(i + 1);
                        newPl.RemoveVertexAt(i + 1);

                    }
                }
            }
        }

        static public Polyline ClearBendsLongFirst(Polyline originalPl, Polyline boundary, double dis, int Mode)
        {
            Polyline newPl = originalPl.Clone() as Polyline;
            newPl = ClearPolylineUnclosed(newPl);

            double bufferDis = 0;
            if (Mode == 0)
            {
                bufferDis = 5;
            }
            else if (Mode == 1)
            {
                bufferDis = -50;
            }

            Polyline newBoundary = boundary.Buffer(bufferDis).OfType<Polyline>().ToList().OrderByDescending(x => x.Area).First();
            int num = newPl.NumberOfVertices;
            for (int i = num - 4; i >= 0; i--)
            {
                if (i + 3 >= newPl.NumberOfVertices) continue;
                //if (i == 0) continue;
                Point3d pt0 = newPl.GetPoint3dAt(i);
                Point3d pt1 = newPl.GetPoint3dAt(i + 1);
                Point3d pt2 = newPl.GetPoint3dAt(i + 2);
                Point3d pt3 = newPl.GetPoint3dAt(i + 3);

                if ((pt2 - pt1).Length < dis)
                {
                    Point3d newPt1 = FindDiagonalPoint(pt0, pt1, pt2);
                    Point3d newPt2 = FindDiagonalPoint(pt1, pt2, pt3);

                    bool ok1 = newBoundary.Contains(new Line(newPt1, pt2)) && newBoundary.Contains(new Line(newPt1, pt0));
                    bool ok2 = newBoundary.Contains(new Line(newPt2, pt1)) && newBoundary.Contains(new Line(newPt2, pt3));
                    if (i + 3 == num - 1) ok2 = false;
                    if (i == 0) ok1 = false;


                    if (i + 3 == newPl.NumberOfVertices) ok2 = false;
                    if (i == 0) ok1 = false;

                    Vector3d vec0 = pt1 - pt0;
                    Vector3d vec2 = pt3 - pt2;
                    if ((ok1 && ok2 && vec0.Length > vec2.Length) || (ok2 && !ok1))
                    {
                        newPl.AddVertexAt(i + 1, newPt2.ToPoint2D(), 0, 0, 0);
                        newPl.RemoveVertexAt(i + 2);
                        newPl.RemoveVertexAt(i + 2);
                        newPl.RemoveVertexAt(i + 2);
                    }
                    else if ((ok1 && ok2 && vec0.Length < vec2.Length) || (!ok2 && ok1))
                    {
                        newPl.AddVertexAt(i, newPt1.ToPoint2D(), 0, 0, 0);
                        newPl.RemoveVertexAt(i + 1);
                        newPl.RemoveVertexAt(i + 1);
                        newPl.RemoveVertexAt(i + 1);
                    }
                }
            }

            return newPl;
        }

        static public Polyline ConvertToIntegerPolyline(Polyline originalPl)
        {
            var newClearedPl = originalPl.Clone() as Polyline; //change copy

            int num = newClearedPl.NumberOfVertices;
            Vector3d dx = new Vector3d(1, 0, 0);
            //Vector3d dy = new Vector3d(0, 1, 0);

            for (int i = 0; i < num; i++)
            {
                var pt0 = newClearedPl.GetPoint3dAt(i);
                var pt1 = newClearedPl.GetPoint3dAt((i + 1) % num);

                int x0 = 0;
                int y0 = 0;
                int x1 = 0;
                int y1 = 0;

                Vector3d vec = (pt1 - pt0).GetNormal();
                if (Math.Abs(vec.DotProduct(dx)) > 0.9)  // 沿着 x 轴 
                {
                    x0 = (int)pt0.X;
                    y0 = (int)pt0.Y;
                    x1 = (int)pt1.X;
                    y1 = (int)pt0.Y;
                }
                else if (Math.Abs(vec.DotProduct(dx)) < 0.1)
                {
                    x0 = (int)pt0.X;
                    y0 = (int)pt0.Y;
                    x1 = (int)pt0.X;
                    y1 = (int)pt1.Y;
                }
                else
                {
                    continue;
                }

                newClearedPl.RemoveVertexAt(i);
                newClearedPl.AddVertexAt(i, new Point2d(x0, y0), 0, 0, 0);

                newClearedPl.RemoveVertexAt((i + 1) % num);
                newClearedPl.AddVertexAt((i + 1) % num, new Point2d(x1, y1), 0, 0, 0);
            }
            DrawUtils.ShowGeometry(newClearedPl, "l2newClearedPl", 3, lineWeightNum: 30);
            return newClearedPl;
        }

        static public Point3d FindDiagonalPoint(Point3d pt0, Point3d pt1, Point3d pt2)
        {
            Vector3d dir = pt1 - pt0;
            Point3d newPt = pt2 - dir;
            return newPt;
        }

        public static List<Polyline> GetCenterLine(Polyline poly)
        {
            return ThMEPPolygonService.CenterLine(poly.ToNTSPolygon().ToDbMPolygon())
                                      .ToCollection()
                                      .LineMerge()
                                      .OfType<Polyline>().ToList();
        }

        static public Polyline PlRegularization3(Polyline originPl, double value)
        {
            //Polyline ClearedPl = OriginPl.Clone() as Polyline;
            var originalArea = originPl.Area;
            List<Polyline> ClearedPlList = originPl.Buffer(-value / 2).OfType<Polyline>().ToList();
            Polyline ClearedPl = new Polyline();

            if (ClearedPlList.Count > 0)
            {
                ClearedPl = ClearedPlList.FindByMax(x => x.Area);
            }
            else return originPl;

            DrawUtils.ShowGeometry(ClearedPl, "l1bBufferedClearedPl", 4, lineWeightNum: 30);

            Polyline newClearedPl = ClearedPl.Buffer(value / 2).OfType<Polyline>().ToList().FindByMax(x => x.Area);
            var newArea = newClearedPl.Area;

            var proportion = newArea / originalArea;

            if (proportion < 0.8)
            {
                int i = 0;
            }

            if (!newClearedPl.IsCCW())
            {
                newClearedPl.ReverseCurve();
            }

            List<int> deleteList = new List<int>();
            int num = newClearedPl.NumberOfVertices;

            //清理Polyline 
            ClearPolyline(ref newClearedPl);

            //第三筛 筛除外凸耳朵
            deleteList = new List<int>();
            num = newClearedPl.NumberOfVertices;
            for (int i = 0; i < num;)
            {
                Point3d pt1 = newClearedPl.GetPoint3dAt(i);
                var pt0 = newClearedPl.GetPoint3dAt((i + num - 1) % num);
                var pt2 = newClearedPl.GetPoint3dAt((i + 1) % num);
                var pt3 = newClearedPl.GetPoint3dAt((i + 2) % num);
                var ptf1 = newClearedPl.GetPoint3dAt((i + num - 2) % num);

                Vector3d line1 = pt1 - pt0;
                Vector3d line2 = pt2 - pt1;
                Vector3d line3 = pt3 - pt2;

                Vector3d line0 = pt0 - ptf1;

                double dProduct = line1.DotProduct(line3);

                double angle = line0.GetAngleTo(line1, Vector3d.ZAxis);

                if (angle < 1.5 * Math.PI + 0.1 && angle > 1.5 * Math.PI - 0.1)
                {
                    if (line1.Length < value && line3.Length < value && dProduct < 0)
                    {
                        if (Math.Abs(line1.Length - line3.Length) < 10)
                        {
                            deleteList.Add(i);
                            deleteList.Add((i + num - 1) % num);
                            deleteList.Add((i + 1) % num);
                            deleteList.Add((i + 2) % num);
                            i = i + 3;
                            continue;
                        }
                    }
                }
                i++;
            }
            for (int i = num - 1; i >= 0; i--)
            {
                if (deleteList.Contains(i))
                {
                    newClearedPl.RemoveVertexAt(i);
                }
            }

            //第四筛
            deleteList.Clear();
            num = newClearedPl.NumberOfVertices;
            Dictionary<int, Point3d> insertMap = new Dictionary<int, Point3d>();
            for (int i = 0; i < num;)
            {
                Point3d pt0 = newClearedPl.GetPoint3dAt(i);
                var pt1 = newClearedPl.GetPoint3dAt((i + 1) % num);
                var pt2 = newClearedPl.GetPoint3dAt((i + 2) % num);
                var pt3 = newClearedPl.GetPoint3dAt((i + 3) % num);
                var pt4 = newClearedPl.GetPoint3dAt((i + 4) % num);

                var pt5 = newClearedPl.GetPoint3dAt((i + 5) % num);
                var ptf1 = newClearedPl.GetPoint3dAt((i + num - 1) % num);

                Vector3d vec1 = pt1 - pt0;
                Vector3d vec2 = pt2 - pt1;
                Vector3d vec3 = pt3 - pt2;
                Vector3d vec4 = pt4 - pt3;

                int find = 0;

                if (vec2.Length < value && vec3.Length < value)
                {
                    double angle = vec2.GetAngleTo(vec3, Vector3d.ZAxis);
                    if (angle < 1.5 * Math.PI + 0.1 && angle > 1.5 * Math.PI - 0.1)
                    {
                        if (vec1.Length > vec4.Length)
                        {
                            Line lineStart = new Line(pt1, pt1 + vec2.GetNormal() * Parameter.ClearExtendLength);
                            Vector3d endVec = pt5 - pt4;
                            Line lineEnd = new Line(pt4 - endVec.GetNormal() * value * 2, pt5 + endVec.GetNormal() * value * 2);
                            List<Point3d> intersectionList = lineStart.Intersect(lineEnd, Intersect.OnBothOperands);
                            if (intersectionList.Count > 0)
                            {
                                Point3d intersectionPoint = intersectionList.FindByMin(x => x.DistanceTo(pt1));

                                deleteList.Add((i + 2) % num);
                                deleteList.Add((i + 3) % num);
                                deleteList.Add((i + 4) % num);

                                insertMap.Add(i + 2, intersectionPoint);
                                find = 1;
                            }
                        }
                        else   //vec1.Length < vec4.Length
                        {
                            Line lineStart = new Line(pt3, pt3 - vec3.GetNormal() * Parameter.ClearExtendLength);
                            Vector3d endVec = pt0 - ptf1;
                            Line lineEnd = new Line(ptf1 - endVec.GetNormal() * value * 2, pt0 + endVec.GetNormal() * value * 2);
                            List<Point3d> intersectionList = lineStart.Intersect(lineEnd, Intersect.OnBothOperands);
                            if (intersectionList.Count > 0)
                            {
                                Point3d intersectionPoint = intersectionList.FindByMin(x => x.DistanceTo(pt3));

                                deleteList.Add((i) % num);
                                deleteList.Add((i + 1) % num);
                                deleteList.Add((i + 2) % num);

                                insertMap.Add((i + num) % num, intersectionPoint);
                                find = 1;
                            }
                        }
                    }
                }

                if (find == 1) i = i + 4;
                else i++;
            }
            List<int> insertList = insertMap.Keys.ToList();
            for (int i = num - 1; i >= 0; i--)
            {
                if (deleteList.Contains(i))
                {
                    newClearedPl.RemoveVertexAt(i);
                }
                if (insertList.Contains(i))
                {
                    newClearedPl.AddVertexAt(i, insertMap[i].ToPoint2D(), 0, 0, 0);
                }
            }


            //第五筛
            deleteList.Clear();
            insertMap.Clear();
            insertList.Clear();
            num = newClearedPl.NumberOfVertices;
            for (int i = 0; i < num;)
            {
                Point3d pt0 = newClearedPl.GetPoint3dAt(i);
                var pt1 = newClearedPl.GetPoint3dAt((i + 1) % num);
                var pt2 = newClearedPl.GetPoint3dAt((i + 2) % num);
                var pt3 = newClearedPl.GetPoint3dAt((i + 3) % num);
                var pt4 = newClearedPl.GetPoint3dAt((i + 4) % num);

                Vector3d vec1 = pt1 - pt0;
                Vector3d vec2 = pt2 - pt1;
                Vector3d vec3 = pt3 - pt2;
                Vector3d vec4 = pt4 - pt3;

                double angle1 = vec1.GetAngleTo(vec3, Vector3d.ZAxis);
                bool flag = false;
                if (angle1 < 0.3 || angle1 > Math.PI * 2 - 0.3) flag = true;

                int find = 0;
                double angle = vec1.GetAngleTo(vec2, Vector3d.ZAxis);

                if (vec2.Length < value && angle < 1.5 * Math.PI + 0.1 && angle > 1.5 * Math.PI - 0.1 && flag)
                {
                    Line lineStart = new Line(pt0, pt0 + vec1.GetNormal() * Parameter.ClearExtendLength);
                    Line lineEnd = new Line(pt3 - vec4.GetNormal() * value * 2, pt4 + vec4.GetNormal() * value * 2);
                    List<Point3d> intersectionList = lineStart.Intersect(lineEnd, Intersect.OnBothOperands);
                    if (intersectionList.Count > 0)
                    {
                        Point3d intersectionPoint = intersectionList.FindByMin(x => x.DistanceTo(pt1));

                        deleteList.Add((i + 1) % num);
                        deleteList.Add((i + 2) % num);
                        deleteList.Add((i + 3) % num);

                        insertMap.Add((i + 1) % num, intersectionPoint);
                        find = 1;
                    }
                }
                if (find == 1) i = i + 3;
                else i++;
            }
            insertList = insertMap.Keys.ToList();
            for (int i = num - 1; i >= 0; i--)
            {
                if (deleteList.Contains(i))
                {
                    newClearedPl.RemoveVertexAt(i);
                }
                if (insertList.Contains(i))
                {
                    newClearedPl.AddVertexAt(i, insertMap[i].ToPoint2D(), 0, 0, 0);
                }
            }

            //第六筛
            deleteList.Clear();
            insertMap.Clear();
            insertList.Clear();
            num = newClearedPl.NumberOfVertices;
            for (int i = num - 1; i >= 0;)
            {
                Point3d pt0 = newClearedPl.GetPoint3dAt(i);
                var pt1 = newClearedPl.GetPoint3dAt((i - 1 + num) % num);
                var pt2 = newClearedPl.GetPoint3dAt((i - 2 + num) % num);
                var pt3 = newClearedPl.GetPoint3dAt((i - 3 + num) % num);
                var pt4 = newClearedPl.GetPoint3dAt((i - 4 + num) % num);

                Vector3d vec1 = pt1 - pt0;
                Vector3d vec2 = pt2 - pt1;
                Vector3d vec3 = pt3 - pt2;
                Vector3d vec4 = pt4 - pt3;

                double angle1 = vec1.GetAngleTo(vec3, Vector3d.ZAxis);
                bool flag = false;
                if (angle1 < 0.3 || angle1 > Math.PI * 2 - 0.3) flag = true;

                int find = 0;
                double angle = vec1.GetAngleTo(vec2, Vector3d.ZAxis);

                if (vec2.Length < value && angle < 0.5 * Math.PI + 0.1 && angle > 0.5 * Math.PI - 0.1 && flag)
                {
                    Line lineStart = new Line(pt0, pt0 + vec1.GetNormal() * Parameter.ClearExtendLength);
                    Line lineEnd = new Line(pt3 - vec4.GetNormal() * value * 2, pt4 + vec4.GetNormal() * value * 2);
                    List<Point3d> intersectionList = lineStart.Intersect(lineEnd, Intersect.OnBothOperands);
                    if (intersectionList.Count > 0)
                    {
                        Point3d intersectionPoint = intersectionList.FindByMin(x => x.DistanceTo(pt1));

                        deleteList.Add((i - 1 + num) % num);
                        deleteList.Add((i - 2 + num) % num);
                        deleteList.Add((i - 3 + num) % num);

                        insertMap.Add((i - 3 + num) % num, intersectionPoint);
                        find = 1;
                    }
                }
                if (find == 1) i = i - 3;
                else i--;
            }
            insertList = insertMap.Keys.ToList();
            for (int i = num - 1; i >= 0; i--)
            {
                if (deleteList.Contains(i))
                {
                    newClearedPl.RemoveVertexAt(i);
                }
                if (insertList.Contains(i))
                {
                    newClearedPl.AddVertexAt(i, insertMap[i].ToPoint2D(), 0, 0, 0);
                }
            }

            //
            newClearedPl = ConvertToIntegerPolyline(newClearedPl);

            //return
            return newClearedPl;
        }

        /// <summary>
        /// //////////////////
        /// </summary>
        /// <param name="originPl"></param>
        /// <param name="fixList"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        static public Polyline PlClearSmall(Polyline originPl, List<Point3d> fixList, double value)
        {

            // 点集重排列
            var points = PassageWayUtils.GetPolyPoints(originPl);
            points = SmoothUtils.SmoothPoints(points);
            var si = PassageWayUtils.GetPointIndex(fixList[0], points);
            var ei = PassageWayUtils.GetPointIndex(fixList[1], points);

            if (si == -1 || ei == -1) return originPl;

            if ((ei + 1) % points.Count != si)
            {
                var tmp = si;
                si = ei;
                ei = tmp;
            }
            PassageWayUtils.RearrangePoints(ref points, si);
            // 多段线倒圆角
            var ret = PassageWayUtils.BuildPolyline(points);

            //清理Polyline 
            Polyline newPl = ClearPolylineUnclosed(ret);

            double bufferDis = 0;
            bufferDis = 75;


            Polyline newBoundary = originPl.Buffer(bufferDis).OfType<Polyline>().ToList().OrderByDescending(x => x.Area).First();

            newPl = ClearSmallHelper1(newPl, newBoundary, fixList, value, 0);
            newPl = ClearSmallHelper1(newPl, newBoundary, fixList, value, 1);
            newPl = ClearSmallHelper1(newPl, newBoundary, fixList, value, 2);

            var pointsNew = PassageWayUtils.GetPolyPoints(newPl);
            pointsNew = SmoothUtils.SmoothPoints(pointsNew);
            newPl = PassageWayUtils.BuildPolyline(pointsNew);

            return newPl;
        }


        static public Polyline ClearSmallHelper1(Polyline originPl, Polyline newBoundary, List<Point3d> fixList, double value, int mode = 1)
        {
            Polyline newPl = originPl;
            var points = PassageWayUtils.GetPolyPoints(newPl);
            int num = newPl.NumberOfVertices;
            for (int i = num - 4; i >= 0; i--)
            {
                if (i == 2)
                {
                    int stop = 0;
                }
                if (i + 3 >= newPl.NumberOfVertices) continue;
                //if (i == 0) continue;
                Point3d pt0 = newPl.GetPoint3dAt(i);
                Point3d pt1 = newPl.GetPoint3dAt(i + 1);
                Point3d pt2 = newPl.GetPoint3dAt(i + 2);
                Point3d pt3 = newPl.GetPoint3dAt(i + 3);

                if ((pt2 - pt1).Length < 31 && (pt2 - pt1).Length > 29)
                {
                    int stop = 0;
                }

                Vector3d vec0 = pt1 - pt0;
                Vector3d vec1 = pt2 - pt1;
                Vector3d vec2 = pt3 - pt2;


                bool isTurnBack = false;
                if (vec0.GetNormal().DotProduct(vec2.GetNormal()) < -0.95)
                    isTurnBack = true;

                if (mode == 2) isTurnBack = true;

                if ((pt2 - pt1).Length < value && isTurnBack)
                {
                    Point3d newPt1 = FindDiagonalPoint(pt0, pt1, pt2);
                    Point3d newPt2 = FindDiagonalPoint(pt1, pt2, pt3);

                    bool ok1 = newBoundary.Contains(new Line(newPt1, pt2)) && newBoundary.Contains(new Line(newPt1, pt0));
                    bool ok2 = newBoundary.Contains(new Line(newPt2, pt1)) && newBoundary.Contains(new Line(newPt2, pt3));

                    if (mode == 2)
                    {
                        bool ok12 = CheckFixPointSafe(pt0, pt1, pt2, fixList);
                        bool ok22 = CheckFixPointSafe(pt1, pt2, pt3, fixList);
                        ok1 = ok1 && ok12;
                        ok2 = ok2 && ok22;
                    }

                    if (i + 3 == num - 1) ok2 = false;
                    if (i == 0) ok1 = false;

                    if ((ok1 && ok2 && vec0.Length > vec2.Length) || (ok2 && !ok1))
                    {
                        newPl.AddVertexAt(i + 1, newPt2.ToPoint2D(), 0, 0, 0);
                        newPl.RemoveVertexAt(i + 2);
                        newPl.RemoveVertexAt(i + 2);
                        newPl.RemoveVertexAt(i + 2);
                    }
                    else if ((ok1 && ok2 && vec0.Length <= vec2.Length) || (!ok2 && ok1))
                    {
                        newPl.AddVertexAt(i, newPt1.ToPoint2D(), 0, 0, 0);
                        newPl.RemoveVertexAt(i + 1);
                        newPl.RemoveVertexAt(i + 1);
                        newPl.RemoveVertexAt(i + 1);
                    }
                }


                //特殊折返
                if (mode == 0)
                {
                    if (vec0.GetNormal().DotProduct(vec1.GetNormal()) < -0.95)
                    {
                        newPl.RemoveVertexAt(i + 1);
                    }
                }
            }

            var pointsNew = PassageWayUtils.GetPolyPoints(newPl);
            pointsNew = SmoothUtils.SmoothPoints(pointsNew);
            newPl = PassageWayUtils.BuildPolyline(pointsNew);

            return newPl;
        }


        static bool CheckFixPointSafe(Point3d pt0, Point3d pt1, Point3d pt2, List<Point3d> fixList)
        {
            List<Point3d> ptList = new List<Point3d>();
            ptList.Add(pt0);
            ptList.Add(pt1);
            ptList.Add(pt2);

            Polyline newPl = PassageWayUtils.BuildPolyline(ptList);

            for (int i = 0; i < fixList.Count; i++)
            {
                double dis = newPl.GetClosestPointTo(fixList[i], false).DistanceTo(fixList[i]);
                if (dis < 5) return false;
            }
            return true;
        }
    }

    class VectorProcessService
    {
        static public int GetVecIndex(Vector3d vec) 
        {
            Vector3d newVec = vec.GetNormal();

            int res = -10;
            if (newVec.DotProduct(new Vector3d(1, 0, 0)) > 0.95) 
            {
                res = 0;
            }else if (newVec.DotProduct(new Vector3d(0, 1, 0)) > 0.95)
            {
                res = 1;
            }
            else if (newVec.DotProduct(new Vector3d(-1, 0, 0)) > 0.95)
            {
                res = 2;
            }
            else if (newVec.DotProduct(new Vector3d(0, -1, 0)) > 0.95)
            {
                res = 3;
            }

            return res;
        }
    }
}
