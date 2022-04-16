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
        public List<ThFloorModel> FloorList { set; get; }//楼层和范围
        public List<ThRiserInfo> RiserList { set; get; }
        public List<PreLine> PreLines = new List<PreLine>();
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
        public void DrawMap(Point3d basePt, ThPipeTree pipeTree)
        {
            if (FloorList.Count <= 0)
            {
                return;
            }
            MapPostion = basePt;
            //绘制楼层线
            DrawFloorLines(basePt, FloorList, FloorHeight, FloorLength);
            //求出系统图的起点
            var startPt = GetMapStartPoint(basePt, FloorList, pipeTree.FloorIndex);
            DrawRootNode(startPt, pipeTree.RootNode, pipeTree.FloorIndex);
            //打断横管线
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
                PreLines.Add(new PreLine(new Line(tmpPt1, tmpPt2), "W-NOTE"));
                if (i < floorCount)
                {
                    DrawText("W-NOTE", floorList[i].FloorName, tmpPt1, 0.0);
                }
            }
            floorList.Reverse();
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
                    subPt1 = subPt1 + hvector * length;
                    var valvePt = subPt1 - hvector * (1000 * valves.Count);
                    sumLength += length;
                    InsertValves(ValveLayerName, valves, valvePt, hvector);
                }
                //绘制冲洗点位情况           
                bool sflushpointFound = false;
                foreach (var node in pointList)
                {
                    if (node.Item.FlushPoint.Valve != null)
                    {
                        sflushpointFound = true;
                        var flushPoint = node.Item.FlushPoint;
                        DrawFlushPoint(flushPoint, subPt1, vvector, hvector, rootLine);
                        break;
                    }
                }
                if (sflushpointFound)
                {
                    subPt1 += hvector * 1000;
                    sumLength += 1000;
                }
                //立管
                var riserPoint = subPt1;
                DrawRisePipe(ref pointList, ref riserPoint, ref height, ref vvector, ref hvector,
                    ref floorIndex, ref mvector, rootLine);
                sumLength += riserPoint.DistanceTo(subPt1);
                subPt1 = riserPoint;
                //绘制管径              
                string sdimMark = "";
                foreach (var pointNode in pointList)
                {
                    if (pointNode.Item.DimMark != null)
                    {
                        sdimMark = pointNode.Item.DimMark.StrText;
                    }
                }
                var sdimPt = subPt1 - hvector * 1000.0;
                DrawText("W-WSUP-DIMS", sdimMark, sdimPt, 0.0);
                //画子节点
                var subLength = DrawSubNode(subPt1, subNode, height, floorIndex, rootLine);
                sumLength += subLength;
                startPointNode = endPointNode;
                //ToDo2:如果有阀门插入阀门
            }
            //绘制主干线
            double rootLength = startLength + endLength + sumLength;//第一段2000，末尾段1000
            rootLength += SubSpace * (rootNode.Children.Count - 1);//子节点的间隔1000
            Point3d rootPt1 = basePt;
            Point3d rootPt2 = basePt + hvector * rootLength;
            var hLine1 = new Line(rootPt1, rootPt2);
            PreLines.Add(new PreLine(new Line(rootPt1, rootPt2), PipeLayerName, 0));
            //绘制冲洗点位情况
            var _pointList = GetPointList(startPointNode, rootNode.Item.PointNodeList.LastOrDefault());
            bool flushpointFound = false;
            foreach (var node in _pointList)
            {
                if (node.Item.FlushPoint.Valve != null)
                {
                    flushpointFound = true;
                    var flushPoint = node.Item.FlushPoint;
                    DrawFlushPoint(flushPoint, rootPt2, vvector, hvector, rootLine);
                    break;
                }
            }
            if (flushpointFound)
            {
                rootPt2 += hvector * 1000;
                sumLength += 1000;
            }
            //立管
            var _riserPoint = rootPt2;
            DrawRisePipe(ref _pointList, ref _riserPoint, ref height, ref vvector, ref hvector,
                ref floorIndex, ref mvector, rootLine);
            sumLength += _riserPoint.DistanceTo(rootPt2);
            rootPt2 = _riserPoint;
            //绘制管径
            var rootPointList = GetPointList(startPointNode, rootNode.Item.PointNodeList.LastOrDefault());
            string dimMark1 = "";
            foreach (var pointNode in rootPointList)
            {
                if (pointNode.Item.DimMark != null)
                {
                    dimMark1 = pointNode.Item.DimMark.StrText;
                }
            }
            var dimPt1 = rootPt2 - hvector * 1000.0;
            DrawText("W-WSUP-DIMS", dimMark1, dimPt1, 0.0);
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
            PreLines.Add(new PreLine(new Line(vLinePt1, vLinePt2), PipeLayerName));
            Point3d hLinePt1 = vLinePt2;
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
                    childPt1 = childPt1 + hvector * length;
                    sumLength += length;
                    var valvePt = childPt1 - hvector * (1000.0 * valves.Count);
                    InsertValves(ValveLayerName, valves, valvePt, hvector);
                }
                //绘制冲洗点位情况             
                bool sflushpointFound = false;
                foreach (var node in pointList)
                {
                    if (node.Item.FlushPoint.Valve != null)
                    {
                        sflushpointFound = true;
                        var flushPoint = node.Item.FlushPoint;
                        DrawFlushPoint(flushPoint, childPt1, vvector, hvector, rootLine);
                        break;
                    }
                }
                if (sflushpointFound)
                {
                    childPt1 += hvector * 1000;
                    sumLength += 1000;
                }
                //立管
                var riserPoint = childPt1;
                DrawRisePipe(ref pointList, ref riserPoint, ref height, ref vvector, ref hvector,
                    ref floorIndex, ref mvector);
                sumLength += riserPoint.DistanceTo(childPt1);
                childPt1 = riserPoint;
                //绘制管径              
                string sdimMark = "";
                foreach (var pointNode in pointList)
                {
                    if (pointNode.Item.DimMark != null)
                    {
                        sdimMark = pointNode.Item.DimMark.StrText;
                    }
                }
                var sdimPt = childPt1 - hvector * 1000.0;
                DrawText("W-WSUP-DIMS", sdimMark, sdimPt, 0.0);
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
            //绘制冲洗点位情况
            var _pointList = GetPointList(startPointNode, subNode.Item.PointNodeList.LastOrDefault());
            bool flushpointFound = false;
            foreach (var node in _pointList)
            {
                if (node.Item.FlushPoint.Valve != null)
                {
                    flushpointFound = true;
                    var flushPoint = node.Item.FlushPoint;
                    DrawFlushPoint(flushPoint, hLinePt2, vvector, hvector, rootLine);
                    break;
                }
            }
            if (flushpointFound)
            {
                hLinePt2 += hvector * 1000;
                cuLength += 1000;
            }
            //绘制当前节点干线
            PreLines.Add(new PreLine(new Line(hLinePt1, hLinePt2), PipeLayerName, 0));
            var hLine1 = new Line(hLinePt1, hLinePt2);
            //立管
            var _riserPoint = hLinePt2;
            DrawRisePipe(ref _pointList, ref _riserPoint, ref height, ref vvector, ref hvector,
                ref floorIndex, ref mvector);
            cuLength += _riserPoint.DistanceTo(hLinePt2);
            hLinePt2 = _riserPoint;
            //绘制管径
            var rootPointList = GetPointList(startPointNode, subNode.Item.PointNodeList.LastOrDefault());
            string dimMark1 = "";
            foreach (var pointNode in rootPointList)
            {
                if (pointNode.Item.DimMark != null)
                {
                    dimMark1 = pointNode.Item.DimMark.StrText;
                }
            }
            var dimPt1 = hLinePt2 - hvector * 1000.0;
            DrawText("W-WSUP-DIMS", dimMark1, dimPt1, 0.0);
            return cuLength;
        }

        public void DrawFlushPoint(ThFlushPointModel flushPoint, Point3d basePt, Vector3d vvector, Vector3d hvector, Line rootLine = null)
        {
            var vertLength = 400.0;
            if (rootLine != null)
                vertLength += rootLine.GetClosestPointTo(basePt, true).DistanceTo(basePt);
            var vDownPt1 = basePt;
            var vDownPt2 = vDownPt1 - vvector * vertLength;
            PreLines.Add(new PreLine(new Line(vDownPt1, vDownPt2), PipeLayerName, 1));
            var vDownPt3 = vDownPt2 + hvector * 1000;
            PreLines.Add(new PreLine(new Line(vDownPt2, vDownPt3), PipeLayerName, 0));
            var line2 = new Line(vDownPt2, vDownPt3);
            var vDownPt4 = vDownPt3 - vvector * 1000.0;
            PreLines.Add(new PreLine(new Line(vDownPt3, vDownPt4), PipeLayerName, 1));
            using (var adb = AcadDatabase.Active())
            {
                var blId = adb.CurrentSpace.ObjectId.InsertBlockReference(
                    ValveLayerName, "皮带水嘴系统", vDownPt4, new Scale3d(1), 0);
                blId.SetDynBlockValue("可见性", "向右真空破坏组合");
                var br = adb.Element<BlockReference>(blId);
            }
        }
        public double DrawOtherFloor(Point3d basePt, Point3d startPt, int curFloorIndex, int otherFloorIndex, ref bool isToCurFloor)
        {
            Point3d otherPt = GetMapStartPoint(MapPostion, FloorList, otherFloorIndex);
            Point3d otherStartPt = new Point3d(basePt.X, otherPt.Y, 0.0);
            var vLine1 = new Line(basePt, otherStartPt);
            PreLines.Add(new PreLine(new Line(basePt, otherStartPt), PipeLayerName, 1));
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
            return floorLength;
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
            , Line rootLine = null)
        {
            List<Point3d> riserStartPoints = new List<Point3d>();
            for (int j = 0; j < pointList.Count; j++)
            {
                double curRiserLength = 0;
                var node = pointList[j];
                bool hasNode = false;
                if (node.Item.Riser != null)
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
                        if (node.Item.Riser.MarkName != null)
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
                            bool isToCurFloor = false;
                            curRiserLength = DrawOtherFloor(vPt1, firstPt, floorIndex, otherIndex, ref isToCurFloor);
                        }
                    }
                }
                if (node.Item.Break != null)
                {
                    //如果是断线，绘制断线标注
                    var mPt1 = riserPoint;
                    var mPt2 = mPt1 + mvector * 1000.0;
                    var mPt3 = mPt2 - hvector * GetMarkLength(node.Item.Break.BreakName);
                    PreLines.Add(new PreLine(new Line(mPt1, mPt2), "W-WSUP-DIMS"));
                    PreLines.Add(new PreLine(new Line(mPt2, mPt3), "W-WSUP-DIMS"));
                    DrawText("W-WSUP-DIMS", node.Item.Break.BreakName, mPt3, 0.0);
                }
                if (hasNode)
                {
                    var p = riserPoint;
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
                    var blId = adb.CurrentSpace.ObjectId.InsertBlockReference(
                        layer, name, position, new Scale3d(1), 0);
                    var br = adb.Element<BlockReference>(blId);
                    br.Layer = layer;
                    position += vector.GetNormal() * 1000;
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
