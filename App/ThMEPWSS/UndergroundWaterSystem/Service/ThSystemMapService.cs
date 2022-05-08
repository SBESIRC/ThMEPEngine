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
    public partial class ThSystemMapService
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
        private string DIMLAYER = "W-WSUP-DIMS";
        private string NOTELAYER = "W-NOTE";
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
            HelpLines.ForEach(e =>
            {
                e.Layer = "AI-辅助";
                e.ColorIndex = 13;
                e.AddToCurrentSpace();
            });
            CrossedlayerDims.ForEach(e => { DrawText(DIMLAYER, e.Text, e.Point, 0.0); });
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
            var isUnnececcsaryBreakDot = false;
            var _riserPoint = rootPt2;
            DrawRisePipe(ref _pointList, ref _riserPoint, ref height, ref vvector, ref hvector,
                ref floorIndex, ref mvector, hasFlushPoint, markLoc, ref isUnnececcsaryBreakDot, rootLine);
            sumLength += _riserPoint.DistanceTo(rootPt2);
            var endPoints = PreLines.Where(e => CreateVector(e.Line).IsParallelTo(Vector3d.YAxis))
                .Select(e => e.Line)
                .Where(e => Math.Abs(e.StartPoint.Y - rootPt1.Y) < 1 || Math.Abs(e.EndPoint.Y - rootPt1.Y) < 1)
                .Select(e => e.GetCenter())
                .OrderByDescending(e => e.X);
            var endPoint = rootPt2;
            //if (rootNode.Children.Count > 0)
            //{
            //    if (endPoints.Count() > 0) endPoint = endPoints.First();
            //    endPoint = new Point3d(endPoint.X, rootPt1.Y, 0);
            //}
            if (hasFlushPoint)
            {
                endPoint = endPoint - Vector3d.XAxis * 1000;
            }
            var rLine = new Line(rootPt1, endPoint);
            //绘制主干线
            if (/*rootNode.Children.Count > 0 || ValveRecs.Where(e => Math.Abs(e.GetCenter().Y - rootPt1.Y) < 1000).Count() > 0*/false)
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
                    if (!isUnnececcsaryBreakDot && !hasFlushPoint && rootNode.Children.Count == 0)
                    {
                        var break_angle = Math.PI / 2;
                        if (rootPt2.DistanceTo(basePt) < 1) break_angle = 0;
                        DrawBreakDot(rootPt2, break_angle);
                        DrawBreakName(_pointList, rootPt2, false);
                    }
                    //DrawBreakDot(rootPt2, Math.PI / 2);
                    if (dim.Point.X < rootPt2.X - 10)
                        DrawDim(dim);
                }
                else
                {
                    PreLines.Add(new PreLine(rLine, PipeLayerName, 0));
                    if (!isUnnececcsaryBreakDot && !hasFlushPoint && rootNode.Children.Count == 0)
                    {
                        var break_angle = Math.PI / 2;
                        if (rLine.EndPoint.DistanceTo(basePt) < 1) break_angle = 0;
                        DrawBreakDot(rLine.EndPoint, break_angle);
                        DrawBreakName(_pointList, rLine.EndPoint, false);
                    }
                    if (dim.Point.X < endPoint.X - 10)
                        DrawDim(dim);
                }
            }
            else
            {
                if (!isUnnececcsaryBreakDot && !hasFlushPoint /*&& rootNode.Children.Count == 0*/)
                {
                    var break_angle = Math.PI / 2;
                    if (endPoint.DistanceTo(basePt) < 1) break_angle = 0;
                    if (break_angle == Math.PI / 2)
                    {
                        var end_p = endPoint;
                        var end_p_down = end_p - Vector3d.YAxis * 400;
                        var end_p_down_right = end_p_down + Vector3d.XAxis * 1000;
                        var pipe_vert = new Line(end_p, end_p_down);
                        var pipr_hor = new Line(end_p_down, end_p_down_right);
                        PreLines.Add(new PreLine(pipe_vert, PipeLayerName, 1));
                        PreLines.Add(new PreLine(pipr_hor, PipeLayerName, 0));
                        DrawBreakDot(end_p_down_right, break_angle);
                        endPoint = end_p_down_right;
                    }
                    else
                    {
                        DrawBreakDot(endPoint, break_angle);
                    }
                    DrawBreakName(_pointList, endPoint, false);
                }
                PreLines.Add(new PreLine(rLine, PipeLayerName, 0));
                if (dim.Point.X < endPoint.X - 10)
                    DrawDim(dim);
            }
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
            var isUnnececcsaryBreakDot = false;
            var _riserPoint = hLinePt2;
            DrawRisePipe(ref _pointList, ref _riserPoint, ref height, ref vvector, ref hvector,
                ref floorIndex, ref mvector, hasFlushPoint, markLoc, ref isUnnececcsaryBreakDot, null);
            cuLength += _riserPoint.DistanceTo(hLinePt2);
            var endPoints = PreLines.Where(e => CreateVector(e.Line).IsParallelTo(Vector3d.YAxis))
                .Select(e => e.Line)
                .Where(e => Math.Abs(e.StartPoint.Y - hLinePt2.Y) < 1 || Math.Abs(e.EndPoint.Y - hLinePt2.Y) < 1)
                .Select(e => e.GetCenter())
                .OrderByDescending(e => e.X);
            var endPoint = hLinePt2;
            //if (subNode.Children.Count > 0)
            //{
            //    if (endPoints.Count() > 0) endPoint = endPoints.First();
            //    endPoint = new Point3d(endPoint.X, hLinePt2.Y, 0);
            //}
            //绘制当前节点干线
            var line = new Line(hLinePt1, hLinePt2);
            if (hasFlushPoint)
            {
                line = new Line(hLinePt1, hLinePt2 - hvector * 1000);
                endPoint = line.EndPoint;
            }
            else if (!isUnnececcsaryBreakDot)
            {
                var upward = false;
                if (line.Length != 1000)
                {
                    var vertlengh = 800.0;
                    if (rootLine != null)
                        vertlengh += rootLine.GetClosestPointTo(basePt, false).DistanceTo(basePt);
                    var end_p = hLinePt2;
                    var end_p_down = end_p - Vector3d.YAxis * vertlengh;
                    var end_p_down_right = end_p_down + Vector3d.XAxis * 1000;
                    var pipe_vert = new Line(end_p, end_p_down);
                    var pipr_hor = new Line(end_p_down, end_p_down_right);
                    PreLines.Add(new PreLine(pipe_vert, PipeLayerName, 1));
                    PreLines.Add(new PreLine(pipr_hor, PipeLayerName, 0));
                    DrawBreakDot(end_p_down_right, Math.PI / 2);
                    hLinePt2 = end_p_down_right;
                    cuLength += 1000;
                }
                else
                {
                    DrawBreakDot(hLinePt2, Math.PI / 2);
                    if (hLinePt2.Y > basePt.Y) upward = true;
                }
                DrawBreakName(_pointList, hLinePt2, false);
            }
            PreLines.Add(new PreLine(line, PipeLayerName, 0));
            if (dim.Point.X < endPoint.X - 10)
                DrawDim(dim);
            hLinePt2 = _riserPoint;
            return cuLength;
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
                PreLines.Add(new PreLine(crossmark_line, DIMLAYER));
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
                if (crossLayerDims.Length > 0)
                {
                    for (int i = 0; i < pipeTree.RootNode.Item.PointNodeList.Count; i++)
                    {
                        var node = pipeTree.RootNode.Item.PointNodeList[i];
                        if (node.Item.Break != null && node.Item.Break.BreakName.Equals(crossLayerDims))
                        {
                            pipeTree.RootNode.Item.PointNodeList[i].Item.Break.Used = true;
                        }
                    }
                }
                floorLength = DrawRootNode(otherStartPt, pipeTree.RootNode, otherFloorIndex);
            }
            else
                DrawBreakDot(otherStartPt);
            return floorLength;
        }
        public void DrawRisePipe(ref List<ThTreeNode<ThPointModel>> pointList, ref Point3d riserPoint
           , ref double height, ref Vector3d vvector, ref Vector3d hvector, ref int floorIndex, ref Vector3d mvector
           , bool hasFlushPoint, Point3d markloc, ref bool isUnnececcsaryBreakDot, Line rootLine = null)
        {
            var iniriserPoint = riserPoint;
            List<Point3d> riserStartPoints = new List<Point3d>();
            for (int j = 0; j < pointList.Count; j++)
            {
                var node = pointList[j];
                if (node.Item.Riser == null) continue;
                if (node.Item.Riser.RiserPts.Count == 0)
                {
                    string riser_dim = "";
                    if (node.Item.Break != null && node.Item.Break.BreakName != "")
                    {
                        riser_dim = node.Item.Break.BreakName;
                        pointList[j].Item.Break.Used = true;
                        if (!hasFlushPoint)
                        {
                            isUnnececcsaryBreakDot = true;
                            var iniloc = riserPoint;
                            var floorlines = FloorLines.Where(e => e.GetCenter().Y > iniloc.Y).OrderBy(e => e.GetCenter().Y - iniloc.Y);
                            double distance = 1600;
                            if (floorlines.Count() > 0) distance = floorlines.First().GetClosestPointTo(iniloc, true).DistanceTo(iniloc) - 200;
                            var uploc = iniloc + Vector3d.YAxis * distance;
                            var vertline = new Line(iniloc, uploc);
                            var leftuploc = vertline.GetCenter() - Vector3d.XAxis * (GetMarkLength(riser_dim) + 200);
                            var horline = new Line(leftuploc, vertline.GetCenter());
                            PreLines.Add(new PreLine(vertline, PipeLayerName, 1));
                            PreLines.Add(new PreLine(horline, DIMLAYER));
                            DrawText(DIMLAYER, riser_dim, leftuploc, 0.0);
                            DrawBreakDot(uploc);
                            continue;
                        }
                        else
                        {
                            var iniloc = markloc;
                            var leftloc = markloc - Vector3d.XAxis * (GetMarkLength(riser_dim) + 200);
                            var flushline = new Line(leftloc, iniloc);
                            PreLines.Add(new PreLine(flushline, DIMLAYER));
                            DrawText(DIMLAYER, riser_dim, leftloc, 0.0);
                        }
                    }
                    else continue;
                }
                double curRiserLength = 0;
                bool hasNode = false;
                string crossLayerDims = "";
                if (node.Item.Break != null)
                    crossLayerDims = node.Item.Break.BreakName;

                hasNode = true;
                var vertLocPoint = riserPoint;
                riserStartPoints.Add(vertLocPoint);
                //double vlength = FloorHeight / 2.0 - height - 200.0;
                //if (vlength < 400)
                //{
                //    vlength = 400;
                //}

                while (node.Item.Riser.RiserPts.Count != 0)
                {
                    var firstPt = node.Item.Riser.RiserPts.First();
                    node.Item.Riser.RiserPts.Remove(firstPt);
                    var otherIndex = GetFloorIndex(firstPt, FloorList);
                    if (otherIndex != floorIndex)
                    {
                        isUnnececcsaryBreakDot = true;
                        bool isToCurFloor = false;
                        HelpLines.Add(new Line(vertLocPoint, firstPt));
                        var compare_ini_lines = PreLines.Select(e => e.Line).ToList();
                        if (crossLayerDims.Length > 0) pointList[j].Item.Break.Used = true;
                        curRiserLength = DrawOtherFloor(vertLocPoint, firstPt, floorIndex, otherIndex, ref isToCurFloor, crossLayerDims);
                        crossLayerDims = "";
                        //跨层立管太长，同层后面立管位置往前挪排版紧凑些
                        var compare_out_lines = PreLines.Select(e => e.Line).ToList();
                        var generated_lines = compare_out_lines.Except(compare_ini_lines);
                        var points = generated_lines.Select(e => e.GetCenter())
                            .Where(p => Math.Abs(p.Y - vertLocPoint.Y) < 3000).OrderByDescending(p => p.X);
                        var point = vertLocPoint;
                        if (points.Count() > 0) point = points.First();
                        curRiserLength = point.X - vertLocPoint.X;
                        curRiserLength = curRiserLength >= 0 ? curRiserLength : 0;
                    }
                    else
                    {
                        string riser_dim = "";
                        if (node.Item.Break != null && node.Item.Break.BreakName != "")
                        {
                            riser_dim = node.Item.Break.BreakName;
                            pointList[j].Item.Break.Used = true;
                            if (!hasFlushPoint)
                            {
                                isUnnececcsaryBreakDot = true;
                                var iniloc = riserPoint;
                                var floorlines = FloorLines.Where(e => e.GetCenter().Y > iniloc.Y).OrderBy(e => e.GetCenter().Y - iniloc.Y);
                                double distance = 1600;
                                if (floorlines.Count() > 0) distance = floorlines.First().GetClosestPointTo(iniloc, true).DistanceTo(iniloc) - 200;
                                var uploc = iniloc + Vector3d.YAxis * distance;
                                var vertline = new Line(iniloc, uploc);
                                var leftuploc = vertline.GetCenter() - Vector3d.XAxis * (GetMarkLength(riser_dim) + 200);
                                var horline = new Line(leftuploc, vertline.GetCenter());
                                PreLines.Add(new PreLine(vertline, PipeLayerName, 1));
                                PreLines.Add(new PreLine(horline, DIMLAYER));
                                DrawText(DIMLAYER, riser_dim, leftuploc, 0.0);
                                DrawBreakDot(uploc);
                            }
                        }
                    }
                }

                //var cond = CrossedlayerDims.Count > 0 ? !CrossedlayerDims[CrossedlayerDims.Count - 1].Text.Equals(crossLayerDims) : true;
                //if (node.Item.Break != null && crossLayerDims != "" && cond)
                //{
                //    //如果是断线，绘制断线标注
                //    crossLayerDims = node.Item.Break.BreakName;
                //    if (!hasFlushPoint)
                //    {
                //        var iniloc = riserPoint;
                //        var uploc = iniloc + Vector3d.YAxis * 1600;
                //        var vertline = new Line(iniloc, uploc);
                //        var leftuploc = vertline.GetCenter() - Vector3d.XAxis * (GetMarkLength(crossLayerDims) + 200);
                //        var horline = new Line(leftuploc, vertline.GetCenter());
                //        PreLines.Add(new PreLine(vertline, "W-WSUP-DIMS"));
                //        PreLines.Add(new PreLine(horline, "W-WSUP-DIMS"));
                //        DrawText("W-WSUP-DIMS", crossLayerDims, leftuploc, 0.0);
                //    }
                //    else
                //    {
                //        var iniloc = markloc;
                //        var leftloc = markloc - Vector3d.XAxis * (GetMarkLength(crossLayerDims) + 200);
                //        var flushline = new Line(leftloc, iniloc);
                //        PreLines.Add(new PreLine(flushline, "W-WSUP-DIMS"));
                //        DrawText("W-WSUP-DIMS", crossLayerDims, leftloc, 0.0);
                //    }
                //}

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

        //public void DrawRisePipeOld(ref List<ThTreeNode<ThPointModel>> pointList, ref Point3d riserPoint
        //    , ref double height, ref Vector3d vvector, ref Vector3d hvector, ref int floorIndex, ref Vector3d mvector
        //    , bool hasFlushPoint, Point3d markloc, ref bool hascrossedpipe, Line rootLine = null)
        //{
        //    var iniriserPoint = riserPoint;
        //    List<Point3d> riserStartPoints = new List<Point3d>();
        //    for (int j = 0; j < pointList.Count; j++)
        //    {
        //        double curRiserLength = 0;
        //        var node = pointList[j];
        //        bool hasNode = false;
        //        string crossLayerDims = "";
        //        if (node.Item.Break != null)
        //        {
        //            crossLayerDims = node.Item.Break.BreakName;
        //        }
        //        if (node.Item.Riser != null && node.Item.Riser.RiserPts.Count > 0)
        //        {
        //            hasNode = true;
        //            var vPt1 = riserPoint;
        //            riserStartPoints.Add(vPt1);
        //            double vlength = FloorHeight / 2.0 - height - 200.0;
        //            if (vlength < 400)
        //            {
        //                vlength = 400;
        //            }
        //            var vPt2 = vPt1 + vvector * vlength;
        //            if (node.Item.Riser.RiserPts.Count == 0)
        //            {
        //                PreLines.Add(new PreLine(new Line(vPt1, vPt2), PipeLayerName, 1));
        //                if (node.Item.Riser.MarkName != null && node.Item.Riser.MarkName.Length > 0)
        //                {
        //                    var vpt3 = vPt1 + vvector * 300.0;
        //                    var hpt3 = vpt3 - hvector * GetMarkLength(node.Item.Riser.MarkName);
        //                    PreLines.Add(new PreLine(new Line(vpt3, hpt3), DIMLAYER));
        //                    //绘制立管标注
        //                    DrawText(DIMLAYER, node.Item.Riser.MarkName, hpt3, 0.0);
        //                }
        //            }
        //            //todo3：不能只考虑最后一个pointNode，要考虑所有的pointNode
        //            while (node.Item.Riser.RiserPts.Count != 0)
        //            {
        //                var firstPt = node.Item.Riser.RiserPts.First();
        //                node.Item.Riser.RiserPts.Remove(firstPt);
        //                var otherIndex = GetFloorIndex(firstPt, FloorList);
        //                if (otherIndex != floorIndex)
        //                {
        //                    hascrossedpipe = true;
        //                    bool isToCurFloor = false;
        //                    HelpLines.Add(new Line(vPt1, firstPt));
        //                    var compare_ini_lines = PreLines.Select(e => e.Line).ToList();
        //                    curRiserLength = DrawOtherFloor(vPt1, firstPt, floorIndex, otherIndex, ref isToCurFloor, crossLayerDims);
        //                    crossLayerDims = "";
        //                    //跨层立管太长，同层后面立管位置往前挪排版紧凑些
        //                    var compare_out_lines = PreLines.Select(e => e.Line).ToList();
        //                    var generated_lines = compare_out_lines.Except(compare_ini_lines);
        //                    var points = generated_lines.Select(e => e.GetCenter())
        //                        .Where(p => Math.Abs(p.Y - vPt1.Y) < 3000).OrderByDescending(p => p.X);
        //                    var point = vPt1;
        //                    if (points.Count() > 0) point = points.First();
        //                    curRiserLength = point.X - vPt1.X;
        //                    curRiserLength = curRiserLength >= 0 ? curRiserLength : 0;
        //                }
        //            }
        //        }
        //        var cond = CrossedlayerDims.Count > 0 ? !CrossedlayerDims[CrossedlayerDims.Count - 1].Text.Equals(crossLayerDims) : true;
        //        if (node.Item.Break != null && crossLayerDims != "" && cond /*&& isinSubEnd*/)
        //        {
        //            //如果是断线，绘制断线标注
        //            crossLayerDims = node.Item.Break.BreakName;
        //            if (!hasFlushPoint)
        //            {
        //                var iniloc = riserPoint;
        //                var uploc = iniloc + Vector3d.YAxis * 1600;
        //                var vertline = new Line(iniloc, uploc);
        //                var leftuploc = vertline.GetCenter() - Vector3d.XAxis * (GetMarkLength(crossLayerDims) + 200);
        //                var horline = new Line(leftuploc, vertline.GetCenter());
        //                PreLines.Add(new PreLine(vertline, DIMLAYER));
        //                PreLines.Add(new PreLine(horline, DIMLAYER));
        //                DrawText(DIMLAYER, crossLayerDims, leftuploc, 0.0);
        //            }
        //            else
        //            {
        //                var iniloc = markloc;
        //                var leftloc = markloc - Vector3d.XAxis * (GetMarkLength(crossLayerDims) + 200);
        //                var flushline = new Line(leftloc, iniloc);
        //                PreLines.Add(new PreLine(flushline, DIMLAYER));
        //                DrawText(DIMLAYER, crossLayerDims, leftloc, 0.0);
        //            }
        //        }
        //        else if (node.Item.Break != null && crossLayerDims != "" && cond)
        //        {
        //            //如果是断线，绘制断线标注
        //            crossLayerDims = node.Item.Break.BreakName;
        //            if (!hasFlushPoint)
        //            {
        //                var iniloc = riserPoint;
        //                var uploc = iniloc + Vector3d.YAxis * 400;
        //                if (iniloc.Y <= iniriserPoint.Y)
        //                    uploc = iniloc - Vector3d.YAxis * 1000;
        //                var leftuploc = uploc - Vector3d.XAxis * (GetMarkLength(crossLayerDims) + 200);
        //                var vertline = new Line(iniloc, uploc);
        //                var horline = new Line(leftuploc, uploc);
        //                PreLines.Add(new PreLine(vertline, DIMLAYER));
        //                PreLines.Add(new PreLine(horline, DIMLAYER));
        //                DrawText(DIMLAYER, crossLayerDims, leftuploc, 0.0);
        //            }
        //            else
        //            {
        //                var iniloc = markloc;
        //                var leftloc = markloc - Vector3d.XAxis * (GetMarkLength(crossLayerDims) + 200);
        //                var flushline = new Line(leftloc, iniloc);
        //                PreLines.Add(new PreLine(flushline, DIMLAYER));
        //                DrawText(DIMLAYER, crossLayerDims, leftloc, 0.0);
        //            }
        //        }
        //        if (hasNode)
        //        {
        //            riserPoint += hvector.GetNormal() * (curRiserLength + 1000);
        //        }
        //    }
        //    if (riserStartPoints.Count > 1)
        //    {
        //        for (int i = 0; i < riserStartPoints.Count - 1; i++)
        //        {
        //            var cond_a = rootLine == null;
        //            var cond_b = true;
        //            if (!cond_a)
        //            {
        //                cond_b = rootLine.GetClosestPointTo(riserStartPoints[i], true).DistanceTo(riserStartPoints[i]) < 1
        //                    && rootLine.GetClosestPointTo(riserStartPoints[i + 1], true).DistanceTo(riserStartPoints[i + 1]) < 1;
        //            }
        //            var cond_c = CreateVector(riserStartPoints[i], riserStartPoints[i + 1]).IsParallelTo(Vector3d.XAxis);
        //            var cond = cond_a && cond_b && cond_c;
        //            if (cond)
        //            {
        //                PreLines.Add(new PreLine(new Line(riserStartPoints[i], riserStartPoints[i + 1]), PipeLayerName, 0));
        //            }
        //        }
        //    }
        //}    
    }
}
