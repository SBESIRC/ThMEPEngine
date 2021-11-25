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
                    foreach (ObjectId id in nestedEntRes.GetContainers())
                    {
                        var dbObj2 = acadDatabase.Element<Entity>(id);
                        if (dbObj2 is BlockReference br2)
                        {
                            blockName = ThMEPXRefService.OriginalFromXref(br2.GetEffectiveName());
                            break;
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
                var blocks = ExtractBlocks(acadDatabase.Database, blockName);
                var ents = new DBObjectCollection();
                var bufferService = new ThNTSBufferService();
                foreach (BlockReference block in blocks)
                {
                    var frame = CreateFrame(block);
                    var newFrame = bufferService.Buffer(frame, 100.0) as Polyline;
                    newFrame.Color = Color.FromRgb(255,0,0);
                    //newFrame.ColorIndex = 1;
                    newFrame.LineWeight = LineWeight.LineWeight050;
                    ents.Add(newFrame);
                }
                uiConfigs.Frames.Add(blockName, ents);
            }
        }

        private Polyline CreateFrame(Point3d center,double length ,double width)
        {
            return ThDrawTool.CreateRectangle(center, length, width);
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
            var ExtractBlock = new ThExtractBlock();
            ExtractBlock.Extract(db, blockName);
            return ExtractBlock.DBobjs;
        }
        private Polyline CreateFrame(BlockReference  br)
        {
            var objs = ThDrawTool.Explode(br);
            var transformer = new ThMEPOriginTransformer(objs);
            transformer.Transform(objs);
            var curves = objs.OfType<Entity>().Where(e => e is Curve).ToCollection();
            var obb = curves.GetMinimumRectangle();
            transformer.Reset(obb);
            objs.Dispose();
            return obb;
        }
    }
    public class ThExtractBlock
    {
        public List<Entity> Results { get; private set; }
        public DBObjectCollection DBobjs { get; private set; }
        public void Extract(Database database, string blockName)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                Results = acadDatabase
                   .ModelSpace
                   .OfType<Entity>()
                   .ToList();

                var dbObjs = Results.ToCollection();

                DBobjs = new DBObjectCollection();
                foreach (var db in dbObjs)
                {
                    if (db is DBPoint)
                    {
                        continue;
                    }
                    if (db is BlockReference)
                    {
                        if (IsBlock((db as BlockReference).GetEffectiveName(), blockName))
                        {
                            DBobjs.Add((DBObject)db);
                        }
                        else
                        {
                            var objs = new DBObjectCollection();

                            var blockRecordId = (db as BlockReference).BlockTableRecord;
                            var btr = acadDatabase.Blocks.Element(blockRecordId);

                            int indx = 0;
                            var indxFlag = false;
                            foreach (var entId in btr)
                            {
                                var dbObj = acadDatabase.Element<Entity>(entId);
                                if (dbObj is BlockReference)
                                {
                                    if (IsBlock((dbObj as BlockReference).GetEffectiveName(), blockName))
                                    {
                                        indxFlag = true;
                                        break;
                                    }
                                }
                                indx += 1;
                            }

                            (db as BlockReference).Explode(objs);
                            if (indxFlag)
                            {
                                if (indx > objs.Count - 1)
                                {
                                    continue;
                                }
                                DBobjs.Add((DBObject)objs[indx]);
                            }

                        }
                    }
                }
            }
        }
        private bool IsBlock(string valve, string blockName)
        {
            return valve.ToUpper().Contains(blockName);
        }
    }
}