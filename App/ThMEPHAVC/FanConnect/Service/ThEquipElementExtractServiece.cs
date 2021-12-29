using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using GeometryExtensions;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPHVAC.FanConnect.Engine;
using ThMEPHVAC.FanConnect.Model;

namespace ThMEPHVAC.FanConnect.Service
{
    public class ThEquipElementExtractServiece
    {
        public static List<ThFanCUModel> GetFCUModels()
        {
            using (var database = AcadDatabase.Active())
            {
                var fcuEngine = new ThFanCURecognitionEngine();
                var retFcu = fcuEngine.Extract(database.Database);
                return retFcu;
            }
        }
        public static List<Line> GetFanPipes(Point3d startPt)
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
                    foreach(var id in psr.Value.GetObjectIds())
                    {
                        var entity = database.Element<Entity>(id);
                        if(entity.Layer.Contains("AI-水管路由") || entity.Layer.Contains("H-PIPE-C"))
                        {
                            layer = entity.Layer;
                            colorIndex = entity.ColorIndex;
                            break;
                        }
                    }
                }
                var retLines = new List<Line>();
                var tmpLines = database.ModelSpace.OfType<Entity>().Where(o => o.Layer.Contains(layer) && o.ColorIndex == colorIndex).ToList();
                foreach(var l in tmpLines)
                {
                    if(l is Line)
                    {
                        retLines.Add(l as Line);
                    }
                    else if(l is Polyline)
                    {
                        var pl = l as Polyline;
                        retLines.AddRange(pl.ToLines());
                    }
                }
                return retLines;
            }
        }
        //获取水管平面
        public static List<Line> GetWaterSpm(string layer)
        {
            using (var database = AcadDatabase.Active())
            {
                var retLine = new List<Line>();
                var tmpLines = database.ModelSpace.OfType<Entity>().Where(o => IsSPMLayer(o, layer)).ToList();
                foreach (var line in tmpLines)
                {
                    if(line is Line)
                    {
                        retLine.Add(line as Line);
                    }
                    else if(line is Polyline)
                    {
                        var pline = line as Polyline;
                        var dbObjs = new DBObjectCollection();
                        pline.Explode(dbObjs);
                        foreach(var obj in dbObjs)
                        {
                            if(obj is Line)
                            {
                                retLine.Add(obj as Line);
                            }
                        }
                    }
                }
                return retLine;
            }
        }
        public static List<Entity> GetPipeDims(string layer)
        {
            using (var database = AcadDatabase.Active())
            {
                var retData = new List<Entity>();
                var tmpEntity = database.ModelSpace.OfType<Entity>().Where(o => IsSPMLayer(o, layer)).ToList();
                foreach(var ent in tmpEntity)
                {
                    if(ent is Circle)
                    {
                        retData.Add(ent);
                    }
                    else if( ent is BlockReference)
                    {
                        var blk = ent as BlockReference;
                        if(blk.GetEffectiveName().Contains("AI-分歧管"))
                        {
                            retData.Add(ent);
                        }
                    }
                }
                return retData;
            }
        }
        public static List<Entity> GetPipeMarkes(string layer)
        {
            using (var database = AcadDatabase.Active())
            {
                var retData = new List<Entity>();
                var tmpEntity = database.ModelSpace.OfType<Entity>().Where(o => IsSPMLayer(o, layer)).ToList();
                foreach (var ent in tmpEntity)
                {
                    if (ent is DBText)
                    {
                        retData.Add(ent);
                    }
                    else if (ent is BlockReference)
                    {
                        var blk = ent as BlockReference;
                        if (blk.GetEffectiveName().Contains("AI-水管多排标注"))
                        {
                            retData.Add(ent);
                        }
                    }
                }
                return retData;
            }
        }
        private static bool IsSPMLayer(Entity entity, string layer)
        {
            if(entity.Layer.Contains(layer))
            {
                return true;
            }
            return false;
        }
    }
}
