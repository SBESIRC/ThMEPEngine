using System;
using System.Linq;
using System.Collections.Generic;
using AcHelper;
using NFox.Cad;
using Linq2Acad;
using DotNetARX;
using AcHelper.Commands;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using ThCADCore.NTS;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.Temp;
using ThMEPEngineCore;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.Command
{
    public class ThSanitarywareExtractCmd : IAcadCommand, IDisposable
    {
        private string BoundaryLayerName { get; set; }
        private string MarkLayerName { get; set; }
        public ThSanitarywareExtractCmd()
        {
            MarkLayerName = "文字图层";
            BoundaryLayerName = "图纸识别";
        }
        public void Dispose()
        {
        }
        public void Execute()
        {
            using (var acadDb = AcadDatabase.Active())
            using (var pc = new PointCollector(PointCollector.Shape.Window, new List<string>()))
            {
                try
                {
                    pc.Collect();
                }
                catch
                {
                    return;
                }
                Point3dCollection winCorners = pc.CollectedPoints;
                var frame = new Polyline();
                frame.CreateRectangle(winCorners[0].ToPoint2d(), winCorners[1].ToPoint2d());
                frame.TransformBy(Active.Editor.UCS2WCS());

                string blkName = GetBlockName();
                if(string.IsNullOrEmpty(blkName))
                {
                    return;
                }
                string blkMark = GetBlockMark();
                if(string.IsNullOrEmpty(blkMark))
                {
                    return;
                }
                var boundaries = ExtractElements(acadDb.Database, frame.Vertices(), blkName);

                // Print
                CreateLayer(acadDb.Database, BoundaryLayerName, 5);
                CreateLayer(acadDb.Database, MarkLayerName, 6);
                Print(acadDb.Database,boundaries, blkMark);
            }
        }
        private void CreateLayer(Database database, string layerName,short colorIndex)
        {
            using (var currentDb = AcadDatabase.Active())
            {
                if (!currentDb.Layers.Contains(BoundaryLayerName))
                {
                    ThMEPEngineCoreLayerUtils.CreateAILayer(
                        currentDb.Database, layerName, colorIndex);
                }
                else
                {
                    // 设置图层状态
                    database.OpenAILayer(layerName);
                }                  
            }
        }
        private void Print(Database database, List<ThIfcDistributionFlowElement> elements,string mark)
        {
            using (var acadDb = AcadDatabase.Use(database))
            {
                elements.ForEach(o =>
                {
                    o.Outline.Layer = BoundaryLayerName;
                    o.Outline.ColorIndex = (int)ColorIndex.BYLAYER;
                    o.Outline.LineWeight = LineWeight.ByLayer;
                    o.Outline.Linetype = "ByLayer";
                    acadDb.ModelSpace.Add(o.Outline);

                    var center = (o.Outline as Polyline).GetCentroidPoint();
                    var line = new Line(Point3d.Origin, new Point3d(1, 0, 0));
                    line.TransformBy(o.Matrix);
                    var dbText = new DBText();
                    dbText.Height = 80;
                    dbText.WidthFactor = 0.7;
                    dbText.Rotation = line.Angle%Math.PI;
                    dbText.TextString = mark;
                    dbText.Layer = MarkLayerName;
                    dbText.TextStyleId = DbHelper.GetTextStyleId("TH-Style1");
                    dbText.ColorIndex = (int)ColorIndex.BYLAYER;
                    dbText.HorizontalMode = TextHorizontalMode.TextMid;
                    dbText.VerticalMode = TextVerticalMode.TextVerticalMid;
                    dbText.Position = center;
                    dbText.AlignmentPoint = center;                    
                    acadDb.ModelSpace.Add(dbText);
                    line.Dispose();
                });
            }
        }
        private List<ThIfcDistributionFlowElement> ExtractElements(Database database,Point3dCollection pts, string blckName)
        {
            var layers = GetVisibleLayers(database).ToHashSet();
            Func<Entity, bool> CheckLayerValid = e =>
              {
                  return layers.Contains(e.Layer);
              };
            Func<Entity, bool> CheckBlockNameValid = e =>
                 {
                     return e is BlockReference br ? ThMEPXRefService.OriginalFromXref(
                         br.GetEffectiveName()) == blckName : false;
                 };
            var engine = new ThSanitarywareRecognitionEngine()
            { 
                CheckQualifiedLayer = CheckLayerValid,
                CheckQualifiedBlockName = CheckBlockNameValid
            };
            engine.Recognize(database, pts);
            engine.RecognizeMS(database, pts);
            return engine.Elements;
        }
        private List<string> GetVisibleLayers(Database database)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var layers = new List<string>();
                acadDatabase.Layers
                    .Where(o => IsVisibleLayer(o))
                    .ForEachDbObject(o => layers.Add(o.Name));
                return layers;
            }
        }
        private bool IsVisibleLayer(LayerTableRecord layerTableRecord)
        {
            return !(layerTableRecord.IsOff || layerTableRecord.IsFrozen);
        }
        private string GetBlockName()
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var nestedEntOpt = new PromptNestedEntityOptions("\nPick nested entity in block:");
                PromptNestedEntityResult nestedEntRes = Active.Editor.GetNestedEntity(nestedEntOpt);
                if(nestedEntRes.Status==PromptStatus.OK)
                {
                    var dbObj = acadDatabase.Element<Entity>(nestedEntRes.ObjectId);
                    string blockName = "";
                    if (dbObj is BlockReference br)
                    {
                        blockName = ThMEPXRefService.OriginalFromXref(br.GetEffectiveName());
                    }
                    else
                    {
                        if (nestedEntRes.GetContainers().Length > 0)
                        {
                            var containerId = nestedEntRes.GetContainers().First();
                            var dbObj2 = acadDatabase.Element<Entity>(containerId);
                            if (dbObj2 is BlockReference br2)
                            {
                                blockName = ThMEPXRefService.OriginalFromXref(br2.GetEffectiveName());
                            }
                        }
                    }
                    return blockName;
                }
                else
                {
                    return "";
                }
            }
        }
        private string GetBlockMark()
        {
            var pr = Active.Editor.GetString("\n请输入块的标注名称");
            if (pr.Status == PromptStatus.OK)
            {
                return pr.StringResult;
            }
            else
            {
                return "";
            }
        }
    }
}
