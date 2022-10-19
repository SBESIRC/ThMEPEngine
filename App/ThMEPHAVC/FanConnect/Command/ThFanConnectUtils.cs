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
        public static List<ThFanCUModel> SelectFanCUModel(int sysType)
        {
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                var retModeles = new List<ThFanCUModel>();
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "选择要连接的设备",
                    RejectObjectsOnLockedLayers = true,
                };
                var result = Active.Editor.GetSelection(options);

                if (result.Status == PromptStatus.OK)
                {
                    foreach (var obj in result.Value.GetObjectIds())
                    {
                        var entity = acadDb.Element<Entity>(obj);
                        if (entity is BlockReference blk)
                        {
                            var blkName = blk.GetEffectiveName();
                            if (sysType == 0)//水系统
                            {
                                if (blkName == "AI-FCU(两管制)" ||
                                    blkName == "AI-FCU(四管制)" ||
                                    blkName == "AI-吊顶式空调箱")
                                {
                                    retModeles.Add(GetFanFromBlockReference(blk));
                                }
                                else if (blkName == "AI-水管断线")
                                {
                                    retModeles.Add(GetFanFromBlockReference(blk));
                                }
                            }
                            else if (sysType == 1)//冷媒系统
                            {
                                if (blkName == "AI-中静压VRF室内机(风管机)" ||
                                    blkName == "AI-VRF室内机(四面出风型)")
                                {
                                    retModeles.Add(GetFanFromBlockReference(blk));
                                }
                            }
                        }
                    }
                }
                return retModeles;
            }
        }
        public static List<Line> GetNearbyLine(Point3d pt, List<Line> lines, int N = 3)
        {
            List<Line> returnLines = new List<Line>();
            if (lines.Count <= N)
            {
                return lines;
            }

            lines = lines.OrderBy(o => DistanceToPoint(o, pt)).ToList();
            for (int i = 0; i < N; i++)
            {
                returnLines.Add(lines[i]);
            }
            return returnLines;
        }
        public static double DistanceToPoint(Line l, Point3d pt, bool isExtend = false)
        {
            Point3d closestPoint = l.GetClosestPointTo(pt, isExtend);
            return pt.DistanceTo(closestPoint);
        }
        public static Polyline CreateMapFrame(Line line, Point3d pt, double expandLength)
        {
            var clostPt = line.GetClosestPointTo(pt, false);

            if (pt.DistanceTo(clostPt) < 10.0)
            {
                var polyLine = ThDrawTool.CreateSquare(pt, 10000.0);
                return polyLine;
            }
            else
            {
                List<Point3d> pts = new List<Point3d>();
                pts.Add(pt);
                pts.Add(clostPt);
                Polyline polyLine = new Polyline();
                polyLine.AddVertexAt(0, pts[0].ToPoint2D(), 0, 0, 0);
                polyLine.AddVertexAt(1, pts[1].ToPoint2D(), 0, 0, 0);
                var objcet = polyLine.BufferPL(expandLength)[0];
                return objcet as Polyline;
            }
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
        private static List<Polyline> SelelctCrossing(List<Polyline> polylines, Polyline polyline)
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
        private static double GetDoubleFromString(string power)
        {
            double resDouble = 0;
            var reg = new Regex(@"[0-9]*[.]?[0-9]+");

            var str = reg.Match(power);
            if (str.Success)
            {
                resDouble = double.Parse(str.Value);
            }
            return resDouble;
        }
        private static void GetCoolAndHotCapacity(string capacity, out double cool, out double hot)
        {
            var str = capacity.Split('/');
            cool = GetDoubleFromString(str[0]);
            hot = GetDoubleFromString(str[1]);
        }
        private static void GetCoolAndHotTempDiff(string tempDiff, out double cool, out double hot)
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
        private static void EnsureLayerOn(AcadDatabase acadDb, string layer)
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
        public static bool IsContains(Line l1, Line l2)
        {
            var box = l1.ExtendLine(10).Buffer(10);
            if (box.Contains(l2.StartPoint) && box.Contains(l2.EndPoint))
            {
                return true;
            }
            return false;
        }
        public static void UpdateFan(ThFanCUModel fan, double width)
        {
            if (fan.FanType != "AI-水管断线")
            {
                return;
            }
            using (var database = AcadDatabase.Active())
            {
                fan.FanData.UpgradeOpen();
                fan.FanData.ObjectId.SetDynBlockValue("断线宽度", width);//获得动态块的所有动态属性
                fan.FanData.DowngradeOpen();
            }
        }
        public static void FindFcuNode(ThFanTreeNode<ThFanPipeModel> node, ThFanCUModel fan)
        {
            foreach (var item in node.Children)
            {
                FindFcuNode(item, fan);
            }
            if (node.Children.Count == 0)
            {
                var closetPt = fan.FanObb.GetClosestPointTo(node.Item.PLine.EndPoint, false);
                if (fan.FanPoint.DistanceTo(node.Item.PLine.EndPoint) < 400.0)
                {
                    node.Item.ConnectFan = fan;

                    if (fan.CoolFlow != 0 || fan.HotFlow != 0 || fan.CoolCapa != 0)
                    {
                        fan.IsConnected = true;
                        node.Item.PipeWidth = 100.0;
                        if (fan.FanType == "AI-水管断线")
                        {
                            node.Item.PipeWidth = 300.0;
                        }
                        node.Item.PipeLevel = PIPELEVEL.LEVEL4;
                        if (node.Parent != null)
                        {
                            if (node.Parent.Children.Count == 1)
                            {
                                FindFcuNode(node.Parent, node.Item.PipeWidth);
                            }
                        }
                    }
                }
            }
            else
            {
                if (node.Children.Count > 1)
                {
                    for (int i = 0; i < node.Children.Count; i++)
                    {
                        for (int j = i + 1; j < node.Children.Count; j++)
                        {
                            if (IsCollinear(node.Children[i].Item.PLine, node.Children[j].Item.PLine))
                            {
                                if (node.Children[i].Item.PipeWidth >= node.Children[j].Item.PipeWidth)
                                {
                                    node.Children[j].Item.PipeWidth = node.Children[i].Item.PipeWidth;
                                }
                                else
                                {
                                    node.Children[i].Item.PipeWidth = node.Children[j].Item.PipeWidth;
                                }
                            }
                        }
                    }
                }
            }

            return;
        }
        private static void FindFcuNode(ThFanTreeNode<ThFanPipeModel> node, double width)
        {
            node.Item.PipeWidth = width;
            node.Item.PipeLevel = PIPELEVEL.LEVEL3;
            if (node.Parent != null)
            {
                if (node.Parent.Children.Count == 1)
                {
                    FindFcuNode(node.Parent, width);
                }
            }
        }
        private static bool IsCollinear(Line firstLine, Line secondLine)
        {
            var centerPt = firstLine.GetCenter();
            var mt = Matrix3d.Displacement(centerPt.GetVectorTo(Point3d.Origin));
            firstLine.TransformBy(mt);
            secondLine.TransformBy(mt);
            var firstLine2d = new Line2d(firstLine.StartPoint.ToPoint2d(), firstLine.EndPoint.ToPoint2d());
            var secondLine2d = new Line2d(secondLine.StartPoint.ToPoint2d(), secondLine.EndPoint.ToPoint2d());
            //            bool isIntersect = firstLine.IsCollinear(secondLine);
            bool isIntersect = firstLine2d.IsColinearTo(secondLine2d);
            firstLine.TransformBy(mt.Inverse());
            secondLine.TransformBy(mt.Inverse());
            return isIntersect;
        }
        public static bool IsIntersects(Entity firstEnt, Entity secondEnt)
        {
            var centerPt = firstEnt.GetCenter();
            var mt = Matrix3d.Displacement(centerPt.GetVectorTo(Point3d.Origin));
            firstEnt.TransformBy(mt);
            secondEnt.TransformBy(mt);
            bool isIntersect = IntersectWithEx(firstEnt, secondEnt).Count > 0 ? true : false;
            firstEnt.TransformBy(mt.Inverse());
            secondEnt.TransformBy(mt.Inverse());
            return isIntersect;
        }
        public static Point3dCollection IntersectWithEx(Entity firstEntity, Entity secondEntity, Intersect intersectType = Intersect.OnBothOperands)
        {
            Entity e1 = firstEntity.Clone() as Entity;
            Entity e2 = secondEntity.Clone() as Entity;
            Point3d basePt = Point3d.Origin;
            if (e1.GeometricExtents != null)
            {
                basePt = e1.GeometricExtents.CenterPoint();
            }
            var mt = Matrix3d.Displacement(basePt.GetVectorTo(Point3d.Origin));
            e1.TransformBy(mt);
            e2.TransformBy(mt);
            var pts = ThGeometryTool.IntersectWithEx(e1, e2, intersectType);
            return pts.OfType<Point3d>().Select(p => p.TransformBy(mt.Inverse())).ToCollection();
        }
        public static ThFanCUModel GetFanFromBlockReference(BlockReference blk)
        {
            Point3d basePt = Point3d.Origin;
            if (blk.GeometricExtents != null)
            {
                basePt = blk.GeometricExtents.CenterPoint();
            }
            var mt = Matrix3d.Displacement(basePt.GetVectorTo(Point3d.Origin));
            blk.UpgradeOpen();
            blk.TransformBy(mt);
            var tmpFan = new ThFanCUModel();
            tmpFan.FanType = blk.GetEffectiveName();
            //获取几何信息
            if (blk.ObjectId.GetDynBlockValue("水管连接点1 X") != null && blk.ObjectId.GetDynBlockValue("水管连接点1 Y") != null)
            {
                var offset1x = Convert.ToDouble(blk.ObjectId.GetDynBlockValue("水管连接点1 X"));
                var offset1y = Convert.ToDouble(blk.ObjectId.GetDynBlockValue("水管连接点1 Y"));
                var offset1 = new Point3d(offset1x, offset1y, 0);
                tmpFan.FanPoint = offset1.TransformBy(blk.BlockTransform);
                tmpFan.FanObb = GetBlockReferenceAABB(blk);
                tmpFan.FanPoint = tmpFan.FanPoint.TransformBy(mt.Inverse());
                tmpFan.FanObb.TransformBy(mt.Inverse());

                tmpFan.FanPoint = tmpFan.FanPoint.ToPoint2d().ToPoint3d();
                tmpFan.FanObb = tmpFan.FanObb.ToNTSLineString().ToDbPolyline();
            }
            blk.TransformBy(mt.Inverse());
            blk.DowngradeOpen();
            tmpFan.FanData = blk;
            var attrib = blk.ObjectId.GetAttributesInBlockReference();
            if (attrib.ContainsKey("制冷量/制热量"))
            {
                var strCapacity = attrib["制冷量/制热量"];
                GetCoolAndHotCapacity(strCapacity, out double coolCapacity, out double hotCapacity);
                tmpFan.CoolCapa = Math.Max(coolCapacity, hotCapacity);

                if (blk.GetEffectiveName() == "AI-水管断线")
                {
                    if (attrib.ContainsKey("冷/热水量"))
                    {
                        var strTempDiff = attrib["冷/热水量"];
                        strTempDiff = strTempDiff.Replace("(m3/h)", "");
                        GetCoolAndHotTempDiff(strTempDiff, out double coolFlow, out double hotFlow);
                        tmpFan.CoolFlow = coolFlow;
                        tmpFan.HotFlow = hotFlow;
                    }
                }
                else
                {
                    if (attrib.ContainsKey("冷水温差/热水温差"))
                    {
                        var strTempDiff = attrib["冷水温差/热水温差"];
                        GetCoolAndHotTempDiff(strTempDiff, out double coolTempDiff, out double hotTempDiff);

                        tmpFan.CoolFlow = coolCapacity / 1.163 / coolTempDiff;
                        tmpFan.HotFlow = hotCapacity / 1.163 / hotTempDiff;
                    }
                }
            }
            return tmpFan;
        }
        private static Polyline GetBlockReferenceAABB(BlockReference blk)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var blockTableRecord = acadDatabase.Blocks.Element(blk.BlockTableRecord);
                var rectangle = blockTableRecord.GeometricExtents().ToRectangle();
                rectangle.TransformBy(blk.BlockTransform);
                return rectangle;
            }
        }
        public static void ImportBlockFile()
        {
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.HvacPipeDwgPath(), DwgOpenMode.ReadOnly, false))//引用模块的位置
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                if (blockDb.Blocks.Contains("AI-水管多排标注(4排)"))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault("AI-水管多排标注(4排)"), true);
                }
                if (blockDb.Blocks.Contains("AI-水管多排标注(2排)"))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault("AI-水管多排标注(2排)"), true);
                }
                if (blockDb.Blocks.Contains(ThFanConnectCommon.BlkName_PipeDim2_NoH))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(ThFanConnectCommon.BlkName_PipeDim2_NoH), true);
                }
                if (blockDb.Blocks.Contains(ThFanConnectCommon.BlkName_PipeDim4_NoH))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(ThFanConnectCommon.BlkName_PipeDim4_NoH), true);
                }
                if (blockDb.Blocks.Contains("AI-水阀"))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault("AI-水阀"), true);
                }
                if (blockDb.Blocks.Contains("AI-分歧管"))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault("AI-分歧管"), true);
                }
                if (blockDb.Blocks.Contains("AI-FCU(两管制)"))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault("AI-FCU(两管制)"), true);
                }
                if (blockDb.Blocks.Contains("AI-FCU(四管制)"))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault("AI-FCU(四管制)"), true);
                }
                if (blockDb.Blocks.Contains("AI-吊顶式空调箱"))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault("AI-吊顶式空调箱"), true);
                }
                if (blockDb.Blocks.Contains("AI-水管断线"))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault("AI-水管断线"), true);
                }
                if (blockDb.Blocks.Contains("AI-中静压VRF室内机(风管机)"))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault("AI-中静压VRF室内机(风管机)"), true);
                }
                if (blockDb.Blocks.Contains("AI-VRF室内机(四面出风型)"))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault("AI-VRF室内机(四面出风型)"), true);
                }
                if (blockDb.Layers.Contains("H-PIPE-DIMS"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PIPE-DIMS"), false);
                }
                if (blockDb.Layers.Contains("H-PIPE-CS"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PIPE-CS"), false);
                }
                if (blockDb.Layers.Contains("H-PIPE-CR"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PIPE-CR"), false);
                }
                if (blockDb.Layers.Contains("H-PIPE-HS"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PIPE-HS"), false);
                }
                if (blockDb.Layers.Contains("H-PIPE-HR"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PIPE-HR"), false);
                }
                if (blockDb.Layers.Contains("H-PIPE-C"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PIPE-C"), false);
                }
                if (blockDb.Layers.Contains("H-PIPE-CHS"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PIPE-CHS"), false);
                }
                if (blockDb.Layers.Contains("H-PIPE-CHR"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PIPE-CHR"), false);
                }
                if (blockDb.Layers.Contains("H-PIPE-R"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PIPE-R"), false);
                }
                if (blockDb.Layers.Contains("H-PIPE-APPE"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PIPE-APPE"), false);
                }
                if (blockDb.Layers.Contains("H-PAPP-VALV"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PAPP-VALV"), false);
                }
            }
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                EnsureLayerOn(acadDb, "0");
                EnsureLayerOn(acadDb, "H-PIPE-DIMS");
                EnsureLayerOn(acadDb, "H-PIPE-CS");
                EnsureLayerOn(acadDb, "H-PIPE-CR");
                EnsureLayerOn(acadDb, "H-PIPE-HS");
                EnsureLayerOn(acadDb, "H-PIPE-HR");
                EnsureLayerOn(acadDb, "H-PIPE-C");
                EnsureLayerOn(acadDb, "H-PIPE-CHS");
                EnsureLayerOn(acadDb, "H-PIPE-CHR");
                EnsureLayerOn(acadDb, "H-PIPE-R");
                EnsureLayerOn(acadDb, "H-PIPE-APPE");
                EnsureLayerOn(acadDb, "H-PAPP-VALV");
            }
        }
        public static bool IsParallelLine(Line a, Line b, double degreetol = 1)
        {
            double angle = CreateVector((Line)a).GetAngleTo(CreateVector((Line)b));
            return Math.Min(angle, Math.Abs(Math.PI - angle)) / Math.PI * 180 < degreetol;
        }
        private static Vector3d CreateVector(Line line)
        {
            return CreateVector(line.StartPoint, line.EndPoint);
        }
        private static Vector3d CreateVector(Point3d ps, Point3d pe)
        {
            return new Vector3d(pe.X - ps.X, pe.Y - ps.Y, pe.Z - ps.Z);
        }

    }
}
