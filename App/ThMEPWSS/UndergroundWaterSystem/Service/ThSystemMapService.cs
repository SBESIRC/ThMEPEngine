using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPWSS.UndergroundWaterSystem.Model;
using ThMEPWSS.UndergroundWaterSystem.Tree;
using static ThMEPWSS.UndergroundWaterSystem.Utilities.GeoUtils;

namespace ThMEPWSS.UndergroundWaterSystem.Service
{
    public class PreLine
    {
        public PreLine(Line line, string layer, int lineType = -1)
        {
            Line = line;
            Layer = layer;
            LineType = lineType;
        }
        public Line Line;
        public int LineType = -1;//0横管，1立管
        public string Layer = "0";
        public List<Polyline> CorrespondingValveRec = new List<Polyline>();
    }
    public class CrossedLayerDims
    {
        public CrossedLayerDims(string text, Point3d point)
        {
            Text = text;
            Point = point;
        }
        public string Text;
        public Point3d Point;
    }
    public class Dim
    {
        public Dim(Point3d point, string text,Point3d iniPoint)
        {
            Point = point;
            Text = text;
            IniPoint = iniPoint;
        }
        public Point3d Point;
        public Point3d IniPoint;
        public string Text;
    }

    public class ThSystemMapService
    {
        public static string PipeLayerName = null;//管线绘制图层
        public static string TextStyle = null;//绘制标注样式
        public static string TextLayer = null;//绘制标注图层
        public static string ValveLayerName = null;//阀门绘制图层
        public double SubSpace { set; get; }//节点间隔距离
        public double FloorLength { set; get; }//楼层线长度
        public double FloorHeight { set; get; }//楼层高度
        public Point3d MapPostion { set; get; }//系统图基点
        public string StartMarkInfo = "";
        public List<ThFloorModel> FloorList { set; get; }//楼层和范围
        public List<ThRiserInfo> RiserList { set; get; }
        public List<PreLine> PreLines = new List<PreLine>();
        public List<Line> FloorLines = new List<Line>();
        public List<Polyline> ValveRecs = new List<Polyline>();
        public List<CrossedLayerDims> CrossedlayerDims = new List<CrossedLayerDims>();
        public List<Entity> HelpLines = new List<Entity>();
        public Matrix3d Mt { set; get; }
        public ThSystemMapService()
        {
            SubSpace = 1000.0;
            FloorLength = 200000.0;
            FloorHeight = 3000.0;
        }
        public Point3d GetMapStartPoint(Point3d basePt, List<ThFloorModel> floorList, int floorIndex)
        {
            //求出系统图的起点
            int lastFloorIndex = floorList.Count - 1;
            var vvector = new Vector3d(0.0, 1.0, 0.0);
            var hvector = new Vector3d(1.0, 0.0, 0.0);
            var startPt = basePt + vvector * (lastFloorIndex - floorIndex + 0.5) * FloorHeight;
            startPt = startPt + hvector * 2000.0;
            return startPt;
        }
        public double GetNodeLength(ThTreeNode<ThPipeModel> node)
        {
            double startLength = 1000.0;
            double endLength = 1000.0;
            double nodeLength = startLength + endLength;//第一段1000，末尾段1000
            if (node.Children.Count == 0)
            {
                return 1000.0;
            }
            foreach (var child in node.Children)
            {
                nodeLength += GetNodeLength(child);
            }
            nodeLength += SubSpace * (node.Children.Count - 1);//子节点的间隔1000
            return nodeLength;
        }
        public double GetStartLength(ThTreeNode<ThPipeModel> node)
        {
            double length = 2000.0;
            return length;
        }
        public double GetMarkLength(string markName)
        {
            double length = 200.0 * markName.Length;
            return length;
        }
        private void DrawStartInfo(Point3d point)
        {
            var vp = point + Vector3d.YAxis * 400;
            var vline = new Line(point, vp);
            var length= GetMarkLength(StartMarkInfo);
            var leftpoint = vp - Vector3d.XAxis * length - Vector3d.XAxis * 300;
            var hline = new Line(leftpoint, vp);
            PreLines.Add(new PreLine(vline, "W-WSUP-DIMS"));
            PreLines.Add(new PreLine(hline, "W-WSUP-DIMS"));
            DrawText("W-WSUP-DIMS", StartMarkInfo, leftpoint, 0.0);
        }
        public void DrawMap(Point3d basePt, ThPipeTree pipeTree)
        {
            if (FloorList.Count <= 0) return;
            MapPostion = basePt;
            //绘制楼层线
            DrawFloorLines(basePt, FloorList, FloorHeight, FloorLength);
            //求出系统图的起点
            var startPt = GetMapStartPoint(basePt, FloorList, pipeTree.FloorIndex);
            //画起始标注
            if (StartMarkInfo != "") DrawStartInfo(startPt);
            DrawRootNode(startPt, pipeTree.RootNode, pipeTree.FloorIndex);
            //打断横管线
            InterruptAndDisplayPipeLines();
            //HelpLines.ForEach(e =>
            //{
            //    e.Layer = "AI-辅助";
            //    e.ColorIndex = 30;
            //    e.AddToCurrentSpace();
            //});
            CrossedlayerDims.ForEach(e =>
            {
                DrawText("W-WSUP-DIMS", e.Text, e.Point, 0.0);
            });
        }
        public void InterruptAndDisplayPipeLines()
        {
            //阀门横管线打断
            var valvelines = PreLines.Where(e => e.LineType != 1)
                .Where(e =>
                {
                    foreach (var pl in ValveRecs)
                        if (e.Line.GetClosestPointTo(pl.GetCenter(), false).DistanceTo(pl.GetCenter()) < 1)
                            e.CorrespondingValveRec.Add(pl);
                    if (e.CorrespondingValveRec.Count > 0) return true;
                    return false;
                });
            PreLines = PreLines.Except(valvelines).ToList();
            var add_valvelines = new List<PreLine>();
            foreach (var vl in valvelines)
            {
                var line = vl.Line;
                var points = new List<Point3d>();
                vl.CorrespondingValveRec.ForEach(e => points.AddRange(e.Vertices().Cast<Point3d>()
                    .Select(p => line.GetClosestPointTo(p, false))));
                points = RemoveDuplicatePts(points);
                var splits = SplitLine(line, points).Where(e => !IsInAnyPolys(e.GetCenter(), vl.CorrespondingValveRec));
                foreach (var split in splits)
                {
                    PreLine preLine = new PreLine(split, vl.Layer, vl.LineType);
                    add_valvelines.Add(preLine);
                }
            }
            PreLines.AddRange(add_valvelines);
            //
            var horizontalPreLines = PreLines.Where(e => e.LineType == 0).ToList();
            var verticalLines = PreLines.Where(e => e.LineType == 1).Select(e => e.Line).ToList();
            PreLines.Where(e => e.LineType != 0).ForEach(e =>
            {
                e.Line.Layer = e.Layer;
                e.Line.AddToCurrentSpace();
            });
            var tmpPreLines = new List<PreLine>();
            for (int i = 0; i < horizontalPreLines.Count; i++)
            {
                var preLine = horizontalPreLines[i];
                var line = preLine.Line;
                var cutters = verticalLines.Where(e =>
                {
                    var res = SplitLine(e, line).ToList();
                    for (int j = 0; j < res.Count; j++)
                    {
                        if (res[j].Length < 1)
                        {
                            res.RemoveAt(j);
                            j--;
                        }
                    }
                    if (res.Count > 1) return true;
                    else return false;
                }).ToList();
                var splits = SplitLine(line, cutters).Where(e => e.Length >= 300).ToList();
                double tol = 150;
                if (splits.Count > 1)
                {
                    for (int j = 0; j < splits.Count; j++)
                    {
                        if (j == 0)
                            splits[j].EndPoint = splits[j].EndPoint - CreateVector(splits[j]).GetNormal() * tol;
                        else if (j == splits.Count - 1)
                            splits[j].StartPoint = splits[j].StartPoint + CreateVector(splits[j]).GetNormal() * tol;
                        else
                        {
                            splits[j].StartPoint = splits[j].StartPoint + CreateVector(splits[j]).GetNormal() * tol;
                            splits[j].EndPoint = splits[j].EndPoint - CreateVector(splits[j]).GetNormal() * tol;
                        }
                    }
                    horizontalPreLines.RemoveAt(i);
                    i--;
                    splits.ForEach(e => tmpPreLines.Add(new PreLine(e, preLine.Layer, 0)));
                }
            }
            horizontalPreLines.AddRange(tmpPreLines);
            horizontalPreLines.ForEach(e =>
            {
                e.Line.Layer = e.Layer;
                e.Line.AddToCurrentSpace();
            });
        }
        public void DrawFloorLines(Point3d basePt, List<ThFloorModel> floorList, double height, double length)
        {
            floorList.Reverse();
            int floorCount = floorList.Count;
            var hvector = new Vector3d(1.0, 0.0, 0.0);
            var vvector = new Vector3d(0.0, 1.0, 0.0);
            for (int i = 0; i < floorCount + 1; i++)
            {
                Point3d tmpPt1 = basePt + vvector * i * height;
                Point3d tmpPt2 = tmpPt1 + hvector * length;
                var fl = new Line(tmpPt1, tmpPt2);
                FloorLines.Add(fl);
                PreLines.Add(new PreLine(fl, "W-NOTE"));
                if (i < floorCount)
                {
                    DrawText("W-NOTE", floorList[i].FloorName, tmpPt1, 0.0);
                }
            }
            floorList.Reverse();
        }
        private void DrawValves(List<ThTreeNode<ThPointModel>> pointList, ref Point3d point, ref double sumLength,
            Vector3d vector)
        {
            var valves = new List<ThValveModel>();
            for (int j = 0; j < pointList.Count; j++)
            {
                var pointNode = pointList[j];
                foreach (var v in pointNode.Item.Valves)
                {
                    if (v != null && !v.Existed)
                    {
                        var valve = new ThValveModel();
                        valve = v;
                        valve.Existed = true;
                        valves.Add(valve);
                    }
                }
            }
            if (valves.Count > 0)
            {
                double length = 0;
                valves.ForEach(e => length += e.CorrespondingPipeLineLength);
                point = point + vector * length;
                var valvePt = point - vector * 1000;
                sumLength += length;
                InsertValves(ValveLayerName, valves, valvePt, vector);
            }
        }
        public double DrawRootNode(Point3d basePt, ThTreeNode<ThPipeModel> rootNode, int floorIndex)
        {
            Vector3d hvector = new Vector3d(1.0, 0.0, 0.0);
            Vector3d vvector = new Vector3d(0.0, 1.0, 0.0);
            Vector3d mvector = new Vector3d(-1.0, -1.0, 0.0);
            double height = 0.0;
            double endLength = 1000.0;
            double startLength = GetStartLength(rootNode);//考虑阀门
            //绘制子节点
            double sumLength = 0.0;
            Point3d subPt = basePt + hvector * startLength;
            var rootLine = new Line(subPt, subPt + hvector * 999999);
            ThTreeNode<ThPointModel> startPointNode = rootNode.Item.PointNodeList.FirstOrDefault();
            for (int i = 0; i < rootNode.Children.Count; i++)
            {
                var subNode = rootNode.Children[i];
                var subPt1 = subPt + hvector * (sumLength + SubSpace * i);
                //插入阀门
                var endPointNode = subNode.Item.PointNodeList.First().Parent;
                var pointList = GetPointList(startPointNode, endPointNode);
                DrawValves(pointList, ref subPt1, ref sumLength, hvector);
                //绘制冲洗点位情况
                bool _hasFlushPoint = false;
                Point3d _markLoc = new Point3d();
                DrawFlushPointEntry(pointList, ref sumLength, ref subPt1, vvector, hvector, rootLine, ref _hasFlushPoint, ref _markLoc, true);
                //绘制管径
                DrawPipeDims(pointList, hvector, subPt1);
                //立管
                var _hascrossedpipe = false;
                var riserPoint = subPt1;
                DrawRisePipe(ref pointList, ref riserPoint, ref height, ref vvector, ref hvector,
                    ref floorIndex, ref mvector, _hasFlushPoint, _markLoc, ref _hascrossedpipe, rootLine);
                sumLength += riserPoint.DistanceTo(subPt1);
                subPt1 = riserPoint;
                //画子节点
                var subLength = DrawSubNode(subPt1, subNode, height, floorIndex, rootLine);
                sumLength += subLength;
                startPointNode = endPointNode;
            }
            //
            double rootLength = startLength + endLength + sumLength;//第一段2000，末尾段1000
            rootLength += SubSpace * (rootNode.Children.Count - 1);//子节点的间隔1000
            Point3d rootPt1 = basePt;
            Point3d rootPt2 = basePt + hvector * rootLength;
            //插入阀门
            var _pointList = GetPointList(startPointNode, rootNode.Item.PointNodeList.LastOrDefault());
            DrawValves(_pointList, ref rootPt2, ref sumLength, hvector);
            //绘制冲洗点位情况
            bool hasFlushPoint = false;
            Point3d markLoc = new Point3d();
            DrawFlushPointEntry(_pointList, ref sumLength, ref rootPt2, vvector, hvector, rootLine, ref hasFlushPoint, ref markLoc, true);
            //绘制管径
            var rootPointList = GetPointList(startPointNode, rootNode.Item.PointNodeList.LastOrDefault());
            //DrawPipeDims(rootPointList, hvector, rootPt2);
            var dim = GetDims(rootPointList, hvector, rootPt2);
            //立管
            var hascrossedpipe = false;
            var _riserPoint = rootPt2;
            DrawRisePipe(ref _pointList, ref _riserPoint, ref height, ref vvector, ref hvector,
                ref floorIndex, ref mvector, hasFlushPoint, markLoc, ref hascrossedpipe, rootLine);
            sumLength += _riserPoint.DistanceTo(rootPt2);
            var endPoint = PreLines.Where(e => CreateVector(e.Line).IsParallelTo(Vector3d.YAxis))
                .Select(e => e.Line)
                .Where(e => Math.Abs(e.StartPoint.Y - rootPt1.Y) < 1 || Math.Abs(e.EndPoint.Y - rootPt1.Y) < 1)
                .Select(e => e.GetCenter())
                .OrderByDescending(e => e.X)
                .First();
            endPoint = new Point3d(endPoint.X, rootPt1.Y, 0);
            var rLine = new Line(rootPt1, endPoint);
            //绘制主干线
            if (rootNode.Children.Count > 0)
            {
                //如果主干线后面有阀门但没其它东西的情况
                var cond = false;
                var points = ValveRecs.Select(e => e.GetCenter()).Where(e => Math.Abs(endPoint.Y - e.Y) < 1)
                    .OrderByDescending(p => p.X);
                if (points.Count() > 0)
                {
                    var point = points.First();
                    if (point.X > endPoint.X)
                        cond = true;
                }
                if (cond)
                {
                    PreLines.Add(new PreLine(new Line(rootPt1, rootPt2), PipeLayerName, 0));
                    DrawBreakDot(rootPt2, Math.PI / 2);
                    if (dim.Point.X < rootPt2.X - 10)
                        DrawDim(dim);
                }
                else
                {
                    PreLines.Add(new PreLine(rLine, PipeLayerName, 0));
                    if (dim.Point.X < endPoint.X - 10)
                        DrawDim(dim);
                }
            }
            else
                DrawBreakDot(rootPt1);
            rootPt2 = _riserPoint;
            return rootLength;
        }
        public double DrawSubNode(Point3d basePt, ThTreeNode<ThPipeModel> subNode, double height, int floorIndex, Line rootLine)
        {
            Vector3d vvector = new Vector3d(0.0, 1.0, 0.0);
            Vector3d hvector = new Vector3d(1.0, 0.0, 0.0);
            Vector3d mvector = new Vector3d(-1.0, -1.0, 0.0);
            //绘制当前节点
            height = height + 400;
            double startLength = 1000.0;
            double endLength = 1000.0;
            //绘制垂直线
            Point3d vLinePt1 = basePt;
            Point3d vLinePt2 = basePt + vvector * 400.0;
            if (subNode.Children.Count == 0)
            {
                var vertlengh = 400.0;
                if (rootLine != null)
                    vertlengh += rootLine.GetClosestPointTo(basePt, false).DistanceTo(basePt);
                vLinePt2 = basePt - vvector * vertlengh;
            }
            PreLines.Add(new PreLine(new Line(vLinePt1, vLinePt2), PipeLayerName, 1));
            Point3d hLinePt1 = vLinePt2;
            //debug
            var pdwg = subNode.Item.PointNodeList.First().Item.Position;
            HelpLines.Add(new Line(vLinePt1, pdwg));
            //绘制子节点
            double sumLength = 0.0;
            Point3d childPt = hLinePt1 + hvector * startLength;
            ThTreeNode<ThPointModel> startPointNode = subNode.Item.PointNodeList.FirstOrDefault();
            for (int i = 0; i < subNode.Children.Count; i++)
            {
                var childNode = subNode.Children[i];
                var childPt1 = childPt + hvector * (sumLength + SubSpace * i);
                //插入阀门
                var endPointNode = childNode.Item.PointNodeList.First().Parent;
                var pointList = GetPointList(startPointNode, endPointNode);
                DrawValves(pointList, ref childPt1, ref sumLength, hvector);
                //绘制冲洗点位情况
                bool _hasFlushPoint = false;
                Point3d _markLoc = new Point3d();
                DrawFlushPointEntry(pointList, ref sumLength, ref childPt1, vvector, hvector, rootLine, ref _hasFlushPoint, ref _markLoc, true);
                //绘制管径
                DrawPipeDims(pointList, hvector, childPt1);
                //立管
                var _hascrossedpipe = false;
                var riserPoint = childPt1;
                DrawRisePipe(ref pointList, ref riserPoint, ref height, ref vvector, ref hvector,
                    ref floorIndex, ref mvector, _hasFlushPoint, _markLoc, ref _hascrossedpipe);
                sumLength += riserPoint.DistanceTo(childPt1);
                childPt1 = riserPoint;
                //绘制子节点
                var subLength = DrawSubNode(childPt1, childNode, height, floorIndex, rootLine);
                sumLength += subLength;
                startPointNode = endPointNode;
            }
            var cuLength = startLength + sumLength;
            if (subNode.Children.Count == 0)
            {
                cuLength = 1000.0;
            }
            else
            {
                cuLength += SubSpace * (subNode.Children.Count - 1);//子节点的间隔1000
                cuLength += endLength;
            }
            Point3d hLinePt2 = vLinePt2 + hvector * cuLength;
            //插入阀门
            var _pointList = GetPointList(startPointNode, subNode.Item.PointNodeList.LastOrDefault());
            DrawValves(_pointList, ref hLinePt2, ref cuLength, hvector);
            //绘制冲洗点位情况
            bool hasFlushPoint = false;
            Point3d markLoc = new Point3d();
            DrawFlushPointEntry(_pointList, ref cuLength, ref hLinePt2, vvector, hvector, rootLine, ref hasFlushPoint, ref markLoc, true);
            //绘制管径
            var rootPointList = GetPointList(startPointNode, subNode.Item.PointNodeList.LastOrDefault());
            //DrawPipeDims(rootPointList, hvector, hLinePt2);
            var dim = GetDims(rootPointList, hvector, hLinePt2);
            //立管
            var hascrossedpipe = false;
            var _riserPoint = hLinePt2;
            DrawRisePipe(ref _pointList, ref _riserPoint, ref height, ref vvector, ref hvector,
                ref floorIndex, ref mvector, hasFlushPoint, markLoc, ref hascrossedpipe);
            cuLength += _riserPoint.DistanceTo(hLinePt2);
            var endPoint = PreLines.Where(e => CreateVector(e.Line).IsParallelTo(Vector3d.YAxis))
                .Select(e => e.Line)
                .Where(e => Math.Abs(e.StartPoint.Y - hLinePt2.Y) < 1 || Math.Abs(e.EndPoint.Y - hLinePt2.Y) < 1)
                .Select(e => e.GetCenter())
                .OrderByDescending(e => e.X)
                .First();
            endPoint = new Point3d(endPoint.X, hLinePt2.Y, 0);
            //绘制当前节点干线
            var line = new Line(hLinePt1, hLinePt2);
            if (hasFlushPoint)
            {
                line = new Line(hLinePt1, hLinePt2 - hvector * 1000);
                endPoint = line.EndPoint;
            }
            else if (!hascrossedpipe)
                DrawBreakDot(hLinePt2, Math.PI / 2);
            PreLines.Add(new PreLine(line, PipeLayerName, 0));
            if (dim.Point.X < endPoint.X - 10)
                DrawDim(dim);
            hLinePt2 = _riserPoint;
            return cuLength;
        }
        private void DrawFlushPointEntry(List<ThTreeNode<ThPointModel>> _pointList, ref double length,
            ref Point3d point, Vector3d vvector, Vector3d hvector, Line rootLine, ref bool hasFlushPoint, ref Point3d markLoc, bool isInChild)
        {
            bool flushpointFound = false;
            foreach (var node in _pointList)
            {
                if (node.Item.FlushPoint.Valve != null)
                {
                    flushpointFound = true;
                    var flushPoint = node.Item.FlushPoint;
                    DrawFlushPoint(flushPoint, point, vvector, hvector, ref markLoc, isInChild,rootLine);
                    hasFlushPoint = true;
                    break;
                }
            }
            if (flushpointFound)
            {
                point += hvector * 1000;
                length += 1000;
            }
        }
        private Dim GetDims(List<ThTreeNode<ThPointModel>> pointList, Vector3d hvector, Point3d point)
        {
            var p = new Point3d();
            string dimMark1 = "";
            foreach (var pointNode in pointList)
            {
                if (pointNode.Item.DimMark != null)
                {
                    dimMark1 = pointNode.Item.DimMark.StrText;
                    p = pointNode.Item.DimMark.Position;
                }
            }
            var dimPt1 = point - hvector * 1000.0;
            return new Dim(dimPt1, dimMark1,p);
        }
        private void DrawDim(Dim dim)
        {
            DrawText("W-WSUP-DIMS", dim.Text, dim.Point, 0.0);
        }
        private void DrawPipeDims(List<ThTreeNode<ThPointModel>> pointList, Vector3d hvector, Point3d point)
        {
            var p = new Point3d();
            string dimMark1 = "";
            foreach (var pointNode in pointList)
            {
                if (pointNode.Item.DimMark != null)
                {
                    dimMark1 = pointNode.Item.DimMark.StrText;
                    p = pointNode.Item.DimMark.Position;
                }
            }
            var dimPt1 = point - hvector * 1000.0;
            DrawText("W-WSUP-DIMS", dimMark1, dimPt1, 0.0);
        }
        public void DrawFlushPoint(ThFlushPointModel flushPoint, Point3d basePt, Vector3d vvector, Vector3d hvector, ref Point3d markLoc, bool isInChild, Line rootLine = null)
        {
            var vertLength = 400.0;
            var vertdist= rootLine.GetClosestPointTo(basePt, true).DistanceTo(basePt);
            if (rootLine != null)
                vertLength += vertdist;
            var vDownPt1 = basePt;
            var vDownPt4 = vDownPt1 - vvector * 1000.0;
            if (isInChild)
            {
                if (rootLine != null)
                    vDownPt4 = new Point3d(vDownPt4.X, rootLine.GetCenter().Y - 1400, 0);
                var vertline = new Line(vDownPt1, vDownPt4);
                PreLines.Add(new PreLine(vertline, PipeLayerName, 1));
                markLoc = vertline.GetCenter();
            }
            else
            {
                var vDownPt2 = vDownPt1 - vvector * vertLength;
                PreLines.Add(new PreLine(new Line(vDownPt1, vDownPt2), PipeLayerName, 1));
                var vDownPt3 = vDownPt2 + hvector * 1000;
                PreLines.Add(new PreLine(new Line(vDownPt2, vDownPt3), PipeLayerName, 0));
                vDownPt4 = vDownPt3 - vvector * 1000.0;
                if (rootLine != null)
                    vDownPt4 = new Point3d(vDownPt4.X, rootLine.GetCenter().Y - 1400, 0);
                var vertline = new Line(vDownPt3, vDownPt4);
                PreLines.Add(new PreLine(vertline, PipeLayerName, 1));
                markLoc = vertline.GetCenter();
            }
            using (var adb = AcadDatabase.Active())
            {
                var blId = adb.CurrentSpace.ObjectId.InsertBlockReference(
                    ValveLayerName, "皮带水嘴系统", vDownPt4, new Scale3d(1), 0);
                blId.SetDynBlockValue("可见性", "向右真空破坏组合");
                var br = adb.Element<BlockReference>(blId);
            }
        }
        public double DrawOtherFloor(Point3d basePt, Point3d startPt, int curFloorIndex, int otherFloorIndex, ref bool isToCurFloor, string crossLayerDims)
        {
            Point3d otherPt = GetMapStartPoint(MapPostion, FloorList, otherFloorIndex);
            Point3d otherStartPt = new Point3d(basePt.X, otherPt.Y, 0.0);
            var vLine1 = new Line(basePt, otherStartPt);
            PreLines.Add(new PreLine(vLine1, PipeLayerName, 1));
            //跨层立管标注
            var crossp = vLine1.GetCenter();
            var floors = FloorLines.OrderBy(e => e.GetCenter().Y);
            double dist_floor = -1;
            foreach (var floor in floors)
            {
                var dist = floor.GetClosestPointTo(crossp, true).DistanceTo(crossp);
                if (dist < 450 && floor.GetCenter().Y > crossp.Y)
                {
                    dist_floor = 450 - dist;
                    break;
                }
            }
            if (dist_floor > -1) crossp = crossp - Vector3d.YAxis * dist_floor;
            var crossp_left = crossp.TransformBy(Matrix3d.Displacement(-Vector3d.XAxis * (200 + GetMarkLength(crossLayerDims))));
            var crossmark_line = new Line(crossp_left, crossp);
            if (crossLayerDims.Length > 0)
            {
                PreLines.Add(new PreLine(crossmark_line, "W-WSUP-DIMS"));
                CrossedlayerDims.Add(new CrossedLayerDims(crossLayerDims, crossp_left));
            }
            //构造树
            double floorLength = 0.0;
            var pipeTree = new ThPipeTree(startPt, FloorList, RiserList, Mt);
            if (pipeTree.RootNode != null)
            {
                var crossFloorIndexList = FindCrossFloorIndexList(pipeTree.RootNode);
                foreach (var index in crossFloorIndexList)
                {
                    if (index == curFloorIndex)
                    {
                        isToCurFloor = true;
                        break;
                    }
                }
                floorLength = DrawRootNode(otherStartPt, pipeTree.RootNode, otherFloorIndex);
            }
            else
                DrawBreakDot(otherStartPt);
            return floorLength;
        }
        public void DrawBreakDot(Point3d point, double angle = 0)
        {
            using (var adb = AcadDatabase.Active())
            {
                var blId = adb.CurrentSpace.ObjectId.InsertBlockReference(
                    "W-WSUP-DIMS", "断线", point, new Scale3d(1), angle);
                var br = adb.Element<BlockReference>(blId);
            }
        }
        public void DrawCircle(Point3d center, double radius, string layer, int colorIndex)
        {
            using (var database = AcadDatabase.Active())
            {
                var circle = new Circle(center, new Vector3d(0.0, 0.0, 1.0), radius);
                circle.Layer = layer;
                circle.ColorIndex = colorIndex;
                Draw.AddToCurrentSpace(circle);
            }

        }
        public Line DrawLine(Point3d startPt, Point3d endPt, string layer, int colorIndex = -1)
        {
            using (var database = AcadDatabase.Active())
            {
                var line = new Line(startPt, endPt);
                line.Layer = layer;
                if (colorIndex != -1)
                    line.ColorIndex = colorIndex;
                Draw.AddToCurrentSpace(line);
                return line;
            }

        }
        public void DrawText(string layer, string strText, Point3d position, double angle)
        {
            using (var database = AcadDatabase.Active())
            {
                var dbText = new DBText();
                dbText.Layer = layer;
                dbText.TextString = strText;
                dbText.Position = position;
                dbText.Rotation = angle;
                dbText.Height = 300.0;
                dbText.WidthFactor = 0.7;
                dbText.TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3");
                //dbText.HorizontalMode = TextHorizontalMode.TextCenter;
                //dbText.VerticalMode = TextVerticalMode.TextVerticalMid;
                //dbText.AlignmentPoint = position;
                database.ModelSpace.Add(dbText);
            }
        }
        public void DrawRisePipe(ref List<ThTreeNode<ThPointModel>> pointList, ref Point3d riserPoint
            , ref double height, ref Vector3d vvector, ref Vector3d hvector, ref int floorIndex, ref Vector3d mvector
            , bool hasFlushPoint, Point3d markloc, ref bool hascrossedpipe, Line rootLine = null)
        {
            var iniriserPoint = riserPoint;
            List<Point3d> riserStartPoints = new List<Point3d>();
            for (int j = 0; j < pointList.Count; j++)
            {
                double curRiserLength = 0;
                var node = pointList[j];
                bool hasNode = false;
                string crossLayerDims = "";
                if (node.Item.Break != null)
                {
                    crossLayerDims = node.Item.Break.BreakName;
                }
                if (node.Item.Riser != null && node.Item.Riser.RiserPts.Count > 0)
                {
                    hasNode = true;
                    var vPt1 = riserPoint;
                    riserStartPoints.Add(vPt1);
                    double vlength = FloorHeight / 2.0 - height - 200.0;
                    if (vlength < 400)
                    {
                        vlength = 400;
                    }
                    var vPt2 = vPt1 + vvector * vlength;
                    if (node.Item.Riser.RiserPts.Count == 0)
                    {
                        PreLines.Add(new PreLine(new Line(vPt1, vPt2), PipeLayerName, 1));
                        if (node.Item.Riser.MarkName != null && node.Item.Riser.MarkName.Length > 0)
                        {
                            var vpt3 = vPt1 + vvector * 300.0;
                            var hpt3 = vpt3 - hvector * GetMarkLength(node.Item.Riser.MarkName);
                            PreLines.Add(new PreLine(new Line(vpt3, hpt3), "W-WSUP-DIMS"));
                            //绘制立管标注
                            DrawText("W-WSUP-DIMS", node.Item.Riser.MarkName, hpt3, 0.0);
                        }
                    }
                    //todo3：不能只考虑最后一个pointNode，要考虑所有的pointNode
                    while (node.Item.Riser.RiserPts.Count != 0)
                    {
                        var firstPt = node.Item.Riser.RiserPts.First();
                        node.Item.Riser.RiserPts.Remove(firstPt);
                        var otherIndex = GetFloorIndex(firstPt, FloorList);
                        if (otherIndex != floorIndex)
                        {
                            hascrossedpipe = true;
                            bool isToCurFloor = false;
                            HelpLines.Add(new Line(vPt1, firstPt));
                            var compare_ini_lines = PreLines.Select(e => e.Line).ToList();
                            curRiserLength = DrawOtherFloor(vPt1, firstPt, floorIndex, otherIndex, ref isToCurFloor, crossLayerDims);
                            crossLayerDims = "";
                            //跨层立管太长，同层后面立管位置往前挪排版紧凑些
                            var compare_out_lines = PreLines.Select(e => e.Line).ToList();
                            var generated_lines = compare_out_lines.Except(compare_ini_lines);
                            var points = generated_lines.Select(e => e.GetCenter())
                                .Where(p => Math.Abs(p.Y - vPt1.Y) < 3000).OrderByDescending(p => p.X);
                            var point = vPt1;
                            if (points.Count() > 0) point = points.First();
                            curRiserLength = point.X - vPt1.X;
                            curRiserLength = curRiserLength >= 0 ? curRiserLength : 0;
                        }
                    }
                }
                var cond = CrossedlayerDims.Count > 0 ? !CrossedlayerDims[CrossedlayerDims.Count - 1].Text.Equals(crossLayerDims) : true;
                if (node.Item.Break != null && crossLayerDims != "" && cond)
                {
                    //如果是断线，绘制断线标注
                    crossLayerDims = node.Item.Break.BreakName;
                    if (!hasFlushPoint)
                    {
                        var iniloc = riserPoint;
                        var uploc = iniloc + Vector3d.YAxis * 400;
                        if (iniloc.Y <= iniriserPoint.Y)
                            uploc = iniloc - Vector3d.YAxis * 1000;
                        var leftuploc = uploc - Vector3d.XAxis * (GetMarkLength(crossLayerDims) + 200);
                        var vertline = new Line(iniloc, uploc);
                        var horline = new Line(leftuploc, uploc);
                        PreLines.Add(new PreLine(vertline, "W-WSUP-DIMS"));
                        PreLines.Add(new PreLine(horline, "W-WSUP-DIMS"));
                        DrawText("W-WSUP-DIMS", crossLayerDims, leftuploc, 0.0);
                    }
                    else
                    {
                        var iniloc = markloc;
                        var leftloc = markloc - Vector3d.XAxis * (GetMarkLength(crossLayerDims) + 200);
                        var flushline = new Line(leftloc, iniloc);
                        PreLines.Add(new PreLine(flushline, "W-WSUP-DIMS"));
                        DrawText("W-WSUP-DIMS", crossLayerDims, leftloc, 0.0);
                    }
                }
                if (hasNode)
                {
                    riserPoint += hvector.GetNormal() * (curRiserLength + 1000);
                }
            }
            if (riserStartPoints.Count > 1)
            {
                for (int i = 0; i < riserStartPoints.Count - 1; i++)
                {
                    var cond_a = rootLine == null;
                    var cond_b = true;
                    if (!cond_a)
                    {
                        cond_b = rootLine.GetClosestPointTo(riserStartPoints[i], true).DistanceTo(riserStartPoints[i]) < 1
                            && rootLine.GetClosestPointTo(riserStartPoints[i + 1], true).DistanceTo(riserStartPoints[i + 1]) < 1;
                    }
                    var cond_c = CreateVector(riserStartPoints[i], riserStartPoints[i + 1]).IsParallelTo(Vector3d.XAxis);
                    var cond = cond_a && cond_b && cond_c;
                    if (cond)
                    {
                        PreLines.Add(new PreLine(new Line(riserStartPoints[i], riserStartPoints[i + 1]), PipeLayerName, 0));
                    }
                }
            }
        }
        public void InsertValves(string layer, List<ThValveModel> valves, Point3d position, Vector3d vector)
        {
            using (var adb = AcadDatabase.Active())
            {
                foreach (var valve in valves)
                {
                    var name = "";
                    if (valve.Valve.Database != null)
                        name = valve.Valve.GetEffectiveName();
                    else
                        name = valve.Valve.Name;
                    //var sc = valve.Valve.ScaleFactors;
                    //var sc_x = sc.X > 0 ? 1 : -1;
                    //var sc_y = sc.Y > 0 ? 1 : -1;
                    //sc = new Scale3d(sc_x, sc_y,1);
                    var blId = adb.CurrentSpace.ObjectId.InsertBlockReference(
                        layer, name, position, new Scale3d(1), Math.PI);
                    var br = adb.Element<BlockReference>(blId);
                    if (Math.Abs(position.X - br.GeometricExtents.CenterPoint().X) > 1)
                    {
                        br.TransformBy(Matrix3d.Displacement(new Vector3d(position.X - br.GeometricExtents.CenterPoint().X, 0, 0)));
                    }
                    br.Layer = layer;
                    position += vector.GetNormal() * 400;
                    var rec = br.GeometricExtents.ToRectangle();
                    ValveRecs.Add(rec);
                }
            }
        }
        public int GetFloorIndex(Point3d startPt, List<ThFloorModel> floorList)
        {
            int index = -1;
            for (int i = 0; i < floorList.Count; i++)
            {
                if (floorList[i].FloorArea.Contains(startPt))
                {
                    index = i;
                    break;
                }
            }
            return index;
        }
        public List<int> FindCrossFloorIndexList(ThTreeNode<ThPipeModel> node)
        {
            var retList = new List<int>();
            foreach (var child in node.Children)
            {
                retList.AddRange(FindCrossFloorIndexList(child));
            }
            if (node.Item.PointNodeList.Last().Item.Riser != null)
            {
                if (node.Item.PointNodeList.Last().Item.Riser.RiserPts.Count != 0)
                {
                    foreach (var pt in node.Item.PointNodeList.Last().Item.Riser.RiserPts)
                    {
                        var floorIndex = GetFloorIndex(pt, FloorList);
                        retList.Add(floorIndex);
                    }
                }
            }
            return retList;
        }
        public bool IsClossLine(Line vline, Line hline, ref Point3d clossPt)
        {
            bool isCloss = false;
            var pts = new Point3dCollection();
            vline.IntersectWith(hline, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
            if (pts.Count > 0)
            {
                clossPt = pts[0];
                if (clossPt.DistanceTo(vline.StartPoint) > 10.0
                    && clossPt.DistanceTo(vline.EndPoint) > 10.0
                    && clossPt.DistanceTo(hline.StartPoint) > 10.0
                    && clossPt.DistanceTo(hline.EndPoint) > 10.0)
                {
                    isCloss = true;
                }
            }
            return isCloss;
        }
        public List<ThTreeNode<ThPointModel>> GetPointList(ThTreeNode<ThPointModel> rootNode, ThTreeNode<ThPointModel> lastNode)
        {
            var retNodes = new List<ThTreeNode<ThPointModel>>();
            if (lastNode.Parent == null || lastNode == rootNode)
            {
                retNodes.Add(lastNode);
                return retNodes;
            }
            if (lastNode.Parent == rootNode)
            {
                retNodes.Add(lastNode);
                retNodes.Add(rootNode);
                retNodes.Reverse();
                return retNodes;
            }
            ThTreeNode<ThPointModel> tempNode = lastNode;
            while (tempNode.Parent != rootNode)
            {
                retNodes.Add(tempNode);
                tempNode = tempNode.Parent;
            }
            retNodes.Add(tempNode);
            retNodes.Add(rootNode);
            retNodes.Reverse();
            return retNodes;
        }
    }
}
