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
    public class ThMapLine
    {
        public int LineType { set; get; }//0代表横管线,1代表立管线
        public Line Line { set; get; }
    }

    public class ThSystemMapService
    {
        public double SubSpace { set; get; }//节点间隔距离
        public double FloorLength { set; get; }//楼层线长度
        public double FloorHeight { set; get; }//楼层高度
        public Point3d MapPostion { set; get; }//系统图基点
        public List<ThFloorModel> FloorList { set; get; }//楼层和范围
        public List<ThRiserInfo> RiserList { set; get; }
        public Matrix3d Mt { set; get; }
        public ThSystemMapService()
        {
            SubSpace = 1000.0;
            FloorLength = 100000.0;
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
            var mapLineList = new List<ThMapLine>();
            DrawRootNode(startPt, pipeTree.RootNode, pipeTree.FloorIndex, ref mapLineList);
            //打断横管线
            var vLineList = new List<Line>();
            var hLineList = new List<Line>();
            foreach (var mapLine in mapLineList)
            {
                if (mapLine.LineType == 0)
                {
                    hLineList.Add(mapLine.Line);
                }
                else if (mapLine.LineType == 1)
                {
                    vLineList.Add(mapLine.Line);
                }
            }

            foreach (var vline in vLineList)
            {
                var lineList = new List<Line>();
                var cloosPts = new List<Point3d>();
                foreach (var hline in hLineList)
                {
                    Point3d clossPt = new Point3d();
                    if (IsClossLine(vline, hline, ref clossPt))
                    {
                        lineList.Add(hline);
                        cloosPts.Add(clossPt);
                    }
                }
                hLineList = hLineList.Except(lineList).ToList();
                for (int i = 0; i < lineList.Count; i++)
                {
                    hLineList.AddRange(BreakLine(lineList[i], cloosPts[i]));
                }
            }

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
                DrawLine(tmpPt1, tmpPt2, "0", 1);
                if (i < floorCount)
                {
                    DrawText("0", floorList[i].FloorName, tmpPt1, 0.0);
                }
            }
            floorList.Reverse();
        }
        public double DrawRootNode(Point3d basePt, ThTreeNode<ThPipeModel> rootNode, int floorIndex, ref List<ThMapLine> mapLineList)
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
                    InsertValves("0", valves, valvePt, hvector);
                }
                //立管
                var riserPoint = subPt1;
                DrawRisePipe(ref pointList, ref riserPoint, ref height, ref vvector, ref hvector,
                    ref floorIndex, ref mvector, ref mapLineList, rootLine);
                sumLength += riserPoint.DistanceTo(subPt1);
                subPt1 = riserPoint;
                //画子节点
                var subLength = DrawSubNode(subPt1, subNode, height, floorIndex, ref mapLineList, rootLine);
                sumLength += subLength;
                //绘制管径
                //var endPointNode = subNode.Item.PointNodeList.First().Parent;
                //var pointList = GetPointList(startPointNode, endPointNode);
                string dimMark = "";
                foreach (var pointNode in pointList)
                {
                    if (pointNode.Item.DimMark != null)
                    {
                        dimMark = pointNode.Item.DimMark.StrText;
                    }
                }
                var dimPt = subPt1 - hvector * 1000.0;
                DrawText("0", dimMark, dimPt, 0.0);
                startPointNode = endPointNode;
                //ToDo2:如果有阀门插入阀门
            }
            //绘制主干线
            double rootLength = startLength + endLength + sumLength;//第一段2000，末尾段1000
            rootLength += SubSpace * (rootNode.Children.Count - 1);//子节点的间隔1000
            Point3d rootPt1 = basePt;
            Point3d rootPt2 = basePt + hvector * rootLength;
            var hLine1 = DrawLine(rootPt1, rootPt2, "0", 1);
            var mapLine1 = new ThMapLine();
            mapLine1.LineType = 0;
            mapLine1.Line = hLine1;
            mapLineList.Add(mapLine1);
            //绘制冲洗点位情况
            var lastItem = rootNode.Item.PointNodeList.LastOrDefault();
            if (lastItem.Item.FlushPoint.Valve != null)
            {
                var flushPoint = lastItem.Item.FlushPoint;
                DrawFlushPoint(flushPoint, rootPt2 - hvector * 200, vvector, hvector);
            }
            //立管
            var _riserPoint = rootPt2;
            var _pointList = GetPointList(startPointNode, rootNode.Item.PointNodeList.LastOrDefault());
            DrawRisePipe(ref _pointList, ref _riserPoint, ref height, ref vvector, ref hvector,
                ref floorIndex, ref mvector, ref mapLineList, rootLine);
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
            DrawText("0", dimMark1, dimPt1, 0.0);
            return rootLength;
        }
        public double DrawSubNode(Point3d basePt, ThTreeNode<ThPipeModel> subNode, double height, int floorIndex, ref List<ThMapLine> mapLineList, Line rootLine)
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
            DrawLine(vLinePt1, vLinePt2, "0", 1);
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
                    InsertValves("0", valves, valvePt, hvector);
                }
                //立管
                var riserPoint = childPt1;
                DrawRisePipe(ref pointList, ref riserPoint, ref height, ref vvector, ref hvector,
                    ref floorIndex, ref mvector, ref mapLineList);
                sumLength += riserPoint.DistanceTo(childPt1);
                childPt1 = riserPoint;
                //绘制子节点
                var subLength = DrawSubNode(childPt1, childNode, height, floorIndex, ref mapLineList, rootLine);
                sumLength += subLength;
                //绘制管径
                //var endPointNode = childNode.Item.PointNodeList.First().Parent;
                //var pointList = GetPointList(startPointNode, endPointNode);
                string dimMark = "";
                foreach (var pointNode in pointList)
                {
                    if (pointNode.Item.DimMark != null)
                    {
                        dimMark = pointNode.Item.DimMark.StrText;
                    }
                }
                var dimPt = childPt1 - hvector * 1000.0;
                DrawText("0", dimMark, dimPt, 0.0);
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
            //绘制当前节点干线
            Point3d hLinePt2 = vLinePt2 + hvector * cuLength;
            var hLine1 = DrawLine(hLinePt1, hLinePt2, "0", 1);
            var mapLine1 = new ThMapLine();
            mapLine1.LineType = 0;
            mapLine1.Line = hLine1;
            //绘制冲洗点位情况
            var lastItem = subNode.Item.PointNodeList.LastOrDefault();
            if (lastItem.Item.FlushPoint.Valve != null)
            {
                var flushPoint = lastItem.Item.FlushPoint;
                DrawFlushPoint(flushPoint, hLinePt2 - hvector * 200, vvector, hvector, rootLine);
            }
            //立管
            var _riserPoint = hLinePt2;
            var _pointList = GetPointList(startPointNode, subNode.Item.PointNodeList.LastOrDefault());
            DrawRisePipe(ref _pointList, ref _riserPoint, ref height, ref vvector, ref hvector,
                ref floorIndex, ref mvector, ref mapLineList);
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
            DrawText("0", dimMark1, dimPt1, 0.0);
            return cuLength;
        }

        public void DrawFlushPoint(ThFlushPointModel flushPoint, Point3d basePt, Vector3d vvector, Vector3d hvector, Line rootLine = null)
        {
            var vertLength = 400.0;
            if (rootLine != null)
                vertLength += rootLine.GetClosestPointTo(basePt, true).DistanceTo(basePt);
            var vDownPt1 = basePt;
            var vDownPt2 = vDownPt1 - vvector * vertLength;
            DrawLine(vDownPt1, vDownPt2, "0", 1);
            var vDownPt3 = vDownPt2 + hvector * 500;
            DrawLine(vDownPt2, vDownPt3, "0", 1);
            var vDownPt4 = vDownPt3 - vvector * 1000.0;
            DrawLine(vDownPt3, vDownPt4, "0", 1);
            using (var adb = AcadDatabase.Active())
            {
                var blId = adb.CurrentSpace.ObjectId.InsertBlockReference(
                    "0", "皮带水嘴系统", vDownPt4, new Scale3d(1), 0);
                blId.SetDynBlockValue("可见性", "向右真空破坏组合");
                var br = adb.Element<BlockReference>(blId);
            }
        }
        public double DrawOtherFloor(Point3d basePt, Point3d startPt, int curFloorIndex, int otherFloorIndex, ref bool isToCurFloor, ref List<ThMapLine> mapLineList)
        {
            Point3d otherPt = GetMapStartPoint(MapPostion, FloorList, otherFloorIndex);
            Point3d otherStartPt = new Point3d(basePt.X, otherPt.Y, 0.0);
            var vLine1 = DrawLine(basePt, otherStartPt, "0", 1);
            var mapLine1 = new ThMapLine();
            mapLine1.LineType = 1;
            mapLine1.Line = vLine1;
            mapLineList.Add(mapLine1);
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
                floorLength = DrawRootNode(otherStartPt, pipeTree.RootNode, otherFloorIndex, ref mapLineList);
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
        public Line DrawLine(Point3d startPt, Point3d endPt, string layer, int colorIndex)
        {
            using (var database = AcadDatabase.Active())
            {
                var line = new Line(startPt, endPt);
                line.Layer = layer;
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
            , ref List<ThMapLine> mapLineList, Line rootLine = null)
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
                        DrawLine(vPt1, vPt2, "0", 0);
                        if (node.Item.Riser.MarkName != null)
                        {
                            var vpt3 = vPt1 + vvector * 300.0;
                            var hpt3 = vpt3 - hvector * GetMarkLength(node.Item.Riser.MarkName);
                            DrawLine(vpt3, hpt3, "0", 2);
                            //绘制立管标注
                            DrawText("0", node.Item.Riser.MarkName, hpt3, 0.0);
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
                            curRiserLength = DrawOtherFloor(vPt1, firstPt, floorIndex, otherIndex, ref isToCurFloor, ref mapLineList);
                        }
                    }
                }
                if (node.Item.Break != null)
                {
                    //如果是断线，绘制断线标注
                    var mPt1 = riserPoint;
                    var mPt2 = mPt1 + mvector * 1000.0;
                    var mPt3 = mPt2 - hvector * GetMarkLength(node.Item.Break.BreakName);
                    DrawLine(mPt1, mPt2, "0", 1);
                    DrawLine(mPt2, mPt3, "0", 1);
                    DrawText("0", node.Item.Break.BreakName, mPt3, 0.0);
                }
                if (hasNode)
                {
                    var p = riserPoint;
                    riserPoint += hvector.GetNormal() * (curRiserLength + 1000);
                    //var cond = rootLine != null && rootLine.GetClosestPointTo(riserPoint, true).DistanceTo(riserPoint) < 1;
                    //if (!cond)
                    //    DrawLine(p, riserPoint, "0", 1);
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
                        DrawLine(riserStartPoints[i], riserStartPoints[i + 1], "0", 1);
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
                    try
                    {
                        name = valve.Valve.GetEffectiveName();
                    }
                    catch
                    {
                        name = valve.Valve.Name;
                    }
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
        public List<Line> BreakLine(Line hline, Point3d pt)
        {
            var retList = new List<Line>();
            var hVector = new Vector3d(1.0, 0.0, 0.0);
            var pt1 = pt - hVector * 150.0;
            var pt2 = pt + hVector * 150.0;
            var line1 = DrawLine(hline.StartPoint, pt1, "0", 1);
            var line2 = DrawLine(pt2, hline.EndPoint, "0", 1);
            retList.Add(line1);
            retList.Add(line2);
            hline.UpgradeOpen();
            hline.Erase();
            hline.DowngradeOpen();
            return retList;
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
