using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPEngineCore.CAD;

namespace ThMEPHVAC.FanConnect.Model
{
    public class ThFanTreeNode<T>
    {
        /// <summary>
        /// 子节点
        /// </summary>
        public List<ThFanTreeNode<T>> Children { set; get; }
        /// <summary>
        /// 父节点
        /// </summary>
        public ThFanTreeNode<T> Parent { set; get; }
        /// <summary>
        /// 当前节点值
        /// </summary>
        public T Item { set; get; }
        public ThFanTreeNode(T item)
        {
            Children = new List<ThFanTreeNode<T>>();
            Parent = null;
            Item = item;
        }

        public void InsertChild(ThFanTreeNode<T> child)
        {
            child.Parent = this;
            Children.Add(child);
        }
        public ThFanTreeNode<T> GetChild(int index)
        {
            return Children[index];
        }
        public int ChildIndex(ThFanTreeNode<T> child)
        {
            for(int i = 0; i < Children.Count();i++)
            {
                if(Children[i] == child)
                {
                    return i;
                }
            }
            return -1;
        }
    }
    public class ThFanTreeModel
    {
        public ThFanTreeNode<ThFanPipeModel> RootNode { set; get; }
        public ThFanTreeModel(Point3d startPt, List<Line> lines)
        {
            RootNode = GetRootNode(startPt,lines);
        }
        ThFanTreeNode<ThFanPipeModel> GetRootNode(Point3d startPt, List<Line> lines)
        {
            var line = FindStartLine(startPt, lines);
            if(line == null)
            {
                return null;
            }
            var pipeModel = new ThFanPipeModel(line,PIPELEVEL.LEVEL1);
            var rootNode = new ThFanTreeNode<ThFanPipeModel>(pipeModel);
            InsertNodeFromLines(rootNode, lines);
            return rootNode;
        }

        Line FindStartLine(Point3d startPt,List<Line> lines)
        {
            foreach(var l in lines)
            {
                if(l.StartPoint.DistanceTo(startPt) < 10)
                {
                    lines.Remove(l);
                    return l;
                }
                else if(l.EndPoint.DistanceTo(startPt) < 10)
                {
                    var tmpPt = l.StartPoint;
                    l.StartPoint = l.EndPoint;
                    l.EndPoint = tmpPt;
                    lines.Remove(l);
                    return l;
                }
            }
            return null;
        }

        List<Line> FindConnectLine(Point3d pt,ref List<Line> lines)
        {
            var remLines = new List<Line>();
            var retLines = new List<Line>();
            foreach (var l in lines)
            {
                if (l.StartPoint.DistanceTo(pt) < 10)
                {
                    remLines.Add(l);
                    retLines.Add(l);
                }
                else if (l.EndPoint.DistanceTo(pt) < 10)
                {
                    var tmpPt = l.StartPoint;
                    l.StartPoint = l.EndPoint;
                    l.EndPoint = tmpPt;
                    retLines.Add(l);
                    remLines.Add(l);
                }
                else if(l.GetDistToPoint(pt) < 10)
                {
                    //将l在pt处打断
                    var closPt = l.GetClosestPointTo(pt,false);
                    var line1 = new Line(closPt, l.StartPoint);
                    var line2 = new Line(closPt, l.EndPoint);
                    retLines.Add(line1);
                    retLines.Add(line2);
                    remLines.Add(l);
                }
            }
            lines = lines.Except(remLines).ToList();
            return retLines;
        }
        List<Line> FindNearLine(Line line ,ref List<Line> lines)
        {
            var remLines = new List<Line>();
            var retLines = new List<Line>();
            foreach(var l in lines)
            {
                var startPt = l.StartPoint;
                var endPt = l.EndPoint;
                double startDist = line.GetDistToPoint(startPt);
                double endDist = line.GetDistToPoint(endPt);
                if(startDist < 10.0)
                {
                    retLines.Add(l);
                    remLines.Add(l);
                }
                else if (endDist < 10.0)
                {
                    l.StartPoint = endPt;
                    l.EndPoint = startPt;
                    retLines.Add(l);
                    remLines.Add(l);
                }
                else if(line.IsIntersects(l))
                {
                    var pts = l.IntersectWithEx(line);
                    //将l在交点位置打断
                    if (pts.Count > 0)
                    {
                        var line1 = new Line(pts[0], l.StartPoint);
                        var line2 = new Line(pts[0], l.EndPoint);
                        retLines.Add(line1);
                        retLines.Add(line2);
                    }
                    remLines.Add(l);
                }
            }
            lines = lines.Except(remLines).ToList();
            return retLines;
        }
        void InsertNodeFromLines(ThFanTreeNode<ThFanPipeModel> node, List<Line> lines)
        {
            //取当前结点的末端点
            var endPt = node.Item.PLine.EndPoint;
            //获取该点相连的线
            var conlines = FindConnectLine(endPt,ref lines);

            bool conFlag = false;
            if (conlines.Count > 1)
            {
                conFlag = true;
            }
            var basVector = node.Item.PLine.LineDirection().GetNormal();
            foreach (var l in conlines)
            {
                bool isFlag = false;
                var tmpVector = l.LineDirection().GetNormal();
                var croVector = basVector.CrossProduct(tmpVector).GetNormal();
                if (conFlag)
                {
                    if (croVector.Equals(new Vector3d(0.0, 0.0, -1.0)))
                    {
                        isFlag = true;
                    }
                }
                var level = PIPELEVEL.LEVEL1;
                if(node.Parent != null)
                {
                    level = node.Parent.Item.PipeLevel;
                }
                var childModel = new ThFanPipeModel(l, level);
                childModel.IsFlag = isFlag;
                if(node.Item.IsFlag)
                {
                    childModel.IsFlag = true;
                }
                childModel.IsConnect = true;
                childModel.CroVector = croVector;
                var childNode = new ThFanTreeNode<ThFanPipeModel>(childModel);
                node.InsertChild(childNode);
                InsertNodeFromLines(childNode, lines);
            }
            //获取与该item.PLine中间相连的线
            var neaLines = FindNearLine(node.Item.PLine,ref lines);
            foreach (var l in neaLines)
            {
                bool isFlag = false;
                var tmpVector = l.LineDirection().GetNormal();
                var croVector = basVector.CrossProduct(tmpVector).GetNormal();
                if (croVector.Equals(new Vector3d(0.0, 0.0, -1.0)))
                {
                    isFlag = true;
                }
                var childModel = new ThFanPipeModel(l, PIPELEVEL.LEVEL2);
                childModel.IsFlag = isFlag;
                if (node.Item.IsFlag)
                {
                    childModel.IsFlag = true;
                }
                childModel.IsConnect = false;
                childModel.CroVector = croVector;
                var childNode = new ThFanTreeNode<ThFanPipeModel>(childModel);
                node.InsertChild(childNode);
                InsertNodeFromLines(childNode, lines);
                
            }
        }
    }
}
