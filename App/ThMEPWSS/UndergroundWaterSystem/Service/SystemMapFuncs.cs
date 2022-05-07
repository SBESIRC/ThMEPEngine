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
            var length = GetMarkLength(StartMarkInfo);
            var leftpoint = vp - Vector3d.XAxis * length - Vector3d.XAxis * 300;
            var hline = new Line(leftpoint, vp);
            PreLines.Add(new PreLine(vline, DIMLAYER));
            PreLines.Add(new PreLine(hline, DIMLAYER));
            DrawText(DIMLAYER, StartMarkInfo, leftpoint, 0.0);
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
                PreLines.Add(new PreLine(fl, NOTELAYER));
                if (i < floorCount) DrawText(NOTELAYER, floorList[i].FloorName, tmpPt1, 0.0);
                else DrawText(NOTELAYER, "1F", tmpPt1, 0.0);
            }
            floorList.Reverse();
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
        private void DrawBreakName(List<ThTreeNode<ThPointModel>> pointList, Point3d point,bool upward)
        {
            string dim = "";
            for (int i = 0; i < pointList.Count; i++)
            {
                var node = pointList[i];
                if (node.Item.Break != null && node.Item.Break.BreakName != "" && !node.Item.Break.Used)
                {
                    dim = node.Item.Break.BreakName;
                    break;
                }
            }
            if (dim.Length == 0) return;
            var iniloc = point;
            var uploc = iniloc + Vector3d.YAxis * 400;
            if (!upward)
                uploc = iniloc - Vector3d.YAxis * 1000;
            var leftuploc = uploc - Vector3d.XAxis * (GetMarkLength(dim) + 200);
            var vertline = new Line(iniloc, uploc);
            var horline = new Line(leftuploc, uploc);
            PreLines.Add(new PreLine(vertline, DIMLAYER));
            PreLines.Add(new PreLine(horline, DIMLAYER));
            DrawText(DIMLAYER, dim, leftuploc, 0.0);
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
                    DrawFlushPoint(flushPoint, point, vvector, hvector, ref markLoc, isInChild, rootLine);
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
            return new Dim(dimPt1, dimMark1, p);
        }
        private void DrawDim(Dim dim)
        {
            DrawText(DIMLAYER, dim.Text, dim.Point, 0.0);
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
            DrawText(DIMLAYER, dimMark1, dimPt1, 0.0);
        }
        public void DrawFlushPoint(ThFlushPointModel flushPoint, Point3d basePt, Vector3d vvector, Vector3d hvector, ref Point3d markLoc, bool isInChild, Line rootLine = null)
        {
            var vertLength = 400.0;
            var vertdist = rootLine.GetClosestPointTo(basePt, true).DistanceTo(basePt);
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
        public void DrawBreakDot(Point3d point, double angle = 0)
        {
            using (var adb = AcadDatabase.Active())
            {
                var blId = adb.CurrentSpace.ObjectId.InsertBlockReference(
                    DIMLAYER, "断线", point, new Scale3d(1), angle);
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
                    if(name.Contains("752"))//不翻转方向的阀门
                        br.Rotation=0;
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
