using System;
using System.Linq;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;

namespace ThMEPEngineCore.Service
{
    public class ThRectangleConverter
    {
        private Polyline Poly { get; set; }
        private List<Line> Edges { get; set; }
        public ThRectangleConverter(Polyline poly)
        {
            Poly = poly != null ? poly : new Polyline();
            Edges = poly.GetEdges();
            Edges=Edges.Where(o => o.Length > 1e-4).ToList();
        }
        public bool IsClosed()
        {
            if(Poly.Closed)
            {
                return true;
            }
            else
            {
                if(Poly.GetPoint3dAt(0).DistanceTo(Poly.GetPoint3dAt(Poly.NumberOfVertices-1))<=1.0)
                {                    
                    return true;
                }
            }
            return false;
        }

        public Polyline ConvertRectangle1()
        {
            // 对于一个多边形，只有两条邻边是直角边
            // 其它的边投影到直角边上的线段都在直角边内
            var result = new Polyline();
            if (IsClosed()==false)
            {
                return result;
            }
            var pairIndexes = FindRightAngleSide();            
            if (pairIndexes.Count == 0)
            {
                return result;
            }
            foreach(var pairIndex in pairIndexes)
            {
                Tuple<Line, Line> sides;
                if(CanBuildRectangle(pairIndex,out sides))
                {
                    result = BuildRectangle1(sides);
                    break;
                }
            }            
            return result;
        }



        public Polyline ConvertRectangle2()
        {
            // 对于一个多边形，只要有两条邻边是直角边
            // 生成一个长方形
            if (IsClosed() == false)
            {
                return new Polyline();
            }
            var pairIndexes = FindRightAngleSide();
            if (pairIndexes.Count == 0)
            {
                return new Polyline();
            }
            var pairIndex =pairIndexes
                .Where(o=> Edges[o.Item1].Length>1.0 && Edges[o.Item2].Length > 1.0)
                .OrderByDescending(o => Edges[o.Item1].Length + Edges[o.Item2].Length)
                .First();
            return BuildRectangle2(pairIndex);
        }

        public Polyline ConvertRectangle3()
        {
            // OBB
            if(this.Poly.Area==0.0 || !IsClosed())
            {
                return new Polyline();
            }
            return ThCADCore.NTS.ThCADCoreNTSPolylineExtension.GetMinimumRectangle(this.Poly);
        }

        private Polyline BuildRectangle1(Tuple<Line, Line> sides)
        {
            var rectangle = new Polyline()
            {
                Closed = true
            };
            var rightSide1 = sides.Item1;
            var rightSide2 = sides.Item2;

            var pts = new Point3dCollection();
            rightSide1.IntersectWith(rightSide2, Intersect.ExtendBoth, pts, IntPtr.Zero, IntPtr.Zero);
            if (pts.Count == 1)
            {
                var pt1 = pts[0];
                var pt2 = pt1.DistanceTo(rightSide1.StartPoint) > pt1.DistanceTo(rightSide1.EndPoint)
                    ? rightSide1.StartPoint : rightSide1.EndPoint;
                var pt4 = pt1.DistanceTo(rightSide2.StartPoint) > pt1.DistanceTo(rightSide2.EndPoint)
                    ? rightSide2.StartPoint : rightSide2.EndPoint;
                var midPt = ThGeometryTool.GetMidPt(pt2, pt4);
                var pt3 = pt1 + pt1.GetVectorTo(midPt).GetNormal().MultiplyBy(pt1.DistanceTo(midPt) * 2.0);
                rectangle=ThDrawTool.CreatePolyline(new Point3dCollection { pt1, pt2, pt3, pt4 });
            }
            return rectangle;
        }
        private Polyline BuildRectangle2(Tuple<int, int> rightSideIndex)
        {
            var rectangle = new Polyline()
            {
                Closed = true
            };
            var rightSide1 = Edges[rightSideIndex.Item1];
            var rightSide2 = Edges[rightSideIndex.Item2];
            var pts = new Point3dCollection();
            rightSide1.IntersectWith(rightSide2, Intersect.ExtendBoth, pts, IntPtr.Zero, IntPtr.Zero);
            if (pts.Count == 1)
            {
                var pt1 = pts[0];
                var pt2 = pt1.DistanceTo(rightSide1.StartPoint) > pt1.DistanceTo(rightSide1.EndPoint)
                    ? rightSide1.StartPoint : rightSide1.EndPoint;
                var pt4 = pt1.DistanceTo(rightSide2.StartPoint) > pt1.DistanceTo(rightSide2.EndPoint)
                    ? rightSide2.StartPoint : rightSide2.EndPoint;

                var polyPts = new List<Point3d>();
                Edges.ForEach(o =>
                {
                    polyPts.Add(o.StartPoint);
                    polyPts.Add(o.EndPoint);
                });                

                var u = pt1.GetVectorTo(pt2).GetNormal();
                var p = pt1.GetVectorTo(pt4).GetNormal();
                var v = u.CrossProduct(p);                
                var plane = new Plane(pt1, u, v);

                var mt1 = Matrix3d.WorldToPlane(plane);
                var mt2 = Matrix3d.PlaneToWorld(plane);     

                var transPts= polyPts.Select(o => o.TransformBy(mt1)).ToList();
                var minX = transPts.Select(o => o.X).OrderBy(o => o).First();
                var maxX = transPts.Select(o => o.X).OrderByDescending(o => o).First();
                var minY = transPts.Select(o => o.Z).OrderBy(o => o).First();
                var maxY = transPts.Select(o => o.Z).OrderByDescending(o => o).First();

                
                var ptCol = new Point3dCollection();
                ptCol.Add(new Point3d(minX, 0, minY).TransformBy(mt2));
                ptCol.Add(new Point3d(maxX, 0, minY).TransformBy(mt2));
                ptCol.Add(new Point3d(maxX, 0, maxY).TransformBy(mt2));
                ptCol.Add(new Point3d(minX, 0, maxY).TransformBy(mt2));
                rectangle = ThDrawTool.CreatePolyline(ptCol);
                plane.Dispose();
            }
            return rectangle;
        }
        private bool CanBuildRectangle(Tuple<int, int> rightSideIndex,out Tuple<Line,Line> sides)
        {
            var rightSide1 = FindPreLine(rightSideIndex.Item1);
            var rightSide2 = FindNextLine(rightSideIndex.Item2);
            sides = Tuple.Create(rightSide1, rightSide2);

            var nonRightEdges = new List<Line>();
            for(int i = 0;i< Edges.Count;i++)
            {
                if(i!= rightSideIndex.Item1 && i != rightSideIndex.Item2)
                {
                    nonRightEdges.Add(Edges[i]);
                }
            }

            for(int i=0;i< nonRightEdges.Count;i++)
            {
                bool res = JudgeLineProjectionOnOtherLine(rightSide1, nonRightEdges[i]) &&
                JudgeLineProjectionOnOtherLine(rightSide2, nonRightEdges[i]);

            }

            return nonRightEdges
                .Where(o => 
                JudgeLineProjectionOnOtherLine(rightSide1, o) &&
                JudgeLineProjectionOnOtherLine(rightSide2, o))
                .Count() == nonRightEdges.Count;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseLine">基准线</param>
        /// <param name="targetLine">要投影到基准线上的线</param>
        /// <returns></returns>

        private bool JudgeLineProjectionOnOtherLine(Line baseLine,Line targetLine)
        {
            // 判断targetLine在baseLine上的投影线是否在baseLine内
            var extend = baseLine.ExtendLine(5.0);
            return ThGeometryTool.IsProjectionPtInLine(extend.StartPoint, extend.EndPoint, targetLine.StartPoint) &&
                ThGeometryTool.IsProjectionPtInLine(extend.StartPoint, extend.EndPoint, targetLine.EndPoint);
        }
        private List<Tuple<int, int>> FindRightAngleSide()
        {
            var pairIndexes = new List<Tuple<int, int>>();
            for (int i = 1; i <= Edges.Count; i++)
            {
                int index = i % Edges.Count;
                if (Edges[index].IsVertical(Edges[i - 1]))
                {
                    pairIndexes.Add(Tuple.Create(i - 1, index));
                }
            }
            return pairIndexes;
        }

        private Line FindPreLine(int index)
        {
            var preIndex = index;
            var lines = new List<Line>() { Edges[index] };
            do
            {
                preIndex = (--preIndex + Edges.Count) % Edges.Count;
                if(ThMEPNTSExtension.IsLooseCollinear(Edges[index].StartPoint, 
                    Edges[index].EndPoint, Edges[preIndex].StartPoint, Edges[preIndex].EndPoint))
                {
                    lines.Add(Edges[preIndex]);
                }
                else
                {
                    break;
                }
            }
            while (preIndex!= index);
            var rangePts= lines.GetCollinearMaxPts();
            return new Line(rangePts.Item1, rangePts.Item2);
        }
        private Line FindNextLine(int index)
        {
            var nextIndex = index;
            var lines = new List<Line>() { Edges[index] };
            do
            {
                nextIndex = ++nextIndex % Edges.Count;
                if (ThMEPNTSExtension.IsLooseCollinear(Edges[index].StartPoint,
                    Edges[index].EndPoint, Edges[nextIndex].StartPoint, Edges[nextIndex].EndPoint))
                {
                    lines.Add(Edges[nextIndex]);
                }
                else
                {
                    break;
                }
            }
            while (nextIndex != index);
            var rangePts = lines.GetCollinearMaxPts();
            return new Line(rangePts.Item1, rangePts.Item2);
        }
    }
}
