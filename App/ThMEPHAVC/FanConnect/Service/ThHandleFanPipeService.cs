using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using GeometryExtensions;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
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
        public List<Line> CleanPipe(Matrix3d mt)
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

            return tmpAllLines;
        }
        public ThFanTreeModel HandleFanPipe(Matrix3d mt, List<Line> tmpAllLines)
        {
            /////将数据移动到原点附近
            //foreach (var l in AllLine)
            //{
            //    l.TransformBy(mt);
            //}
            //StartPoint = StartPoint.TransformBy(mt);
            //foreach (var f in AllFan)
            //{
            //    f.FanObb.TransformBy(mt);
            //    f.FanPoint = f.FanPoint.TransformBy(mt);
            //}
            //// 处理pipes 1.清除重复线段 ；2.将线在交点处打断
            //ThLaneLineCleanService cleanServiec = new ThLaneLineCleanService();
            //var allLineColles = cleanServiec.CleanNoding(AllLine.ToCollection());
            //var tmpAllLines = new List<Line>();
            //foreach (var l in allLineColles)
            //{
            //    var line = l as Line;
            //    tmpAllLines.Add(line);
            //}
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
            StartPoint = StartPoint.TransformBy(mt.Inverse());
            return treeModel;
        }
        public List<Entity> GetDbPipes(out string layer, out int colorIndex)
        {
            using (var database = AcadDatabase.Active())
            {
                string tmpLayer = "AI-水管路由";
                int tmpIndex = 0;
                var box = ThDrawTool.CreateSquare(StartPoint.TransformBy(Active.Editor.WCS2UCS()), 50.0);
                //以pt为中心，做一个矩形
                //找到改矩形内所有的Entity
                //遍历Entity找到目标层
                var psr = Active.Editor.SelectCrossingPolygon(box.Vertices());
                if (psr.Status == PromptStatus.OK)
                {
                    foreach (var id in psr.Value.GetObjectIds())
                    {
                        var entity = database.Element<Entity>(id);
                        if (entity.Layer.Contains("AI-水管路由") || entity.Layer.Contains("H-PIPE-C"))
                        {
                            tmpLayer = entity.Layer;
                            tmpIndex = entity.ColorIndex;
                            break;
                        }
                    }
                }
                layer = tmpLayer;
                colorIndex = tmpIndex;
                var retLines = new List<Entity>();
                var tmpLines = database.ModelSpace.OfType<Entity>();
                foreach (var l in tmpLines)
                {
                    if (l.Layer.ToUpper() == tmpLayer && l.ColorIndex == tmpIndex)
                    {
                        retLines.Add(l);
                    }
                }
                return retLines;
            }
        }
        public void RemoveDbPipe(ThFanTreeModel treeModel, List<Entity> dbObjs, Matrix3d mt)
        {
            //找到图纸上对应的线，进行删除
            foreach(var obj in dbObjs)
            {
                obj.UpgradeOpen();
                obj.TransformBy(mt);
                obj.DowngradeOpen();
            }
            //找到与起点相连的线
            var removeEntity = FindDbPipe(treeModel.RootNode, ref dbObjs);
            foreach (var obj in removeEntity)
            {
                obj.UpgradeOpen();
                obj.Erase();
                obj.DowngradeOpen();
            }
            //删除所有相连的线
            foreach (var obj in dbObjs)
            {
                obj.UpgradeOpen();
                obj.TransformBy(mt.Inverse());
                obj.DowngradeOpen();
            }
        }
        public List<Entity> FindDbPipe(ThFanTreeNode<ThFanPipeModel> node, ref List<Entity> objs)
        {
            var retEntity = new List<Entity>();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(objs.ToCollection());

            var box = node.Item.PLine.Buffer(10);
            var dbObjs = spatialIndex.SelectCrossingPolygon(box);
            foreach(var obj in dbObjs)
            {
                if(obj is Entity ent)
                {
                    retEntity.Add(ent);
                }
            }
            objs = objs.Except(retEntity).ToList();
            foreach(var child in node.Children)
            {
                retEntity.AddRange(FindDbPipe(child,ref objs));
            }
            return retEntity;
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
