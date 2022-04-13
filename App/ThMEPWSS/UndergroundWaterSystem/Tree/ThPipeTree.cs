using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPWSS.UndergroundWaterSystem.Model;
using ThMEPWSS.UndergroundWaterSystem.Tree;
using ThMEPWSS.UndergroundWaterSystem.Service;
using static ThMEPWSS.UndergroundWaterSystem.Utilities.GeoUtils;

namespace ThMEPWSS.UndergroundWaterSystem.Tree
{

    /// <summary>
    /// 一个楼层一个Tree
    /// 用来绘制系统图
    /// </summary>
    public class ThPipeTree
    {
        public int FloorIndex { set; get; }
        public ThTreeNode<ThPipeModel> RootNode { set; get; }
        public ThPipeTree(Point3d startPt, List<ThFloorModel> floorList, List<ThRiserInfo> riserInfo, Matrix3d mt)
        {
            //找到startPt属于哪个起点
            FloorIndex = GetFloorIndex(startPt, floorList);
            if(FloorIndex == -1)
            {
                return;
            }
            
            //找到该楼层的横管
            var pipeLines = floorList[FloorIndex].FloorInfo.PipeLines;
            if(pipeLines.Count == 0)
            {
                return;
            }
            var tmpLine = new List<Line>();
            foreach(var l in pipeLines)
            {
                var line = new Line(l.StartPoint, l.EndPoint);
                tmpLine.Add(line);
            }
            //处理横管数据
            var pipeHandleService = new ThPipeHandleService();
            var seriesLines = pipeHandleService.FindSeriesLine(startPt, tmpLine);
            if(seriesLines == null)
            {
                return;
            }
            var cleanedLines = pipeHandleService.CleanLines(seriesLines, mt);
            var points = new List<Point3d>();
            riserInfo.ForEach(e => points.AddRange(e.RiserPts));
            InterrptLineByPoints(cleanedLines, points);
            //todo3:在立管处打断cleanedLines
            //找到标注
            var markList = floorList[FloorIndex].FloorInfo.MarkList;
            //提取到管径
            var dimList = floorList[FloorIndex].FloorInfo.DimList;
            //提取到阀门
            var valveList = floorList[FloorIndex].FloorInfo.ValveList;
            //构建PointTree
            var pointTree = new ThPointTree(startPt, cleanedLines, riserInfo, markList, dimList,valveList);
            //构建RootNode
            RootNode = CreateRootNode(pointTree);
        }
        public ThTreeNode<ThPipeModel> CreateRootNode(ThPointTree pointTree)
        {
            //查找从开始节点经过三通或者四通最多的节点的最后一个三通或者四通节点
            var lastNode = FindLastNode(pointTree.RootNode);
            var pipeModel = new ThPipeModel();
            pipeModel.PointNodeList = GetPointList(pointTree.RootNode, lastNode);
            var rootNode = new ThTreeNode<ThPipeModel>(pipeModel);
            InsertToNode(rootNode, pipeModel.PointNodeList);
            return rootNode;
        }
        public void InsertToNode(ThTreeNode<ThPipeModel> node, List<ThTreeNode<ThPointModel>> nodes)
        {
            foreach(var n in nodes)
            {
                foreach(var c in n.Children)
                {
                    if(!c.Item.IsTraversal)
                    {
                        //插入一个
                        var lastNode = FindLastNode(c);
                        var pipeModel = new ThPipeModel();
                        pipeModel.PointNodeList = GetPointList(c, lastNode);
                        var childNode = new ThTreeNode<ThPipeModel>(pipeModel);
                        node.InsertChild(childNode);
                        InsertToNode(childNode, pipeModel.PointNodeList);
                    }
                }
            }
        }
        /// <summary>
        /// 查找从开始节点起经过三通或者四通最多节点的最后一个节点
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public ThTreeNode<ThPointModel> FindLastNode(ThTreeNode<ThPointModel> startNode)
        {
            ThTreeNode<ThPointModel> retNode = null;
            if(startNode.Children.Count == 0)
            {
                retNode = startNode;
                return retNode;
            }
            int teeCount = 0;
            foreach(var child in startNode.Children)
            {
                if(teeCount <= child.Item.TeeCount)
                {
                    teeCount = child.Item.TeeCount;
                    retNode = child;
                }
            }
            return FindLastNode(retNode);
        }
        public List<ThTreeNode<ThPointModel>> GetPointList(ThTreeNode<ThPointModel> rootNode, ThTreeNode<ThPointModel> lastNode)
        {
            var retNodes = new List<ThTreeNode<ThPointModel>>();
            if(lastNode.Parent == null || lastNode == rootNode)
            {
                lastNode.Item.IsTraversal = true;
                retNodes.Add(lastNode);
                return retNodes;
            }
            if(lastNode.Parent == rootNode)
            {
                lastNode.Item.IsTraversal = true;
                rootNode.Item.IsTraversal = true;
                retNodes.Add(lastNode);
                retNodes.Add(rootNode);
                retNodes.Reverse();
                return retNodes;
            }
            ThTreeNode<ThPointModel> tempNode = lastNode;
            while (tempNode.Parent != rootNode)
            {
                retNodes.Add(tempNode);
                tempNode.Item.IsTraversal = true;
                tempNode = tempNode.Parent;
            }
            tempNode.Item.IsTraversal = true;
            retNodes.Add(tempNode);
            rootNode.Item.IsTraversal = true;
            retNodes.Add(rootNode);
            retNodes.Reverse();
            return retNodes;
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
    }
}
