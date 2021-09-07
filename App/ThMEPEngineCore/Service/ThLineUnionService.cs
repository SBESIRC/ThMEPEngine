using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Buffer;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;

namespace ThMEPEngineCore.Service
{
    public static class ThLineUnionService
    {
        public static bool Overlap(Line firstline, Line secondline)
        {
            var angle = Math.Min(firstline.Delta.GetAngleTo(secondline.Delta), Math.PI - firstline.Delta.GetAngleTo(secondline.Delta));
            if (angle > 1.0 / 180 * Math.PI)
                return false;
            if (Point3dEuqal(firstline.StartPoint,secondline.StartPoint))
                return true;
            if (Point3dEuqal(firstline.EndPoint, secondline.StartPoint))
                return true;
            if (Point3dEuqal(firstline.StartPoint, secondline.EndPoint))
                return true;
            if (Point3dEuqal(firstline.EndPoint, secondline.EndPoint))
                return true;
            bool lap = ThGeometryTool.IsOverlap(firstline.StartPoint, firstline.EndPoint, secondline.StartPoint, secondline.EndPoint) 
                        && ThGeometryTool.IsCollinearEx(firstline.StartPoint, firstline.EndPoint, secondline.StartPoint, secondline.EndPoint);
            if (lap)
                return true;
            return false;
        }
        public static Line UnionOverlapLine(Line firstline, Line secondline)
        {
            //>>>排序的精度问题如何处理才更好
            //利用投影，减小精度的干扰
            Point3d p1 = ThGeometryTool.GetProjectPtOnLine(secondline.StartPoint, firstline.StartPoint, firstline.EndPoint);
            Point3d p2 = ThGeometryTool.GetProjectPtOnLine(secondline.EndPoint, firstline.StartPoint, firstline.EndPoint);
            var pts = new List<Point3d>()
            {
                firstline.StartPoint,firstline.EndPoint,p1,p2
            };
            //>>>暂时取整表示吧
            //pts = pts.OrderBy(o => Math.Round(o.X,0)).ThenBy(o => Math.Round(o.Y,0)).ToList();
            var tuple = ThGeometryTool.GetCollinearMaxPts(pts);
            return new Line(tuple.Item1, tuple.Item2);
        }
        public static Line NormalizeLaneLine(Line line, double tolerance = 1.0)
        {
            var newLine = new Line(line.StartPoint, line.EndPoint);
            if (Math.Abs(line.StartPoint.Y - line.EndPoint.Y) <= tolerance)
            {
                //近似沿X轴
                if (line.StartPoint.X < line.EndPoint.X)
                {
                    newLine = new Line(line.StartPoint, line.EndPoint);
                }
                else
                {
                    newLine = new Line(line.EndPoint, line.StartPoint);
                }
            }
            else if (Math.Abs(line.StartPoint.X - line.EndPoint.X) <= tolerance)
            {
                //此线是近似沿Y轴
                if (line.StartPoint.Y < line.EndPoint.Y)
                {
                    newLine = new Line(line.StartPoint, line.EndPoint);
                }
                else
                {
                    newLine = new Line(line.EndPoint, line.StartPoint);
                }
            }
            else
            {
                newLine = newLine.Normalize();
            }
            return newLine;
        }
        public static bool Point3dEuqal(Point3d p1, Point3d p2, double tolerance = 0.1)
        {
            if (!ThAuxiliaryUtils.DoubleEquals(p1.X, p2.X, tolerance))
                return false;
            if (!ThAuxiliaryUtils.DoubleEquals(p1.Y, p2.Y, tolerance))
                return false;
            return true;
        }
        public static List<Line> UnionLineList(List<Line> lines)
        {
            var normalizeobjs = new List<Line>();
            var refvec = new Vector3d(1.0, 0.0, 0.0);
            lines.ForEach(o => normalizeobjs.Add(NormalizeLaneLine(o)));
            normalizeobjs = normalizeobjs.OrderBy(o => Math.Round(o.Delta.GetAngleTo(refvec), 2))
                                        .ThenBy(o => Math.Round(o.StartPoint.X, 1))
                                        .ThenBy(o => Math.Round(o.StartPoint.Y, 1)).ToList();
            var res = new List<Line>();
            if (normalizeobjs.Count == 0)
                return res;
            res.Add(normalizeobjs[0]);
            for (int i = 1; i < normalizeobjs.Count; i++)
            {
                bool isolated = true;
                for (int j = 0; j < res.Count; j++)
                {
                    if (Overlap(res[j], normalizeobjs[i]))
                    {
                        var temp = res[j];
                        res.Remove(res[j]);
                        var ans = UnionOverlapLine(temp, normalizeobjs[i]);
                        if (ans.Length > 1.0)
                        {
                            res.Add(ans);
                            isolated = false;
                            break;
                        }
                    }
                }
                if (isolated && normalizeobjs[i].Length > 1.0)
                    res.Add(normalizeobjs[i]);
            }
            return res;
        }
    }

    public class ThListLineMerge
    {
        private const double MergeTolerance = 501.0; //两根平行线间距
        private const double OverlapTolerance = 501.0; //两根平行线首尾间距
        private const double AngleTolerance = 3.0 * Math.PI / 180; //如果没有平行，则认为在夹角小于3度的算平行
        private const double ShortLineTolerance = 500.0; //分支线，且末端未连接任何线，小于此长度的丢弃
        public List<Line> Lines { get; set; }
        public ThListLineMerge(List<Line> linelist)
        {
            Lines = new List<Line>();
            linelist.ForEach(o => Lines.Add(ThLineUnionService.NormalizeLaneLine(o)));
        }

        public bool Needtomerge(out Line refline,out Line tomoveline)
        {
            refline = new Line();
            tomoveline = new Line();
            bool res = false;
            for (int i = 0; i < Lines.Count - 1; i++)
                for (int j = i+1 ; j < Lines.Count; j++)
                {
                    if (Isparallel(Lines[i], Lines[j]) && Paralledlinedistance(Lines[i], Lines[j]) < MergeTolerance 
                        && Parallellinesisoverlap(Lines[i],Lines[j])) 
                    {
                        if(Lines[i].Length>Lines[j].Length)
                        {
                            //移动规则：移动较短的平行线
                            refline = Lines[i];
                            tomoveline = Lines[j];
                        }
                        else
                        {
                            refline = Lines[j];
                            tomoveline = Lines[i];
                        }
                        return true;
                    }
                }
            return res;
        }
        private bool Isparallel(Line l1,Line l2)
        {
            return l1.Delta.GetAngleTo(l2.Delta) < AngleTolerance;
        }

        private double Paralledlinedistance(Line l1,Line l2)
        {
            Vector3d crossvec = l1.StartPoint.GetVectorTo(l2.StartPoint);
            Vector3d norm = l1.StartPoint.GetVectorTo(l1.EndPoint).GetNormal();
            return Math.Sqrt(Math.Pow(crossvec.Length, 2) - Math.Pow(crossvec.DotProduct(norm), 2));
        }

        private bool Parallellinesisoverlap(Line l1,Line l2)
        {
            if (ThGeometryTool.IsOverlap(l1.StartPoint, l1.EndPoint, l2.StartPoint, l2.EndPoint))
                return true;
            Vector3d v1 = l1.StartPoint.GetVectorTo(l2.EndPoint);
            Vector3d v2 = l1.EndPoint.GetVectorTo(l2.StartPoint);
            Vector3d v = v1.Length < v2.Length ? v1 : v2;
            Vector3d norm = l1.StartPoint.GetVectorTo(l1.EndPoint).GetNormal();
            if (Math.Abs(v.DotProduct(norm)) < OverlapTolerance)
                return true;
            return false;
        }

        private Polyline CreateRectangle(Line line)
        {
            BufferParameters bufferpar = new BufferParameters() { EndCapStyle = EndCapStyle.Flat, JoinStyle = NetTopologySuite.Operation.Buffer.JoinStyle.Mitre };
            var res = line.ToNTSLineString().Buffer((MergeTolerance - 1) / 2, bufferpar).ToDbCollection()[0] as Polyline;
            return res.Buffer(1.0)[0] as Polyline;
        }

        public void Domoveparallellines(Line refline,Line tomoveline)
        {
            List<Tuple<Line, Point3d>> intersectpts = new List<Tuple<Line, Point3d>>();
            Lines.ForEach(o => 
            {
                var pts = new Point3dCollection();
                tomoveline.IntersectWith(o, Intersect.OnBothOperands, pts, IntPtr.Zero, IntPtr.Zero);
                if(pts.Count==1)
                {
                    intersectpts.Add(Tuple.Create(o, pts[0]));
                }
            });
            //构建移动tomoveline后，并与refline合并后的线
            //1. 构建方向向量的法向，进而创建变换矩阵
            Vector3d crossvec = tomoveline.StartPoint.GetVectorTo(refline.StartPoint);
            Vector3d normdir = tomoveline.StartPoint.GetVectorTo(tomoveline.EndPoint).GetNormal();
            Vector3d norm = crossvec - (crossvec.DotProduct(normdir)) * normdir;
            Matrix3d transform = new Matrix3d(new double[]{  1.0, 0.0, 0.0, norm.X,
                                                             0.0, 1.0, 0.0, norm.Y,
                                                             0.0, 0.0, 1.0, norm.Z,
                                                             0.0, 0.0, 0.0, 1.0 });
            //创建合并的线，以及对应的矩形框
            tomoveline.TransformBy(transform);
            Line mergeline = ThLineUnionService.UnionOverlapLine(refline, tomoveline);
            var rec = CreateRectangle(mergeline);
            //2. 对于在tomoveline上的点及线，筛选哪些需要进行处理
            //首先对一些线本来就与要移动的线相交的进行处理。处理规则：
            //如果该线与矩形框的交点小于2个，则将距离原来交点最近的端点改为平移后的端点
            //如果该线与矩形框的交点为2个以上，则不进行处理。
            //删除两个需要合并的线，并将新合并的线加入列表
            //以防万一，再次将线进行简单的Union合并
            intersectpts.ForEach(o => 
            {
                var pts = new Point3dCollection();
                o.Item1.IntersectWith(rec, Intersect.OnBothOperands, pts, IntPtr.Zero, IntPtr.Zero);
                if(pts.Count<2)
                {
                    var linepoints = Changenearestpoint(o.Item1, o.Item2);
                    linepoints[0]= o.Item2.TransformBy(transform);
                    Lines.Remove(o.Item1);
                    Lines.Add(new Line(linepoints[0], linepoints[1]));
                }
            });
            Lines.Remove(refline);
            Lines.Remove(tomoveline);
            Lines.Add(mergeline);
            CleanZeroLines();
            Lines = ThLineUnionService.UnionLineList(Lines);
        }

        /// <summary>
        /// 第一个为近点，第二个为远点
        /// </summary>
        /// <param name="line"></param>
        /// <param name="interscetpt"></param>
        /// <returns></returns>
        private List<Point3d> Changenearestpoint(Line line, Point3d interscetpt)
        {
            Point3d near = line.StartPoint.DistanceTo(interscetpt) > line.EndPoint.DistanceTo(interscetpt) ? line.EndPoint : line.StartPoint;
            Point3d far = ThLineUnionService.Point3dEuqal(near, line.StartPoint) ? line.EndPoint : line.StartPoint;
            return new List<Point3d>() { near, far };
        }

        private void CleanZeroLines()
        {
            Lines = Lines.Where(o => o.Length > 1.0).ToList();
        }

        private List<Line> NodingLines(DBObjectCollection curves)
        {
            var results = new List<Line>();
            var geometry = curves.ToNTSNodedLineStrings();
            if (geometry is LineString line)
            {
                results.Add(line.ToDbline());
            }
            else if (geometry is MultiLineString Lines)
            {
                results.AddRange(Lines.Geometries.Cast<LineString>().Select(o => o.ToDbline()));
            }
            else
            {
                throw new NotSupportedException();
            }
            return results;
        }

        public void Simplifierlines()
        {
            // 首先去掉本来就存在的短线，然后再去掉过长的线头
            Lines = Lines.Where(o => o.Length > ShortLineTolerance).ToList();
            var extendline = new List<Line>();
            Lines.ForEach(o => extendline.Add(o.ExtendLine(0.5)));
            Lines = NodingLines(extendline.ToCollection());
            Lines = Lines.Where(o => o.Length > ShortLineTolerance).ToList();
            Lines = ThLineUnionService.UnionLineList(Lines);
        }
    }
}
