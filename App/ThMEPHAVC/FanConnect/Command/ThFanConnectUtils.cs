using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPHVAC.FanConnect.Model;

namespace ThMEPHVAC.FanConnect.Command
{
    public class Point3dEx : IEquatable<Point3dEx>
    {
        public double Tolerance = 1; //mm
        public Point3d _pt;

        public Point3dEx(double tol = 1)
        {
            _pt = new Point3d();
            Tolerance = tol;
        }
        public Point3dEx(Point3d pt, double tol = 1)
        {
            _pt = pt;
            Tolerance = tol;
        }

        public Point3dEx(double x, double y, double z, double tol = 1)
        {
            _pt = new Point3d(x, y, z);
            Tolerance = tol;
        }

        public override int GetHashCode()
        {
            return ((int)_pt.X / 1000).GetHashCode() ^ ((int)_pt.Y / 1000).GetHashCode();
        }
        public bool Equals(Point3dEx other)
        {
            return Math.Abs(other._pt.X - this._pt.X) < Tolerance && Math.Abs(other._pt.Y - this._pt.Y) < Tolerance;
        }
    }
    public class ThFanConnectUtils
    {
        public static Point3dCollection SelectArea()
        {
            using (PointCollector pc = new PointCollector(PointCollector.Shape.Window, new List<string>()))
            {
                try
                {
                    pc.Collect();
                }
                catch
                {
                    return new Point3dCollection();
                }
                Point3dCollection winCorners = pc.CollectedPoints;
                var frame = new Polyline();
                frame.CreateRectangle(winCorners[0].ToPoint2d(), winCorners[1].ToPoint2d());
                frame.TransformBy(Active.Editor.UCS2WCS());
                return frame.Vertices();
            }
        }
        public static Point3d SelectPoint()
        {
            var point1 = Active.Editor.GetPoint("\n请选择水管起点位置\n");
            if (point1.Status != PromptStatus.OK)
            {
                return new Point3d();
            }
            return point1.Value.TransformBy(Active.Editor.UCS2WCS());
        }
        public static List<ThFanCUModel> SelectFanCUModel()
        {
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                var retModeles = new List<ThFanCUModel>();
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "选择要布置的设备",
                    RejectObjectsOnLockedLayers = true,
                };
                var result = Active.Editor.GetSelection(options);

                if (result.Status == PromptStatus.OK)
                {
                    foreach (var obj in result.Value.GetObjectIds())
                    {
                        var entity = acadDb.Element<Entity>(obj);
                        if (entity is BlockReference)
                        {
                            var blk = entity as BlockReference;
                            if (blk.ObjectId.GetDynBlockValue("水管连接点1 X") != null && blk.ObjectId.GetDynBlockValue("水管连接点1 Y") != null)
                            {
                                var tmpFan = new ThFanCUModel();
                                var offset1x = Convert.ToDouble(blk.ObjectId.GetDynBlockValue("水管连接点1 X"));
                                var offset1y = Convert.ToDouble(blk.ObjectId.GetDynBlockValue("水管连接点1 Y"));

                                var offset1 = new Point3d(offset1x, offset1y, 0);
                                var dbcollection = new DBObjectCollection();
                                blk.Explode(dbcollection);
                                dbcollection = dbcollection.OfType<Entity>().Where(O => O is Curve).ToCollection();

                                tmpFan.FanPoint = offset1.TransformBy(blk.BlockTransform);
                                tmpFan.FanObb = dbcollection.GetMinimumRectangle();
                                retModeles.Add(tmpFan);
                            }
                        }
                    }
                }
                return retModeles;
            }
        }
        public static List<Line> SelectPipes()
        {
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                List<Line> retLines = new List<Line>();
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "选择要布置的水管",
                    RejectObjectsOnLockedLayers = true,
                };
                var result = Active.Editor.GetSelection(options);
                if (result.Status == PromptStatus.OK)
                {
                    foreach (var obj in result.Value.GetObjectIds())
                    {
                        var entity = acadDb.Element<Entity>(obj);
                        if(entity is Polyline)
                        {
                            var line = entity as Polyline;
                            if(line.Layer.Contains("AI-水管路由"))
                            {
                                retLines.AddRange(line.ToLines());
                            }
                        }
                        else if(entity is Line)
                        {
                            var line = entity as Line;
                            if(line.Layer.Contains("AI-水管路由"))
                            {
                                retLines.Add(line);
                            }
                        }
                    }
                }
                return retLines;
            }
        }
        public static List<Line> GetNearbyLine(Point3d pt, List<Line> lines, int N = 3)
        {
            List<Line> returnLines = new List<Line>();
            if (lines.Count <= N)
            {
                return lines;
            }

            lines = lines.OrderBy(o => DistanceToPoint(o,pt)).ToList();
            for (int i = 0; i < N; i++)
            {
                returnLines.Add(lines[i]);
            }
            return returnLines;
        }
        public static double DistanceToPoint(Line l ,Point3d pt,bool isExtend = false)
        {
            Point3d closestPoint = l.GetClosestPointTo(pt, isExtend);
            return pt.DistanceTo(closestPoint);
        }
        public static Polyline CreateMapFrame(Line line,Point3d pt, double expandLength)
        {
            List<Point3d> pts = new List<Point3d>();
            pts.Add(pt);
            pts.Add(line.GetClosestPointTo(pt, false));

            Polyline polyLine = new Polyline();
            polyLine.AddVertexAt(0, pts[0].ToPoint2D(), 0, 0, 0);
            polyLine.AddVertexAt(0, pts[1].ToPoint2D(), 0, 0, 0);
            var objcet = polyLine.BufferPL(expandLength)[0];
            return objcet as Polyline;
        }
        /// <summary>
        /// 判断是否和外框线相交
        /// </summary>
        /// <param name="line"></param>
        /// <param name="frame"></param>
        /// <returns></returns>
        public static bool CheckIntersectWithFrame(Curve line, Polyline frame)
        {
            return frame.IsIntersects(line);
        }
        /// <summary>
        /// 用nts的selectCrossing计算是否相交
        /// </summary>
        /// <returns></returns>
        public static bool LineIntersctBySelect(List<Polyline> polylines, Polyline line, double bufferWidth)
        {
            DBObjectCollection dBObject = new DBObjectCollection() { line };
            foreach (Polyline polyline in dBObject.Buffer(bufferWidth))
            {
                if (SelelctCrossing(polylines, polyline).Count > 0)
                {
                    return true;
                }
            }

            return false;
        }
        public static bool LineIntersctBySelect(List<Line> lines, Polyline pl)
        {
            foreach (var l in lines)
            {
                if (l.IsIntersects(pl))
                {
                    return true;
                }
            }
            return false;
        }
        public static List<Polyline> SelelctCrossing(List<Polyline> polylines, Polyline polyline)
        {
            var objs = polylines.ToCollection();
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var resHoles = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Polyline>().ToList();

            return resHoles;
        }
        public static List<Line> CleanLaneLines(List<Line> lines)
        {
            var rstLines = new List<Line>();

            //Grouping
            var lineSegs = lines.Select(l => new LineSegment2d(l.StartPoint.ToPoint2D(), l.EndPoint.ToPoint2D())).ToList();
            List<HashSet<LineSegment2d>> lineSegGroups = new List<HashSet<LineSegment2d>>();

            while (lineSegs.Count() != 0)
            {
                var tmpLineSeg = lineSegs.First();
                bool alreadyContains = false;
                foreach (var g in lineSegGroups)
                {
                    if (g.Contains(tmpLineSeg))
                    {
                        alreadyContains = true;
                        break;
                    }
                }

                if (alreadyContains) continue;

                var colinerSegs = lineSegs.Where(l => l.IsParallelTo(tmpLineSeg, new Tolerance(0.001, 0.001))).ToHashSet();
                lineSegGroups.Add(colinerSegs);
                lineSegs = lineSegs.Except(colinerSegs).ToList();
            }

            foreach (var lg in lineSegGroups)
            {
                rstLines.AddRange(MergeGroupLines(lg));
            }

            return rstLines;
        }
        private static List<Line> MergeGroupLines(HashSet<LineSegment2d> lineGroup)
        {
            var rstLines = new List<Line>();
            while (lineGroup.Count != 0)
            {
                var l = lineGroup.First();
                lineGroup.Remove(l);
                rstLines.Add(MergeLine(ref l, ref lineGroup));
            }
            return rstLines;

        }
        private static Line MergeLine(ref LineSegment2d l, ref HashSet<LineSegment2d> lineGroup)
        {
            Line rstLine = new Line();

            MergeLineEx(ref l, ref lineGroup);
            rstLine.StartPoint = l.StartPoint.ToPoint3d();
            rstLine.EndPoint = l.EndPoint.ToPoint3d();
            return rstLine;
        }
        private static void MergeLineEx(ref LineSegment2d l, ref HashSet<LineSegment2d> lineGroup)
        {
            //如果 l 与 group里面任何一条线都没有交点，那么就把该l返回
            var overlapLine = IsOverlapLine(l, lineGroup);
            if (overlapLine.Count == 0)//如果没有相交
            {
                return;
            }
            else
            {
                //找到与l相交的线，然后，进行merge,并且把相交的线，从group里面删除
                l = MergeLineEX2(l, overlapLine);
                foreach (var line in overlapLine)
                {
                    lineGroup.Remove(line);
                }
                //merge 以后，继续执行MergeLine;
                MergeLineEx(ref l, ref lineGroup);
            }
        }
        private static HashSet<LineSegment2d> IsOverlapLine(LineSegment2d line, HashSet<LineSegment2d> lineGroup)
        {
            HashSet<LineSegment2d> overlapLine = new HashSet<LineSegment2d>();
            foreach (var l in lineGroup)
            {
                if (IsOverlapLine(line, l))
                {
                    overlapLine.Add(l);
                }
            }

            return overlapLine;
        }
        private static bool IsOverlapLine(LineSegment2d firLine, LineSegment2d secLine)
        {
            var overlapedSeg = firLine.Overlap(secLine, new Tolerance(0.01, 0.01));
            if (overlapedSeg != null)
            {
                return true;
            }
            else
            {
                var ptSet = new HashSet<Point3dEx>();
                var tol = 1E-2;
                ptSet.Add(new Point3dEx(firLine.StartPoint.X, firLine.StartPoint.Y, 0.0, tol));
                ptSet.Add(new Point3dEx(firLine.EndPoint.X, firLine.EndPoint.Y, 0.0, tol));
                ptSet.Add(new Point3dEx(secLine.StartPoint.X, secLine.StartPoint.Y, 0.0, tol));
                ptSet.Add(new Point3dEx(secLine.EndPoint.X, secLine.EndPoint.Y, 0.0, tol));
                if (ptSet.Count() == 3)
                {
                    return true;
                }
            }
            return false;
        }
        private static LineSegment2d MergeLineEX2(LineSegment2d line, HashSet<LineSegment2d> overlapLines)
        {
            List<Point3d> pts = new List<Point3d>();
            pts.Add(line.StartPoint.ToPoint3d());
            pts.Add(line.EndPoint.ToPoint3d());
            foreach (var l in overlapLines)
            {
                pts.Add(l.StartPoint.ToPoint3d());
                pts.Add(l.EndPoint.ToPoint3d());
            }
            var pairPt = pts.GetCollinearMaxPts();
            return new LineSegment2d(pairPt.Item1.ToPoint2d(), pairPt.Item2.ToPoint2d());
        }
        public static double GetDoubleFromString(string power)
        {
            var str = Regex.Replace(power, @"[^\d.\d]", "");
            double resDouble = double.Parse(str);
            return resDouble;
        }
        public static void GetCoolAndHotCapacity(string capacity, out double cool, out double hot)
        {
            var str = capacity.Split('/');
            cool = GetDoubleFromString(str[0]);
            hot = GetDoubleFromString(str[1]);
        }
        public static void GetCoolAndHotTempDiff(string tempDiff, out double cool, out double hot)
        {
            var str = tempDiff.Split('/');
            cool = GetDoubleFromString(str[0]);
            hot = GetDoubleFromString(str[1]);
        }
        public static double GetVectorAngle(Vector3d vector)
        {
            Vector3d basVector = new Vector3d(1, 0, 0);
            Vector3d refVector = new Vector3d(0, 0, 1);
            double retAngle = basVector.GetAngleTo(vector, refVector);
            return retAngle;
        }
        public static void EnsureLayerOn(AcadDatabase acadDb, string layer)
        {
            acadDb.Database.UnFrozenLayer(layer);
            acadDb.Database.UnLockLayer(layer);
            acadDb.Database.UnOffLayer(layer);
        }
        public static void FindFourWay(ThFanTreeNode<ThFanPipeModel> node)
        {
            foreach (var item in node.Children)
            {
                FindFourWay(item);
            }

            if (node.Children.Count <= 1)
            {
                return;
            }
            var connectChild = node.Children.Where(o => o.Item.IsConnect).ToList();
            var nonConnectChild = node.Children.Where(o => !o.Item.IsConnect).ToList();
            if (connectChild.Count == 2)
            {
                connectChild[0].Item.WayCount = 3;
                connectChild[0].Item.BrotherItem = connectChild[1].Item;
                connectChild[1].Item.WayCount = 3;
                connectChild[1].Item.BrotherItem = connectChild[0].Item;
            }
            for (int i = 0; i < nonConnectChild.Count; i++)
            {
                for (int j = 0; j < nonConnectChild.Count; j++)
                {
                    if (i != j)
                    {
                        if (nonConnectChild[i].Item.PLine.StartPoint.IsEqualTo(nonConnectChild[j].Item.PLine.StartPoint))
                        {
                            nonConnectChild[i].Item.BrotherItem = nonConnectChild[j].Item;
                            nonConnectChild[i].Item.WayCount = 4;
                        }
                    }
                }
            }

        }
        public static void FindFcuNode(ThFanTreeNode<ThFanPipeModel> node, Point3d pt)
        {
            var box = node.Item.PLine.ExtendLine(10).Buffer(10);

            if (box.Contains(pt))
            {
                node.Item.PipeWidth = 100.0;
                node.Item.PipeLevel = PIPELEVEL.LEVEL3;
                return;
            }

            foreach (var item in node.Children)
            {
                FindFcuNode(item, pt);
            }
        }
    }
}
