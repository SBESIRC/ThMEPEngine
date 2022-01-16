using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
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
using ThMEPHVAC.FanLayout.Service;

namespace ThMEPHVAC.FanConnect.Service
{
    public class ThRemSurplusPipe
    {
        public Point3d StartPoint { set; get; }
        public List<Line> AllLine { set; get; }
        public List<ThFanCUModel> AllFan { set; get; }
        public List<Line> RemSurplusPipe()
        {
            var mt = Matrix3d.Displacement(StartPoint.GetVectorTo(Point3d.Origin));
            foreach(var l in AllLine)
            {
                l.TransformBy(mt);
            }
            // 处理pipes 1.清除重复线段 ；2.将线在交点处打断
            ThLaneLineCleanService cleanServiec = new ThLaneLineCleanService();
            var allLineColles = cleanServiec.CleanNoding(AllLine.ToCollection());
            var tmpAllLines = new List<Line>();
            foreach (var l in allLineColles)
            {
                var line = l as Line;
                line.TransformBy(mt.Inverse());
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
            //找到图纸上对应的线，进行删除
            string layer;
            int colorIndex;
            var dbObjs = GetDbPipes(StartPoint,out layer,out colorIndex);
            var tmpLines = new List<Line>();
            foreach(var obj in dbObjs)
            {
                if(obj is Line)
                {
                    var line = (obj as Line).Clone() as Line;
                    tmpLines.Add(line);
                }
                else if(obj is Polyline)
                {
                    var lines = (obj as Polyline).ToLines();
                    tmpLines.AddRange(lines);
                }
                obj.UpgradeOpen();
                obj.Erase();
                obj.DowngradeOpen();
            }
            var badLines = GetBadLine(treeModel.RootNode);
            foreach(var bad in badLines)
            {
                RemoveBadLine(bad, ref tmpLines);
            }
            DrawLines(tmpLines, layer, colorIndex);
            return tmpLines;
        }
        public List<Entity> GetDbPipes(Point3d startPt,out string layer,out int colorIndex)
        {
            using (var database = AcadDatabase.Active())
            {
                string tmpLayer = "AI-水管路由";
                int tmpIndex = 0;
                var box = ThDrawTool.CreateSquare(startPt.TransformBy(Active.Editor.WCS2UCS()), 50.0);
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
                foreach(var l in tmpLines)
                {
                    if (l.Layer.ToUpper() == tmpLayer && l.ColorIndex == tmpIndex)
                    {
                        retLines.Add(l);
                    }
                }
                return retLines;
            }
        }
        public void FindBadNode(ThFanTreeNode<ThFanPipeModel> node)
        {
            foreach (var item in node.Children)
            {
                FindBadNode(item);
            }
            if (node.Item.PipeLevel == PIPELEVEL.LEVEL1)
            {
                if(node.Children.Count == 0)
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
        public List<Line> GetBadLine(ThFanTreeNode<ThFanPipeModel> node)
        {
            var retLine = new List<Line>();
            foreach(var child in node.Children)
            {
                retLine.AddRange(GetBadLine(child));
            }
            if(node.Item.PipeLevel == PIPELEVEL.LEVEL2)
            {
                retLine.Add(node.Item.PLine);
            }
            return retLine;
        }
        public void RemoveBadLine(Line badLine ,ref List<Line> lines)
        {
            var remLines = new List<Line>();
            foreach(var l in lines)
            {
                if (!badLine.IsParallelToEx(l))
                {
                    continue;
                }
                var startPt = l.StartPoint;
                var entPt = l.EndPoint;
                var box = badLine.ExtendLine(1.0).Buffer(10.0);
                if (box.Contains(startPt) && box.Contains(entPt))
                {
                    remLines.Add(l);
                }
                else if (box.Contains(startPt) && !box.Contains(entPt))
                {
                    var pts = box.IntersectWithEx(l);
                    l.StartPoint = pts[0];
                }
                else if (!box.Contains(startPt) && box.Contains(entPt))
                {
                    var pts = box.IntersectWithEx(l);
                    l.EndPoint = pts[0];
                }
                box.Dispose();
            }
            lines = lines.Except(remLines).ToList();
        }
        public void DrawLines(List<Line> lines,string layer,int colorIndex)
        {
            var toDbServiece = new ThFanToDBServiece();
            foreach(var l in lines)
            {
                toDbServiece.InsertEntity(l, layer, colorIndex);
            }
        }
    }
}
