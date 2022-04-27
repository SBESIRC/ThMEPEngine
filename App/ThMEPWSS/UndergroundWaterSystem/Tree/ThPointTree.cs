using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.UndergroundWaterSystem.Model;
using ThMEPWSS.UndergroundWaterSystem.Command;
using Dreambuild.AutoCAD;
using ThCADCore.NTS;
using ThCADExtension;
using static ThMEPWSS.UndergroundWaterSystem.Utilities.GeoUtils;
using ThMEPEngineCore.CAD;

namespace ThMEPWSS.UndergroundWaterSystem.Tree
{
    /// <summary>
    /// 中间结构，用来做遍历查找，生成ThPipeTree
    /// </summary>
    public class ThPointTree
    {
        public ThTreeNode<ThPointModel> RootNode { set; get; }
        public ThPointTree(Point3d startPt, List<Line> lines, List<ThRiserInfo> riserList, List<ThMarkModel> markList, List<ThDimModel> dimList, List<ThValveModel> valveList, List<ThFlushPointModel> flushPointList)
        {
            //构建根节点RootNode
            RootNode = CreateRootNode(startPt, lines, riserList, markList, dimList, valveList, flushPointList);
            BianLiTee(RootNode);
        }
        public ThTreeNode<ThPointModel> CreateRootNode(Point3d startPt, List<Line> lines, List<ThRiserInfo> riserList, List<ThMarkModel> markList, List<ThDimModel> dimList, List<ThValveModel> valveList, List<ThFlushPointModel> flushPointList)
        {
            var startLine = ThUndergroundWaterSystemUtils.FindStartLine(startPt, lines);
            var pointModel = new ThPointModel();
            pointModel.Position = startLine.StartPoint;
            var rootNode = new ThTreeNode<ThPointModel>(pointModel);
            InsertToNode(rootNode, riserList, markList, dimList, valveList, flushPointList, ref lines);
            return rootNode;
        }
        public void InsertToNode(ThTreeNode<ThPointModel> node, List<ThRiserInfo> riserList, List<ThMarkModel> markList, List<ThDimModel> dimList, List<ThValveModel> valveList, List<ThFlushPointModel> flushPointList, ref List<Line> lines)
        {
            //找到与node.Item.Position相连的线
            var point = node.Item.Position;
            //查找当前节点到父节点是否有管径标注
            if (node.Parent != null)
            {
                var pt1 = node.Parent.Item.Position;
                var pt2 = node.Item.Position;
                var line = new Line(pt1, pt2);
                var box = line.Buffer(650);
                var dims = new List<ThDimModel>();
                foreach (var dim in dimList)
                    if (box.Contains(dim.CentralPoint))
                        dims.Add(dim);
                if (dims.Count > 0)
                {
                    var dim = dims.OrderBy(e => line.GetClosestPointTo(e.CentralPoint, false).DistanceTo(e.Position)).First();
                    node.Item.DimMark = dim;
                }
            }
            //ToDo1:判断节点是否有给水角阀平面:皮带水嘴
            foreach (var flushPoint in flushPointList)
            {
                double tol = 80;
                if (point.DistanceTo(flushPoint.Point) <= tol)
                {
                    node.Item.FlushPoint = flushPoint;
                    break;
                }
            }
            //ToDo2:判断节点是否有阀门等
            if (node.Parent != null)
            {
                var pt1 = node.Parent.Item.Position;
                var pt2 = node.Item.Position;
                var line = new Line(pt1, pt2);
                line.EndPoint = line.EndPoint.TransformBy(Matrix3d.Displacement(CreateVector(line).GetNormal() * 1200));
                var box = line.Buffer(200);
                foreach (var valve in valveList)
                {
                    if (box.Contains(valve.Point))
                    {
                        node.Item.Valves.Add(valve);
                        //break;
                    }
                }
            }
            //查找当前节点是否有立管
            foreach (var riser in riserList)
            {
                if (MatchRiser(point, riser))
                {
                    node.Item.Riser = riser;
                    break;
                }
            }
            //查找当前节点是否有断线
            if (/*node.Item.Riser == null*/true)
            {
                foreach (var mark in markList)
                {
                    if (MatchMark(point, mark))
                    {
                        var breakModel = new ThBreakModel();
                        breakModel.BreakName = mark.MarkText;
                        node.Item.Break = breakModel;
                    }
                }
            }
            //查找当前节点到父节点这一段是否包含标注
            //查找当前节点到父节点这一段是否包含阀门等
            //后续有其他附加继续添加

            var conlines = FindConnectLine(point, ref lines);
            foreach (var l in conlines)
            {
                if (l.Length < 10.0)
                {
                    continue;
                }
                var childModel = new ThPointModel();
                childModel.Position = l.EndPoint;
                var childNode = new ThTreeNode<ThPointModel>(childModel);
                node.InsertChild(childNode);
                InsertToNode(childNode, riserList, markList, dimList, valveList, flushPointList, ref lines);
            }
        }
        private List<Line> FindConnectLine(Point3d pt, ref List<Line> lines)
        {
            var remLines = new List<Line>();
            var retLines = new List<Line>();
            foreach (var l in lines)
            {
                if (l.StartPoint.DistanceTo(pt) < 10)
                {
                    l.StartPoint = pt;
                    remLines.Add(l);
                    retLines.Add(l);
                }
                else if (l.EndPoint.DistanceTo(pt) < 10)
                {
                    l.EndPoint = l.StartPoint;
                    l.StartPoint = pt;
                    retLines.Add(l);
                    remLines.Add(l);
                }
                else if (l.GetDistToPoint(pt) < 10)
                {
                    //将l在pt处打断
                    var line1 = new Line(pt, l.StartPoint);
                    var line2 = new Line(pt, l.EndPoint);
                    retLines.Add(line1);
                    retLines.Add(line2);
                    remLines.Add(l);
                }
            }
            lines = lines.Except(remLines).ToList();
            return retLines;
        }
        public void BianLiTee(ThTreeNode<ThPointModel> startNode)
        {
            foreach (var child in startNode.Children)
            {
                BianLiTee(child);
            }
            int teeCount = 0;
            foreach (var child in startNode.Children)
            {
                if (teeCount < child.Item.TeeCount)
                {
                    teeCount = child.Item.TeeCount;
                }
            }
            startNode.Item.TeeCount = teeCount;
            if (startNode.Children.Count >= 2)
            {
                startNode.Item.TeeCount = teeCount + 1;
            }
        }
        public bool MatchRiser(Point3d pt, ThRiserInfo riser)
        {
            bool isMatch = false;
            double tol = 80;//老版本:50
            foreach (var p in riser.RiserPts)
            {
                if (p.DistanceTo(pt) < tol)
                {
                    isMatch = true;
                    riser.RiserPts.Remove(p);
                    break;
                }
            }
            return isMatch;
        }
        public bool MatchMark(Point3d pt, ThMarkModel mark)
        {
            bool isMatch = false;
            double tol = 100;
            if (pt.DistanceTo(mark.Poistion) < tol)
            {
                isMatch = true;
            }
            return isMatch;
        }
    }
}
