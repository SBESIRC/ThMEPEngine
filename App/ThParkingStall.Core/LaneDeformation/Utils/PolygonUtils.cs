using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.LaneDeformation;

using NetTopologySuite.Operation.OverlayNG;
using ThParkingStall.Core.MPartitionLayout;
using NetTopologySuite.Operation.Buffer;

namespace ThParkingStall.Core.LaneDeformation
{
    internal class PolygonUtils
    {
        static public BufferParameters MitreParam = new BufferParameters(8, EndCapStyle.Flat, JoinStyle.Mitre, 5.0);

        static public Polygon CreatePolygonRec(double x0,double x1,double y0,double y1) 
        {
            List<Coordinate> pointList = new List<Coordinate>();
            pointList.Add(new Coordinate(x0, y0));
            pointList.Add(new Coordinate(x1, y0));
            pointList.Add(new Coordinate(x1, y1));
            pointList.Add(new Coordinate(x0, y1));
            pointList.Add(new Coordinate(x0, y0));
            Polygon Obb = new Polygon(new LinearRing(pointList.ToArray()));
            
            return Obb;
        }


        static public List<Polygon> GetBufferedPolygons(Polygon pl,double dis) 
        {
            List<Polygon> result = new List<Polygon>();
            Geometry g = pl.Buffer(dis,MitreParam);
            if (g is GeometryCollection collection)
            {
                foreach (var e in collection.Geometries)
                {
                    if (e is Polygon)
                    {
                        if (!e.IsEmpty)
                            result.Add((Polygon)e);
                    }
                }
            }
            else if (g is Polygon)
            {
                if (!g.IsEmpty)
                    result.Add((Polygon)g);
            }

            return result;
        }

        static public List<Polygon> ClearBufferHelper(Polygon pl,double fdis,double dis) 
        {
            List<Polygon> polygons = GetBufferedPolygons(pl, fdis);
            List<Polygon> result = new List<Polygon>();
            for (int i = 0; i < polygons.Count; i++) 
            {
                var g = polygons[i].Buffer(dis,MitreParam);
                if (g is GeometryCollection collection)
                {
                    foreach (var e in collection)
                    {
                        if (e is Polygon)
                        {
                            if(!e.IsEmpty)
                            result.Add((Polygon)e);
                        }
                    }
                }
                else if (g is Polygon)
                {
                    if (!g.IsEmpty)
                        result.Add((Polygon)g);
                }
            }

            return result;
        }



        ////清理polyline
        //static public Polygon Regularization2(Polyline originPl, double value)
        //{
        //    //Polyline ClearedPl = OriginPl.Clone() as Polyline;
        //    var originalArea = originPl.Area;
        //    Polyline ClearedPl = originPl.Buffer(-value / 2).OfType<Polyline>().ToList().FindByMax(x => x.Area);
        //    DrawUtils.ShowGeometry(ClearedPl, "l1bBufferedClearedPl", 4, lineWeightNum: 30);

        //    Polyline newClearedPl = ClearedPl.Buffer(value / 2).OfType<Polyline>().ToList().FindByMax(x => x.Area);
        //    var newArea = newClearedPl.Area;

        //    var proportion = newArea / originalArea;

        //    if (proportion < 0.8)
        //    {
        //        int i = 0;
        //    }

        //    if (!newClearedPl.IsCCW())
        //    {
        //        newClearedPl.ReverseCurve();
        //    }

        //    List<int> deleteList = new List<int>();
        //    int num = newClearedPl.NumberOfVertices;

        //    //清理Polyline 
        //    ClearPolyline(ref newClearedPl);

        //    //第三筛 筛除外凸耳朵 ,必须是类似门的结构
        //    deleteList = new List<int>();
        //    num = newClearedPl.NumberOfVertices;
        //    for (int i = 0; i < num;)
        //    {
        //        Point3d pt1 = newClearedPl.GetPoint3dAt(i);
        //        var pt0 = newClearedPl.GetPoint3dAt((i + num - 1) % num);
        //        var pt2 = newClearedPl.GetPoint3dAt((i + 1) % num);
        //        var pt3 = newClearedPl.GetPoint3dAt((i + 2) % num);
        //        var ptf1 = newClearedPl.GetPoint3dAt((i + num - 2) % num);

        //        Vector3d line1 = pt1 - pt0;
        //        Vector3d line2 = pt2 - pt1;
        //        Vector3d line3 = pt3 - pt2;

        //        Vector3d line0 = pt0 - ptf1;

        //        double dProduct = line1.DotProduct(line3);

        //        double angle = line0.GetAngleTo(line1, Vector3d.ZAxis);

        //        if (angle < 1.5 * Math.PI + 0.1 && angle > 1.5 * Math.PI - 0.1)   //逆时针情况，外凸，因此转270度
        //        {
        //            if (line1.Length < value && line3.Length < value && dProduct < 0)
        //            {
        //                if (Math.Abs(line1.Length - line3.Length) < 10)           //保证对称
        //                {
        //                    deleteList.Add(i);
        //                    deleteList.Add((i + num - 1) % num);
        //                    deleteList.Add((i + 1) % num);
        //                    deleteList.Add((i + 2) % num);
        //                    i = i + 3;
        //                    continue;
        //                }
        //            }
        //        }
        //        i++;
        //    }
        //    for (int i = num - 1; i >= 0; i--)
        //    {
        //        if (deleteList.Contains(i))
        //        {
        //            newClearedPl.RemoveVertexAt(i);
        //        }
        //    }

        //    //第四筛 
        //    deleteList.Clear();
        //    num = newClearedPl.NumberOfVertices;
        //    Dictionary<int, Point3d> insertMap = new Dictionary<int, Point3d>();
        //    for (int i = 0; i < num;)
        //    {
        //        Point3d pt0 = newClearedPl.GetPoint3dAt(i);
        //        var pt1 = newClearedPl.GetPoint3dAt((i + 1) % num);
        //        var pt2 = newClearedPl.GetPoint3dAt((i + 2) % num);
        //        var pt3 = newClearedPl.GetPoint3dAt((i + 3) % num);
        //        var pt4 = newClearedPl.GetPoint3dAt((i + 4) % num);

        //        var pt5 = newClearedPl.GetPoint3dAt((i + 5) % num);
        //        var ptf1 = newClearedPl.GetPoint3dAt((i + num - 1) % num);

        //        Vector3d vec1 = pt1 - pt0;
        //        Vector3d vec2 = pt2 - pt1;
        //        Vector3d vec3 = pt3 - pt2;
        //        Vector3d vec4 = pt4 - pt3;

        //        int find = 0;

        //        if (vec2.Length < value && vec3.Length < value)     //类似于墙角的形状
        //        {
        //            double angle = vec2.GetAngleTo(vec3, Vector3d.ZAxis);
        //            if (angle < 1.5 * Math.PI + 0.1 && angle > 1.5 * Math.PI - 0.1)
        //            {
        //                if (vec1.Length > vec4.Length)
        //                {
        //                    Line lineStart = new Line(pt1, pt1 + vec2.GetNormal() * Parameter.ClearExtendLength);
        //                    Vector3d endVec = pt5 - pt4;
        //                    Line lineEnd = new Line(pt4 - endVec.GetNormal() * value * 2, pt5 + endVec.GetNormal() * value * 2);
        //                    List<Point3d> intersectionList = lineStart.Intersect(lineEnd, Intersect.OnBothOperands);
        //                    if (intersectionList.Count > 0)
        //                    {
        //                        Point3d intersectionPoint = intersectionList.FindByMin(x => x.DistanceTo(pt1));

        //                        deleteList.Add((i + 2) % num);
        //                        deleteList.Add((i + 3) % num);
        //                        deleteList.Add((i + 4) % num);

        //                        insertMap.Add((i + 2) % num, intersectionPoint);
        //                        find = 1;
        //                    }
        //                }
        //                else   //vec1.Length < vec4.Length
        //                {
        //                    Line lineStart = new Line(pt3, pt3 - vec3.GetNormal() * Parameter.ClearExtendLength);
        //                    Vector3d endVec = pt0 - ptf1;
        //                    Line lineEnd = new Line(ptf1 - endVec.GetNormal() * value * 2, pt0 + endVec.GetNormal() * value * 2);
        //                    List<Point3d> intersectionList = lineStart.Intersect(lineEnd, Intersect.OnBothOperands);
        //                    if (intersectionList.Count > 0)
        //                    {
        //                        Point3d intersectionPoint = intersectionList.FindByMin(x => x.DistanceTo(pt3));

        //                        deleteList.Add((i) % num);
        //                        deleteList.Add((i + 1) % num);
        //                        deleteList.Add((i + 2) % num);

        //                        insertMap.Add((i + num) % num, intersectionPoint);
        //                        find = 1;
        //                    }
        //                }
        //            }
        //        }

        //        if (find == 1) i = i + 4;
        //        else i++;
        //    }
        //    List<int> insertList = insertMap.Keys.ToList();
        //    for (int i = num - 1; i >= 0; i--)
        //    {
        //        if (deleteList.Contains(i))
        //        {
        //            newClearedPl.RemoveVertexAt(i);
        //        }
        //        if (insertList.Contains(i))
        //        {
        //            newClearedPl.AddVertexAt(i, insertMap[i].ToPoint2D(), 0, 0, 0);
        //        }
        //    }


        //    //第五筛
        //    deleteList.Clear();
        //    insertMap.Clear();
        //    insertList.Clear();
        //    num = newClearedPl.NumberOfVertices;
        //    for (int i = 0; i < num;)
        //    {
        //        Point3d pt0 = newClearedPl.GetPoint3dAt(i);
        //        var pt1 = newClearedPl.GetPoint3dAt((i + 1) % num);
        //        var pt2 = newClearedPl.GetPoint3dAt((i + 2) % num);
        //        var pt3 = newClearedPl.GetPoint3dAt((i + 3) % num);
        //        var pt4 = newClearedPl.GetPoint3dAt((i + 4) % num);

        //        Vector3d vec1 = pt1 - pt0;
        //        Vector3d vec2 = pt2 - pt1;
        //        Vector3d vec3 = pt3 - pt2;
        //        Vector3d vec4 = pt4 - pt3;

        //        double angle1 = vec1.GetAngleTo(vec3, Vector3d.ZAxis);
        //        bool flag = false;
        //        if (angle1 < 0.3 || angle1 > Math.PI * 2 - 0.3) flag = true;

        //        int find = 0;
        //        double angle = vec1.GetAngleTo(vec2, Vector3d.ZAxis);

        //        if (vec2.Length < value && angle < 1.5 * Math.PI + 0.1 && angle > 1.5 * Math.PI - 0.1 && flag)
        //        {
        //            Line lineStart = new Line(pt0, pt0 + vec1.GetNormal() * Parameter.ClearExtendLength);
        //            Line lineEnd = new Line(pt3 - vec4.GetNormal() * value * 2, pt4 + vec4.GetNormal() * value * 2);
        //            List<Point3d> intersectionList = lineStart.Intersect(lineEnd, Intersect.OnBothOperands);
        //            if (intersectionList.Count > 0)
        //            {
        //                Point3d intersectionPoint = intersectionList.FindByMin(x => x.DistanceTo(pt1));

        //                deleteList.Add((i + 1) % num);
        //                deleteList.Add((i + 2) % num);
        //                deleteList.Add((i + 3) % num);

        //                insertMap.Add((i + 1) % num, intersectionPoint);
        //                find = 1;
        //            }
        //        }
        //        if (find == 1) i = i + 3;
        //        else i++;
        //    }
        //    insertList = insertMap.Keys.ToList();
        //    for (int i = num - 1; i >= 0; i--)
        //    {
        //        if (deleteList.Contains(i))
        //        {
        //            newClearedPl.RemoveVertexAt(i);
        //        }
        //        if (insertList.Contains(i))
        //        {
        //            newClearedPl.AddVertexAt(i, insertMap[i].ToPoint2D(), 0, 0, 0);
        //        }
        //    }

        //    //第六筛
        //    deleteList.Clear();
        //    insertMap.Clear();
        //    insertList.Clear();
        //    num = newClearedPl.NumberOfVertices;
        //    for (int i = num - 1; i >= 0;)
        //    {
        //        Point3d pt0 = newClearedPl.GetPoint3dAt(i);
        //        var pt1 = newClearedPl.GetPoint3dAt((i - 1 + num) % num);
        //        var pt2 = newClearedPl.GetPoint3dAt((i - 2 + num) % num);
        //        var pt3 = newClearedPl.GetPoint3dAt((i - 3 + num) % num);
        //        var pt4 = newClearedPl.GetPoint3dAt((i - 4 + num) % num);

        //        Vector3d vec1 = pt1 - pt0;
        //        Vector3d vec2 = pt2 - pt1;
        //        Vector3d vec3 = pt3 - pt2;
        //        Vector3d vec4 = pt4 - pt3;

        //        double angle1 = vec1.GetAngleTo(vec3, Vector3d.ZAxis);
        //        bool flag = false;
        //        if (angle1 < 0.3 || angle1 > Math.PI * 2 - 0.3) flag = true;

        //        int find = 0;
        //        double angle = vec1.GetAngleTo(vec2, Vector3d.ZAxis);

        //        if (vec2.Length < value && angle < 0.5 * Math.PI + 0.1 && angle > 0.5 * Math.PI - 0.1 && flag)
        //        {
        //            Line lineStart = new Line(pt0, pt0 + vec1.GetNormal() * Parameter.ClearExtendLength);
        //            Line lineEnd = new Line(pt3 - vec4.GetNormal() * value * 2, pt4 + vec4.GetNormal() * value * 2);
        //            List<Point3d> intersectionList = lineStart.Intersect(lineEnd, Intersect.OnBothOperands);
        //            if (intersectionList.Count > 0)
        //            {
        //                Point3d intersectionPoint = intersectionList.FindByMin(x => x.DistanceTo(pt1));

        //                deleteList.Add((i - 1 + num) % num);
        //                deleteList.Add((i - 2 + num) % num);
        //                deleteList.Add((i - 3 + num) % num);

        //                insertMap.Add((i - 3 + num) % num, intersectionPoint);
        //                find = 1;
        //            }
        //        }
        //        if (find == 1) i = i - 3;
        //        else i--;
        //    }
        //    insertList = insertMap.Keys.ToList();
        //    for (int i = num - 1; i >= 0; i--)
        //    {
        //        if (deleteList.Contains(i))
        //        {
        //            newClearedPl.RemoveVertexAt(i);
        //        }
        //        if (insertList.Contains(i))
        //        {
        //            newClearedPl.AddVertexAt(i, insertMap[i].ToPoint2D(), 0, 0, 0);
        //        }
        //    }

        //    //
        //    newClearedPl = ConvertToIntegerPolyline(newClearedPl);

        //    //return
        //    return newClearedPl;
        //}

        //static public void ClearPolyline(ref Polyline newClearedPl, double value = 5)
        //{
        //    if (!newClearedPl.IsCCW()) newClearedPl.ReverseCurve();

        //    List<int> deleteList = new List<int>();
        //    int num = newClearedPl.NumberOfVertices;

        //    //第一筛
        //    //筛除 重合点+距离过段线段
        //    for (int i = 0; i < num; i++)
        //    {
        //        var pt1 = newClearedPl.GetPoint3dAt(i);
        //        //如果超出距离就跳出

        //        var pt2 = newClearedPl.GetPoint3dAt((i + 1) % num);
        //        Vector3d dir2 = pt2 - pt1;
        //        if (dir2.Length < value)
        //        {
        //            deleteList.Add(i);
        //        }
        //    }
        //    for (int i = num - 1; i >= 0; i--)
        //    {
        //        if (deleteList.Contains(i))
        //        {
        //            //if (newClearedPl.NumberOfVertices >= 4)
        //            //{

        //            //}
        //            //else break;

        //            newClearedPl.RemoveVertexAt(i);
        //        }
        //    }

        //    //第二筛
        //    //筛除 直线多余点
        //    deleteList = new List<int>();
        //    num = newClearedPl.NumberOfVertices;
        //    for (int i = 0; i < num; i++)
        //    {
        //        var pt1 = newClearedPl.GetPoint3dAt(i);
        //        //如果超出距离就跳出
        //        var pt0 = newClearedPl.GetPoint3dAt((i + num - 1) % num);
        //        var pt2 = newClearedPl.GetPoint3dAt((i + 1) % num);
        //        Vector3d dir1 = pt1 - pt0;
        //        Vector3d dir2 = pt2 - pt1;
        //        double angle = dir2.GetAngleTo(dir1, Vector3d.ZAxis);
        //        if (angle < 0.1 || angle > 2 * Math.PI - 0.1)
        //        {
        //            deleteList.Add(i);
        //        }
        //    }
        //    for (int i = num - 1; i >= 0; i--)
        //    {
        //        if (deleteList.Contains(i))
        //        {
        //            newClearedPl.RemoveVertexAt(i);
        //        }
        //    }
        //}
    }
}
