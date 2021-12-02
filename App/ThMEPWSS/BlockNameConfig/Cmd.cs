using System;
using System.Linq;
using System.Collections.Generic;
using AcHelper;
using NFox.Cad;
using Linq2Acad;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Command;
using ThMEPWSS.ViewModel;
using ThMEPEngineCore.Engine;
using ThCADExtension;

namespace ThMEPWSS.BlockNameConfig
{
    public class Cmd : ThMEPBaseCommand, IDisposable
    {
        readonly BlockConfigSetViewModel _UiConfigs;

        public Cmd(BlockConfigSetViewModel uiConfigs)
        {
            _UiConfigs = uiConfigs;
            CommandName = "THWTKSB";
            ActionName = "生成";
        }
        public void Dispose()
        {
        }
        public override void SubExecute()
        {
            try
            {
                Execute(_UiConfigs);
            }
            catch (Exception ex)
            {
                Active.Editor.WriteMessage(ex.Message);
            }
        }
        public override void AfterExecute()
        {
            Active.Editor.WriteMessage($"seconds: {_stopwatch.Elapsed.TotalSeconds} \n");
        }

        public void Execute2()
        {
            try
            {
                Execute2(_UiConfigs);
            }
            catch (Exception ex)
            {
                Active.Editor.WriteMessage(ex.Message);
            }
        }

        public void Execute(BlockConfigSetViewModel uiConfigs)
        {
            using (var docLock = Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                PromptNestedEntityOptions nestedEntOpt = new PromptNestedEntityOptions("\nPick nested entity in block:");
                Document dwg = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
                Editor ed = dwg.Editor;
                PromptNestedEntityResult nestedEntRes = ed.GetNestedEntity(nestedEntOpt);

                var entId = nestedEntRes.ObjectId;
                var dbObj = acadDatabase.Element<Entity>(entId);

                string blockName = "";
                if (dbObj is BlockReference br)
                {
                    blockName = ThMEPXRefService.OriginalFromXref(br.GetEffectiveName());
                }
                else
                {
                    if(nestedEntRes.GetContainers().Length>0)
                    {
                        var containerId = nestedEntRes.GetContainers().First();
                        var dbObj2 = acadDatabase.Element<Entity>(containerId);
                        if (dbObj2 is BlockReference br2)
                        {
                            blockName = ThMEPXRefService.OriginalFromXref(br2.GetEffectiveName());
                        }
                    }
                }
                if (blockName.Equals(""))
                {
                    return;
                }
                if (uiConfigs.ConfigList.Count != 0)
                {
                    foreach (var config in uiConfigs.ConfigList)
                    {
                        if (config.layerName.Equals(blockName))
                        {
                            return;
                        }
                    }
                }
                uiConfigs.ConfigList.Add(new ViewModel.BlockNameConfigViewModel(blockName));
                //添加块的框线
                var blks = ExtractBlocks(acadDatabase.Database, blockName);
                var bufferService = new ThNTSBufferService();
                var ents = new DBObjectCollection();
                foreach (BlockReference blk in blks)
                {
                    var obb = CreateFrame(blk);
                    var newFrame = bufferService.Buffer(obb, 100.0) as Polyline;
                    newFrame.Color = Color.FromRgb(255,0,0);
                    newFrame.LineWeight = LineWeight.LineWeight050;
                    ents.Add(newFrame);
                }
                uiConfigs.Frames.Add(blockName, ents);
            }
        }

        public Polyline CreateFrame(BlockReference br)
        {
            var objs = ThDrawTool.Explode(br);
            var curves = objs.OfType<Entity>()
                .Where(e => e is Curve).ToCollection();
            curves = Tesslate(curves);
            curves = curves.OfType<Curve>().Where(o => o != null && o.GetLength() > 1e-6).ToCollection();
            var transformer = new ThMEPOriginTransformer(curves);
            transformer.Transform(curves);
            var obb = curves.GetMinimumRectangle();
            transformer.Reset(obb);
            return obb;
        }

        private DBObjectCollection Tesslate(DBObjectCollection curves, 
            double arcLength = 50.0, double chordHeight = 50.0)
        {
            var results = new DBObjectCollection();
            curves.OfType<Curve>().ToList().ForEach(o =>
            {
                if (o is Line)
                {
                    results.Add(o);
                }
                else if (o is Arc arc)
                {
                    results.Add(arc.TessellateArcWithArc(arcLength));
                }
                else if (o is Circle circle)
                {
                    results.Add(circle.TessellateCircleWithArc(arcLength));
                }
                else if (o is Polyline polyline)
                {
                    results.Add(polyline.TessellatePolylineWithArc(arcLength));
                }
                else if (o is Ellipse ellipse)
                {
                    results.Add(ellipse.Tessellate(chordHeight));
                }
                else if (o is Spline spline)
                {
                    results.Add(spline.Tessellate(chordHeight));
                }
            });
            return results;
        }

        public void Execute2(BlockConfigSetViewModel uiConfigs)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var entOpt = new PromptEntityOptions("\nPick entity in block:");
                var entityResult = Active.Editor.GetEntity(entOpt);

                var entId = entityResult.ObjectId;
                var dbObj = acadDatabase.Element<Entity>(entId);
                if(dbObj is not BlockReference)
                {
                    return;
                }
                var blockName = (dbObj as BlockReference).GetEffectiveName();
                if (blockName.Contains("*"))
                {
                    return;
                }
                if (uiConfigs.ConfigList.Count != 0)
                {
                    foreach (var config in uiConfigs.ConfigList)
                    {
                        if (config.layerName.Equals(blockName))
                        {
                            return;
                        }
                    }
                }
                uiConfigs.ConfigList.Add(new ViewModel.BlockNameConfigViewModel(blockName));
            }
        }

        private DBObjectCollection ExtractBlocks(Database db,string blockName)
        {
            Func<Entity, bool> IsBlkNameQualified = (e) =>
              {
                  if (e is BlockReference br)
                  {
                      return br.GetEffectiveName().ToUpper().EndsWith(blockName.ToUpper());
                  }
                  return false;
              };
            var blkVisitor = new ThBlockReferenceExtractionVisitor();
            blkVisitor.CheckQualifiedLayer = (e) => true;
            blkVisitor.CheckQualifiedBlockName = IsBlkNameQualified;

            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(blkVisitor);
            extractor.ExtractFromMS(db);
            extractor.Extract(db);
            return blkVisitor.Results.Select(o => o.Geometry).ToCollection();
        }
    }
    public class ThBlockReferenceExtractionVisitor : ThDistributionElementExtractionVisitor
    {
        public Func<Entity, bool> CheckQualifiedLayer { get; set; }
        public Func<Entity, bool> CheckQualifiedBlockName { get; set; }
        public ThBlockReferenceExtractionVisitor()
        {
            CheckQualifiedLayer = base.CheckLayerValid;
            CheckQualifiedBlockName = (Entity entity) => true;
        }
        public override void DoExtract(List<ThRawIfcDistributionElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is BlockReference br)
            {
                elements.AddRange(Handle(br, matrix));
            }
        }

        public override void DoXClip(List<ThRawIfcDistributionElementData> elements,
            BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid)
            {
                xclip.TransformBy(matrix);
                elements.RemoveAll(o => !IsContain(xclip, o.Geometry));
            }
        }

        private List<ThRawIfcDistributionElementData> Handle(BlockReference br, Matrix3d matrix)
        {
            var results = new List<ThRawIfcDistributionElementData>();
            if (IsDistributionElement(br) && CheckLayerValid(br))
            {
               var clone = br.Clone() as BlockReference;
                if (clone!=null)
                {
                    clone.TransformBy(matrix);
                    results.Add(new ThRawIfcDistributionElementData()
                    {
                        Geometry = clone,
                    });
                }
            }
            return results;
        }
        private bool IsContain(ThMEPXClipInfo xclip, Entity ent)
        {
            if (ent is BlockReference br)
            {
                return xclip.Contains(br.GeometricExtents.ToRectangle());
            }
            else
            {
                return false;
            }
        }
        public override bool IsDistributionElement(Entity entity)
        {
            return CheckQualifiedBlockName(entity);
        }
        public override bool CheckLayerValid(Entity curve)
        {
            return CheckQualifiedLayer(curve);
        }
        
        public override bool IsBuildElementBlock(BlockTableRecord blockTableRecord)
        {
            // 忽略图纸空间和匿名块
            if (blockTableRecord.IsLayout)
            {
                return false;
            }

            // 忽略不可“炸开”的块
            if (!blockTableRecord.Explodable)
            {
                return false;
            }
            return true;
        }
    }
}