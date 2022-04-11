using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
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
            DrawRootNode(startPt, pipeTree.RootNode,  pipeTree.FloorIndex,ref mapLineList);
            //打断横管线
            var vLineList = new List<Line>();
            var hLineList = new List<Line>();
            foreach(var mapLine in mapLineList)
            {
                if(mapLine.LineType == 0)
                {
                    hLineList.Add(mapLine.Line);
                }
                else if(mapLine.LineType == 1)
                {
                    vLineList.Add(mapLine.Line);
                }
            }

            foreach(var vline in vLineList)
            {
                var lineList = new List<Line>();
                var cloosPts = new List<Point3d>();
                foreach(var hline in hLineList)
                {
                    Point3d clossPt = new Point3d();
                    if(IsClossLine(vline,hline,ref clossPt))
                    {
                        lineList.Add(hline);
                        cloosPts.Add(clossPt);
                    }
                }
                hLineList = hLineList.Except(lineList).ToList();
                for(int i = 0; i < lineList.Count;i++)
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
        public double DrawRootNode(Point3d basePt,ThTreeNode<ThPipeModel> rootNode, int floorIndex,ref List<ThMapLine> mapLineList)
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
            ThTreeNode<ThPointModel> startPointNode = rootNode.Item.PointNodeList.FirstOrDefault();
            for (int i = 0; i < rootNode.Children.Count; i++)
            {
                var subNode = rootNode.Children[i];
                var subPt1 = subPt + hvector * (sumLength + SubSpace * i);
                var subLength =  DrawSubNode(subPt1, subNode, height, floorIndex,ref mapLineList);
                sumLength += subLength;
                //绘制管径
                var endPointNode = subNode.Item.PointNodeList.First().Parent;
                var pointList = GetPointList(startPointNode, endPointNode);
                string dimMark = "";
                foreach(var pointNode in pointList)
                {
                    if(pointNode.Item.DimMark != null)
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
            //如果主节点连接的是立管，那么还需要绘制一段向上的线
            if (rootNode.Item.PointNodeList.Last().Item.Riser != null)
            {
                var vPt1 = rootPt2;
                double vlength = FloorHeight / 2.0 - height - 200.0;
                if(vlength < 400)
                {
                    vlength = 400;
                }
                var vPt2 = vPt1 + vvector * vlength;
                if(rootNode.Item.PointNodeList.Last().Item.Riser.RiserPts.Count == 0)
                {
                    DrawLine(vPt1, vPt2, "0", 0);
                    if (rootNode.Item.PointNodeList.Last().Item.Riser.MarkName != null)
                    {
                        var vpt3 = vPt1 + vvector * 300.0;
                        var hpt3 = vpt3 - hvector * GetMarkLength(rootNode.Item.PointNodeList.Last().Item.Riser.MarkName);
                        DrawLine(vpt3, hpt3, "0", 2);
                        //绘制立管标注
                        DrawText("0", rootNode.Item.PointNodeList.Last().Item.Riser.MarkName, hpt3, 0.0);
                    }
                }
                //todo3：不能只考虑最后一个pointNode，要考虑所有的pointNode
                while (rootNode.Item.PointNodeList.Last().Item.Riser.RiserPts.Count != 0)
                {
                    var firstPt = rootNode.Item.PointNodeList.Last().Item.Riser.RiserPts.First();
                    rootNode.Item.PointNodeList.Last().Item.Riser.RiserPts.Remove(firstPt);
                    var otherIndex = GetFloorIndex(firstPt, FloorList);
                    if (otherIndex != floorIndex)
                    {
                        bool isToCurFloor = false;
                        DrawOtherFloor(vPt1, firstPt, floorIndex, otherIndex,ref isToCurFloor,ref mapLineList);
                    }
                }
            }
            else if(rootNode.Item.PointNodeList.Last().Item.Break != null)
            {
                //如果是断线，绘制断线标注
                var mPt1 = rootPt2;
                var mPt2 = mPt1 + mvector * 1000.0;
                var mPt3 = mPt2 - hvector * GetMarkLength(rootNode.Item.PointNodeList.Last().Item.Break.BreakName);
                DrawLine(mPt1, mPt2, "0", 1);
                DrawLine(mPt2, mPt3, "0", 1);
                DrawText("0", rootNode.Item.PointNodeList.Last().Item.Break.BreakName, mPt3, 0.0);
            }
            //ToDo1:如果有水角阀平面，绘制向下的线
            //ToDo2:如果有阀门插入阀门
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
        public double DrawSubNode(Point3d basePt, ThTreeNode<ThPipeModel> subNode, double height, int floorIndex, ref List<ThMapLine> mapLineList)
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
                var subLength = DrawSubNode(childPt1, childNode, height, floorIndex,ref mapLineList);
                sumLength += subLength;
                //绘制管径
                var endPointNode = childNode.Item.PointNodeList.First().Parent;
                var pointList = GetPointList(startPointNode, endPointNode);
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
                //ToDo2:如果有阀门插入阀门
            }
            var cuLength = startLength + sumLength; 
            if(subNode.Children.Count == 0)
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
            //如果节点连接的是立管，那么还需要绘制一段向上的线
            if (subNode.Item.PointNodeList.Last().Item.Riser != null)
            {
                var vPt1 = hLinePt2;
                double vlength = FloorHeight / 2.0 - height - 200.0;
                if (vlength < 400)
                {
                    vlength = 400;
                }
                var vPt2 = vPt1 + vvector * vlength;
                if (subNode.Item.PointNodeList.Last().Item.Riser.RiserPts.Count == 0)
                {
                    DrawLine(vPt1, vPt2, "0", 1);
                    if (subNode.Item.PointNodeList.Last().Item.Riser.MarkName != null)
                    {
                        //绘制立管标注
                        var vpt3 = vPt1 + vvector * 1000.0;
                        var hpt3 = vpt3 - hvector * 500.0;
                        DrawLine(vpt3, hpt3, "0", 2);
                        DrawText("0", subNode.Item.PointNodeList.Last().Item.Riser.MarkName, hpt3, 0.0);
                    }
                }
                //todo3：不能只考虑最后一个pointNode，要考虑所有的pointNode
                while (subNode.Item.PointNodeList.Last().Item.Riser.RiserPts.Count != 0)
                {
                    var firstPt = subNode.Item.PointNodeList.Last().Item.Riser.RiserPts.First();
                    subNode.Item.PointNodeList.Last().Item.Riser.RiserPts.Remove(firstPt);
                    var otherIndex = GetFloorIndex(firstPt, FloorList);
                    if (otherIndex != floorIndex)
                    {
                        bool isToCurFloor = false;
                        var floorLength = DrawOtherFloor(vPt1, firstPt, floorIndex,otherIndex,ref isToCurFloor,ref mapLineList);
                        if(isToCurFloor)
                        {
                            cuLength += floorLength;
                        }
                    }
                }
            }
            else if (subNode.Item.PointNodeList.Last().Item.Break != null)
            {
                //如果是断线，绘制断线标注
                var mPt1 = hLinePt2;
                var mPt2 = mPt1 + mvector * 1000.0;
                var mPt3 = mPt2 - hvector * GetMarkLength(subNode.Item.PointNodeList.Last().Item.Break.BreakName);
                DrawLine(mPt1, mPt2, "0", 2);
                DrawLine(mPt2, mPt3, "0", 2);
                DrawText("0", subNode.Item.PointNodeList.Last().Item.Break.BreakName, mPt3, 0.0);
            }
            //ToDo1:如果有水角阀平面，绘制向下的线
            //ToDo2:如果有阀门插入阀门
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
        public double DrawOtherFloor(Point3d basePt, Point3d startPt,int curFloorIndex, int otherFloorIndex,ref bool isToCurFloor, ref List<ThMapLine> mapLineList)
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
                floorLength = DrawRootNode(otherStartPt, pipeTree.RootNode, otherFloorIndex,ref mapLineList);
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
            foreach(var child in node.Children)
            {
                retList.AddRange(FindCrossFloorIndexList(child));
            }
            if (node.Item.PointNodeList.Last().Item.Riser != null)
            {
                if(node.Item.PointNodeList.Last().Item.Riser.RiserPts.Count != 0)
                {
                    foreach(var pt in node.Item.PointNodeList.Last().Item.Riser.RiserPts)
                    {
                        var floorIndex = GetFloorIndex(pt, FloorList);
                        retList.Add(floorIndex);
                    }
                }
            }
            return retList;
        }
        public bool IsClossLine(Line vline,Line hline,ref Point3d clossPt)
        {
            bool isCloss = false;
            var pts = new Point3dCollection();
            vline.IntersectWith(hline,Intersect.OnBothOperands, pts,(IntPtr)0, (IntPtr)0);
            if(pts.Count > 0)
            {
                clossPt = pts[0];
                if(clossPt.DistanceTo(vline.StartPoint) > 10.0
                    && clossPt.DistanceTo(vline.EndPoint) > 10.0
                    && clossPt.DistanceTo(hline.StartPoint) > 10.0
                    && clossPt.DistanceTo(hline.EndPoint) > 10.0)
                {
                    isCloss = true;
                }
            }
            return isCloss;
        }
        public List<Line> BreakLine(Line hline , Point3d pt)
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
