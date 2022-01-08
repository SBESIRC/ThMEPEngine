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

namespace ThMEPHVAC.FanConnect.Service
{
    public class ThRemSurplusPipe
    {
        public Point3d StartPoint { set; get; }
        public List<Line> AllLine { set; get; }
        public List<ThFanCUModel> AllFan { set; get; }
        public bool RemoveLine(Line remLine ,Line dbLine)
        {
            if(!remLine.IsParallelToEx(dbLine))
            {
                return false;
            }
            var startPt = dbLine.StartPoint;
            var entPt = dbLine.EndPoint;
            var box = remLine.ExtendLine(1.0).Buffer(10.0);
            if (box.Contains(startPt) && box.Contains(entPt))
            {
                dbLine.UpgradeOpen();
                dbLine.Erase();
                dbLine.DowngradeOpen();
                return true;
            }
            else if (box.Contains(startPt) && !box.Contains(entPt))
            {
                var pts = box.IntersectWithEx(dbLine);
                dbLine.UpgradeOpen();
                dbLine.StartPoint = pts[0];
                dbLine.DowngradeOpen();
            }
            else if (!box.Contains(startPt) && box.Contains(entPt))
            {
                var pts = box.IntersectWithEx(dbLine);
                dbLine.UpgradeOpen();
                dbLine.EndPoint = pts[0];
                dbLine.DowngradeOpen();
            }
            box.Dispose();
            return false;
        }
        public bool RemoveLine(Line remLine, Polyline dbLine)
        {
            var dbObjs = new DBObjectCollection();
            dbLine.Explode(dbObjs);
            foreach(var db in dbObjs)
            {
                if(RemoveLine(remLine,db as Line))
                {
                    return true;
                }
            }
            return false;
        }
        public void RemSurplusPipe()
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
                return;
            }
            foreach (var fcu in AllFan)
            {
                ThFanConnectUtils.FindFcuNode(treeModel.RootNode, fcu.FanPoint);
            }
            var remLine = FindEndLine(treeModel.RootNode);
            //找到图纸上对应的线，进行删除
            var dbObjs = GetDbPipes(StartPoint);
            foreach (var l in remLine)
            {
                foreach (var obj in dbObjs)
                {
                    if (obj is Line)
                    {
                        var line = obj as Line;
                        if (RemoveLine(l,line))
                        {
                            break;
                        }
                    }
                    else if (obj is Polyline)
                    {
                        var pline = obj as Polyline;
                        if(RemoveLine(l,pline))
                        {
                            break;
                        }
                    }
                }
            }
        }
        public List<Line> FindEndLine(ThFanTreeNode<ThFanPipeModel> node)
        {
            var retLine = new List<Line>();
            foreach (var child in node.Children)
            {
                retLine.AddRange(FindEndLine(child));
            }
            if (node.Children.Count != 0)
            {
                return retLine;
            }
            if (node.Item.PipeLevel != PIPELEVEL.LEVEL4)
            {
                if (node.Parent != null)
                {
                    retLine.Add(node.Item.PLine);
                }
            }

            return retLine;
        }
        public List<Entity> GetDbPipes(Point3d startPt)
        {
            using (var database = AcadDatabase.Active())
            {
                string layer = "AI-水管路由";
                var box = ThDrawTool.CreateSquare(startPt.TransformBy(Active.Editor.WCS2UCS()), 50.0);
                //以pt为中心，做一个矩形
                //找到改矩形内所有的Entity
                //遍历Entity找到目标层
                var psr = Active.Editor.SelectCrossingPolygon(box.Vertices());
                int colorIndex = 0;
                if (psr.Status == PromptStatus.OK)
                {
                    foreach (var id in psr.Value.GetObjectIds())
                    {
                        var entity = database.Element<Entity>(id);
                        if (entity.Layer.Contains("AI-水管路由") || entity.Layer.Contains("H-PIPE-C"))
                        {
                            layer = entity.Layer;
                            colorIndex = entity.ColorIndex;
                            break;
                        }
                    }
                }
                var retLines = new List<Line>();
                var tmpLines = database.ModelSpace.OfType<Entity>().Where(o => o.Layer.Contains(layer) && o.ColorIndex == colorIndex).ToList();
                return tmpLines;
            }
        }
    }
}
