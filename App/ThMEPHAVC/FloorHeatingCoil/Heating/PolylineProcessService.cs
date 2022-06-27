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
using ThMEPEngineCore.Model.Hvac;
using ThMEPHVAC.FloorHeatingCoil.Heating;

namespace ThMEPHVAC.FloorHeatingCoil.Heating
{
    class PolylineProcessService
    {
        //
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


        public static void GetRecInfo(Polyline rec, ref Point3d center, ref Vector3d ShortDir,ref Vector3d LongSide,ref Vector3d ShortSide)
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



        //清理polyline （废弃）
        static public Polyline PlRegularization1(Polyline OriginPl)
        {
            Polyline ClearedPl = OriginPl.Clone() as Polyline;
            for (int i = 0; i < ClearedPl.NumberOfVertices;)
            {
                Point3d pt1 = ClearedPl.GetPoint3dAt(i);
                var pt0 = ClearedPl.GetPoint3dAt((i + ClearedPl.NumberOfVertices - 1) % ClearedPl.NumberOfVertices);
                var pt2 = ClearedPl.GetPoint3dAt((i + 1) % ClearedPl.NumberOfVertices);

                Vector3d line1 = pt1 - pt0;
                Vector3d line2 = pt2 - pt1;

                if (line2.Length < Parameter.ClearThreshold)
                {
                    double originalAngle = 0;

                    List<Vector3d> lineToDelete = new List<Vector3d>();
                    int index = i + 2;
                    var ptOld = pt2;
                    while (true)
                    {
                        index = i + 2;
                        var ptNew = ClearedPl.GetPoint3dAt(index % ClearedPl.NumberOfVertices);
                        Vector3d lineNew = ptNew - ptOld;
                    }
                }
                else
                {
                    i++;
                }
            }
            return ClearedPl;
        }


        //清理polyline
        static public Polyline PlRegularization2(Polyline originPl,double value)
        {
            //Polyline ClearedPl = OriginPl.Clone() as Polyline;
            var originalArea = originPl.Area;
            Polyline ClearedPl = originPl.Buffer(-value).OfType<Polyline>().ToList().FindByMax(x=>x.Area);
            DrawUtils.ShowGeometry(ClearedPl, "l1bBufferedClearedPl", 4, lineWeightNum: 30);

            Polyline newClearedPl = ClearedPl.Buffer(value).OfType<Polyline>().ToList().FindByMax(x => x.Area);
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

            //第三筛
            deleteList = new List<int>();
            num = newClearedPl.NumberOfVertices;
            for (int i = 0; i < num;)
            {
                Point3d pt1 = newClearedPl.GetPoint3dAt(i);
                var pt0 = newClearedPl.GetPoint3dAt((i + num - 1) % num);
                var pt2 = newClearedPl.GetPoint3dAt((i + 1) % num);
                var pt3 = newClearedPl.GetPoint3dAt((i + 2) % num);

                Vector3d line1 = pt1 - pt0;
                Vector3d line2 = pt2 - pt1;
                Vector3d line3 = pt3 - pt2;
                if (line1.Length < Parameter.ClearThreshold && line3.Length < Parameter.ClearThreshold)
                {
                    deleteList.Add(i);
                    deleteList.Add((i + num - 1) % num);
                    deleteList.Add((i + 1) % num);
                    deleteList.Add((i + 2) % num);
                    i = i + 3;
                }
                else 
                {
                    i++;
                }
            }
            for (int i = num - 1; i >= 0; i--)
            {
                if (deleteList.Contains(i))
                {
                    newClearedPl.RemoveVertexAt(i);
                }
            }
            
            //第四筛
            return newClearedPl;
        }

        static public void ClearPolyline(ref Polyline newClearedPl) 
        {
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
                if (dir2.Length < 5)
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

    }
}
