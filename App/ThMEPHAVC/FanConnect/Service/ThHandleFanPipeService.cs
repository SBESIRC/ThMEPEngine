using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Service;
using ThMEPHVAC.FanConnect.Command;
using ThMEPHVAC.FanConnect.Model;

namespace ThMEPHVAC.FanConnect.Service
{
    public class ThHandleFanPipeService
    {
        public Point3d StartPoint { set; get; }
        public List<Line> AllLine { set; get; }
        public List<ThFanCUModel> AllFan { set; get; }
        public List<Line> GetRightLine(ThFanTreeModel treeModel, Matrix3d mt)
        {
            var rightLines = FindRightLine(treeModel.RootNode);
            var tmpLines = ThFanConnectUtils.CleanLaneLines(rightLines);
            var retLines = new List<Line>();
            foreach (var l in tmpLines)
            {
                l.TransformBy(mt.Inverse());
                retLines.Add(l);
            }
            return retLines;
        }
        public List<Line> GetBadLine(ThFanTreeModel treeModel, Matrix3d mt)
        {
            var badLines = FindBadLine(treeModel.RootNode);
            var tmpLines = ThFanConnectUtils.CleanLaneLines(badLines);
            var retLines = new List<Line>();
            foreach (var l in tmpLines)
            {
                l.TransformBy(mt.Inverse());
                retLines.Add(l);
            }
            return retLines;
        }
        public ThFanTreeModel HandleFanPipe(Matrix3d mt)
        {
            ///将数据移动到原点附近
            foreach (var l in AllLine)
            {
                l.TransformBy(mt);
            }
            StartPoint = StartPoint.TransformBy(mt);
            foreach (var f in AllFan)
            {
                f.FanObb.TransformBy(mt);
                f.FanPoint = f.FanPoint.TransformBy(mt);
            }
            // 处理pipes 1.清除重复线段 ；2.将线在交点处打断
            ThLaneLineCleanService cleanServiec = new ThLaneLineCleanService();
            var allLineColles = cleanServiec.CleanNoding(AllLine.ToCollection());
            var tmpAllLines = new List<Line>();
            foreach (var l in allLineColles)
            {
                var line = l as Line;
                tmpAllLines.Add(line);
            }
            ThFanTreeModel treeModel = new ThFanTreeModel(StartPoint, tmpAllLines, 300);
            if (treeModel.RootNode == null)
            {
                return null;
            }
            foreach (var fcu in AllFan)
            {
                ThFanConnectUtils.FindFcuNode(treeModel.RootNode, fcu);
            }
            FindBadNode(treeModel.RootNode);
            foreach (var l in AllLine)
            {
                l.TransformBy(mt.Inverse());
            }
            foreach (var f in AllFan)
            {
                f.FanObb.TransformBy(mt.Inverse());
                f.FanPoint = f.FanPoint.TransformBy(mt.Inverse());
            }
            StartPoint = StartPoint.TransformBy(mt);
            return treeModel;
        }
        private void FindBadNode(ThFanTreeNode<ThFanPipeModel> node)
        {
            foreach (var item in node.Children)
            {
                FindBadNode(item);
            }
            if (node.Item.PipeLevel == PIPELEVEL.LEVEL1)
            {
                if (node.Children.Count == 0)
                {
                    node.Item.PipeLevel = PIPELEVEL.LEVEL2;
                }
                else
                {
                    bool isBad = true;
                    foreach (var child in node.Children)
                    {
                        if (child.Item.PipeLevel != PIPELEVEL.LEVEL2)
                        {
                            isBad = false;
                        }
                    }
                    if (isBad)
                    {
                        node.Item.PipeLevel = PIPELEVEL.LEVEL2;
                    }
                }
            }
        }
        private List<Line> FindRightLine(ThFanTreeNode<ThFanPipeModel> node)
        {
            var retLine = new List<Line>();
            foreach (var child in node.Children)
            {
                retLine.AddRange(FindRightLine(child));
            }
            if (node.Item.PipeLevel != PIPELEVEL.LEVEL2)
            {
                retLine.Add(node.Item.PLine);
            }
            return retLine;
        }
        private List<Line> FindBadLine(ThFanTreeNode<ThFanPipeModel> node)
        {
            var retLine = new List<Line>();
            foreach (var child in node.Children)
            {
                retLine.AddRange(FindBadLine(child));
            }
            if (node.Item.PipeLevel == PIPELEVEL.LEVEL2)
            {
                retLine.Add(node.Item.PLine);
            }
            return retLine;
        }
    }
}
