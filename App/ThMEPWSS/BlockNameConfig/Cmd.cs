using AcHelper;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Linq2Acad;
using System;
using System.Linq;
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
            }
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
    }
}