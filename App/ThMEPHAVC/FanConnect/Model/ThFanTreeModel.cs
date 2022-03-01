using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPHVAC.FanConnect.Command;

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
        public ThFanTreeModel(Point3d startPt, List<Line> lines,double space)
        {
            RootNode = GetRootNode(startPt,lines, space);
        }
        private ThFanTreeNode<ThFanPipeModel> GetRootNode(Point3d startPt, List<Line> lines, double space)
        {
            var line = FindStartLine(startPt, lines);
            if(line == null)
            {
                return null;
            }
            var pipeModel = new ThFanPipeModel(line, PIPELEVEL.LEVEL1, space);
            var rootNode = new ThFanTreeNode<ThFanPipeModel>(pipeModel);
            InsertNodeFromLines(rootNode,ref lines, space);
            return rootNode;
        }
        private Line FindStartLine(Point3d startPt,List<Line> lines)
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
        private List<Line> FindConnectLine(Point3d pt,ref List<Line> lines)
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
                else if(l.GetDistToPoint(pt) < 10)
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
        private List<Line> FindNearLine(Line line ,ref List<Line> lines)
        {
            var remLines = new List<Line>();
            var retLines = new List<Line>();
            foreach(var l in lines)
            {
                var startPt = l.StartPoint;
                var endPt = l.EndPoint;
                double startDist = line.GetDistToPoint(startPt);
                double endDist = line.GetDistToPoint(endPt);
                if (startDist < 10.0)
                {
                    var closPt = line.GetClosestPointTo(l.StartPoint, false);
                    l.StartPoint = closPt;
                    retLines.Add(l);
                    remLines.Add(l);
                }
                else if (endDist < 10.0)
                {
                    var closPt = line.GetClosestPointTo(l.EndPoint, false);
                    l.EndPoint = startPt;
                    l.StartPoint = closPt;
                    retLines.Add(l);
                    remLines.Add(l);
                }
                else if (ThFanConnectUtils.IsIntersects(line, l))
                {
                    var pts = ThFanConnectUtils.IntersectWithEx(line,l);
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
        private void InsertNodeFromLines(ThFanTreeNode<ThFanPipeModel> node,ref List<Line> lines, double space)
        {
            //取当前结点的末端点
            var endPt = node.Item.PLine.EndPoint;
            //获取该点相连的线
            var conlines = FindConnectLine(endPt,ref lines);
            //获取与该item.PLine中间相连的线
            var neaLines = FindNearLine(node.Item.PLine, ref lines);
            bool conFlag = false;
            if (conlines.Count > 1)
            {
                conFlag = true;
            }
            var basVector = node.Item.PLine.LineDirection().GetNormal();
            
            foreach (var l in conlines)
            {
                if(l.Length < 10.0)
                {
                    continue;
                }
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
                var childModel = new ThFanPipeModel(l, level, space);
                childModel.IsFlag = node.Item.IsFlag;
                if(isFlag)
                {
                    childModel.IsFlag = !node.Item.IsFlag;
                }
                childModel.IsConnect = true;
                childModel.CroVector = croVector;
                var childNode = new ThFanTreeNode<ThFanPipeModel>(childModel);
                node.InsertChild(childNode);
                InsertNodeFromLines(childNode,ref lines, space);
            }
            
            foreach (var l in neaLines)
            {
                if (l.Length < 10.0)
                {
                    continue;
                }
                bool isFlag = false;
                var tmpVector = l.LineDirection().GetNormal();
                var croVector = basVector.CrossProduct(tmpVector).GetNormal();
                if (croVector.Equals(new Vector3d(0.0, 0.0, -1.0)))
                {
                    isFlag = true;
                }
                var childModel = new ThFanPipeModel(l, PIPELEVEL.LEVEL2, space);
                childModel.IsFlag = node.Item.IsFlag;
                if (isFlag)
                {
                    childModel.IsFlag = !node.Item.IsFlag;
                }
                childModel.IsConnect = false;
                childModel.CroVector = croVector;
                var childNode = new ThFanTreeNode<ThFanPipeModel>(childModel);
                node.InsertChild(childNode);
                InsertNodeFromLines(childNode,ref lines, space);
            }
        }
    }
    public class ThPointTreeModel
    {
        public ThFanTreeNode<ThFanPointModel> RootNode { set; get; }
        public ThPointTreeModel(ThFanTreeNode<ThFanPipeModel> rootNode, List<ThFanCUModel> fan)
        {
            //构建PointTree
            RootNode = GetRootNode(rootNode);
            //计算每个结点的流量
            if (RootNode != null)
            {
                CalNodeValue(RootNode, fan);
                FindEndNode(RootNode);
//                RemEndNode(RootNode, PIPELEVEL.LEVEL2);
            }
        }
        ThFanTreeNode<ThFanPointModel> GetRootNode(ThFanTreeNode<ThFanPipeModel> treeModel)
        {
            var allLines = GetLinesFromNode(treeModel);
            var pointModel = new ThFanPointModel();
            pointModel.CntPoint = treeModel.Item.PLine.StartPoint;
            pointModel.IsFlag = treeModel.Item.IsFlag;
            var rootNode = new ThFanTreeNode<ThFanPointModel>(pointModel);
            InsertNodeFromPipeTree(rootNode, treeModel,ref allLines);
            return rootNode;
        }
        public void InsertNodeFromPipeTree(ThFanTreeNode<ThFanPointModel> node, ThFanTreeNode<ThFanPipeModel> treeModel, ref List<Line> lines)
        {
            var remLines = new List<Line>();
            foreach(var l in lines)
            {
                if (node.Item.CntPoint.IsEqualTo(l.StartPoint))
                {
                    var model = FindPipeModel(l, treeModel);
                    if(model != null)
                    {
                        var pointModel = new ThFanPointModel();
                        pointModel.CntPoint = l.EndPoint;
                        pointModel.IsFlag = model.IsFlag;
                        var pointNode = new ThFanTreeNode<ThFanPointModel>(pointModel);
                        node.InsertChild(pointNode);
                        remLines.Add(l);
                    }
                }
            }
            lines = lines.Except(remLines).ToList();
            foreach(var child in node.Children)
            {
                InsertNodeFromPipeTree(child, treeModel,ref lines);
            }
        }
        public void CalNodeValue(ThFanTreeNode<ThFanPointModel> node, List<ThFanCUModel> fans)
        {
            //优先计算子结点的值
            foreach(var child in node.Children)
            {
                CalNodeValue(child, fans);
            }
            if(node.Children.Count >= 2)
            {
                node.Item.IsCrossPoint = true;
            }
            if(node.Children.Count == 0)
            {
                foreach (var f in fans)
                {
                    var closetPt = f.FanObb.GetClosestPointTo(node.Item.CntPoint,false); 
                    if (closetPt.DistanceTo(node.Item.CntPoint) < 400.0)
                    {
                        node.Item.CoolCapa = f.CoolCapa;
                        node.Item.CoolFlow = f.CoolFlow;
                        node.Item.HotFlow = f.HotFlow;
                        break;
                    }
                }
            }
            else
            {
                foreach(var child in node.Children)
                {
                    node.Item.CoolCapa += child.Item.CoolCapa;
                    node.Item.CoolFlow += child.Item.CoolFlow;
                    node.Item.HotFlow += child.Item.HotFlow;
                }
            }
        }
        public List<Line> GetLinesFromNode(ThFanTreeNode<ThFanPipeModel> treeNode)
        {
            var retLines = new List<Line>();
            foreach (var child in treeNode.Children)
            {
                retLines.AddRange(GetLinesFromNode(child));
            }
            var pts = new List<Point3dEx>();
            pts.Add(new Point3dEx(treeNode.Item.PLine.StartPoint));
            foreach (var child in treeNode.Children)
            {
                if(!child.Item.IsConnect)
                {
                    pts.Add(new Point3dEx(child.Item.PLine.StartPoint));
                }
            }
            pts.Add(new Point3dEx(treeNode.Item.PLine.EndPoint));
            pts = pts.Distinct().ToList();
            pts = pts.OrderBy(o=> treeNode.Item.PLine.StartPoint.DistanceTo(o._pt)).ToList();
            for(int i = 0; i < pts.Count - 1;i++)
            {
                var tmpLine = new Line(pts[i]._pt, pts[i + 1]._pt);
                retLines.Add(tmpLine);
            }
            return retLines;
        }
        public ThFanPipeModel FindPipeModel(Line l, ThFanTreeNode<ThFanPipeModel> rootNode)
        {
            if(ThFanConnectUtils.IsContains(rootNode.Item.PLine,l))
            {
                return rootNode.Item;
            }

            foreach (var child in rootNode.Children)
            {
                var retModel = FindPipeModel(l, child);
                if (retModel != null)
                {
                    return retModel;
                }
            }
            return null;
        }
        public void FindFcuNode(ThFanTreeNode<ThFanPointModel> node)
        {
            node.Item.Level = PIPELEVEL.LEVEL2;
            if (node.Parent != null)
            {
                if (node.Parent.Children.Count == 1)
                {
                    FindFcuNode(node.Parent);
                }
            }
        }
        public void FindEndNode(ThFanTreeNode<ThFanPointModel> node)
        {
            foreach (var item in node.Children)
            {
                FindEndNode(item);
            }
            if(node.Children.Count == 0)
            {
                node.Item.Level = PIPELEVEL.LEVEL2;
                if(node.Parent != null)
                {
                    if (node.Parent.Children.Count == 1)
                    {
                        FindEndNode1(node.Parent);
                    }
                }
            }
        }
        public void FindEndNode1(ThFanTreeNode<ThFanPointModel> node)
        {
            node.Item.Level = PIPELEVEL.LEVEL2;
            if (node.Parent != null)
            {
                if (node.Parent.Children.Count == 1)
                {
                    FindEndNode1(node.Parent);
                }
            }
        }
        public void RemEndNode(ThFanTreeNode<ThFanPointModel> node, PIPELEVEL level)
        {
            foreach (var child in node.Children)
            {
                if (child.Item.Level != level)
                {
                    RemEndNode(child, level);
                }
            }
            node.Children = node.Children.Where(o => o.Item.Level != level).ToList();
        }
        public void RemEndNode(ThFanTreeNode<ThFanPointModel> node, PIPELEVEL level,bool isCodeAndHotPipe,bool isCwPipe,ref List<Entity> marks)
        {
            var remChildren = new List<ThFanTreeNode<ThFanPointModel>>();
            foreach (var child in node.Children)
            {
                if (child.Item.Level != level)
                {
                    RemEndNode(child, level , isCodeAndHotPipe, isCwPipe, ref marks);
                }
            }
            foreach(var child in node.Children)
            {
                if(child.Item.Level == level)
                {
                    remChildren.Add(child);
                    //删除该节点的mark
                    if(isCodeAndHotPipe)
                    {
                        RemCodeAndHotPipeMark(child, ref marks);
                    }
                    if(isCwPipe)
                    {
                        RemCwPipeMark(child, ref marks);
                    }
                }
            }
            node.Children = node.Children.Except(remChildren).ToList();
        }
        public void RemCodeAndHotPipeMark(ThFanTreeNode<ThFanPointModel> node, ref List<Entity> marks)
        {
            if (node.Parent != null)
            {
                var line = new Line(node.Item.CntPoint, node.Parent.Item.CntPoint);
                var remMarks = FindMarkFromLine(line, ref marks);
                foreach(var m in remMarks)
                {
                    m.UpgradeOpen();
                    m.Erase();
                    m.DowngradeOpen();
                }
            }
        }
        public void RemCwPipeMark(ThFanTreeNode<ThFanPointModel> node, ref List<Entity> marks)
        {
            if (node.Parent != null)
            {
                var line = new Line(node.Item.CntPoint, node.Parent.Item.CntPoint);
                var remMark = FindTextFromLine(line, ref marks);
                foreach(var m in remMark)
                {
                    m.UpgradeOpen();
                    m.Erase();
                    m.DowngradeOpen();
                }
            }
        }
        public List<Entity> FindMarkFromLine(Line line, ref List<Entity> marks)
        {
            var box = line.Buffer(650);
            var retMark = new List<Entity>();
            foreach (var mark in marks)
            {
                if (mark is BlockReference)
                {
                    var blk = mark as BlockReference;
                    if (box.Contains(blk.Position))
                    {
                        if (blk.GetEffectiveName().Contains("AI-水管多排标注"))
                        {
                            retMark.Add(mark);
                        }
                    }
                }
            }
            marks = marks.Except(retMark).ToList();
            return retMark;
        }
        public List<Entity> FindTextFromLine(Line line, ref List<Entity> marks)
        {
            var box = line.Buffer(400);
            var retText = new List<Entity>();
            foreach (var mark in marks)
            {
                if (mark is DBText)
                {
                    var text = mark as DBText;
                    if (box.Contains(text.AlignmentPoint))
                    {
                        retText.Add(mark);
                    }
                }
            }
            marks = marks.Except(retText).ToList();
            return retText;
        }
    }
}
