using System;
using System.Collections.Generic;
using AcHelper;
using DotNetARX;
using Linq2Acad;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Command;

namespace TianHua.Hvac.UI.Command
{
    public class ThHvacFGLGInsertCmd : ThMEPBaseCommand, IDisposable
    {
        private string FGLGLayer = "";
        private string FGLGBlkName = "AI-风管立管";        
        public ThHvacFGLGInsertCmd()
        {
            ActionName = "插风管立管";
            CommandName = "THFGLG";
        }
        public void Dispose()
        {
            //
        }
        public override void SubExecute()
        {
            FGLGLayer = GetCurrentLayer();
            OpenLayer();
            ImportBlocks();
            UserInteract();
        }

        private void UserInteract()
        {
            while(true)
            {
                var ppo = Active.Editor.GetPoint("\n选择插入点");
                if(ppo.Status==PromptStatus.OK)
                {
                    var wcsPt = ppo.Value.TransformBy(Active.Editor.CurrentUserCoordinateSystem);
                    InsertBlock(wcsPt);
                }
                else
                {
                    break;
                }
            }
        }

        private void InsertBlock(Point3d position)
        {
            using (var acadDb = AcadDatabase.Active())
            {
                var spaceId = acadDb.ModelSpace.ObjectId;
                var attrs = new Dictionary<string, string> { { "截面尺寸", "1000x400" },{ "风管编号","EA-01"},{ "风量","3000m3/h"} };
                var blkId = spaceId.InsertBlockReference(FGLGLayer, FGLGBlkName, Point3d.Origin, new Scale3d(1.0), 0.0, attrs);
                var blk = acadDb.Element<BlockReference>(blkId);
                blk.TransformBy(Active.Editor.CurrentUserCoordinateSystem);
                var mt = Matrix3d.Displacement(blk.Position.GetVectorTo(position));
                blk.TransformBy(mt);
            }
        }

        private void OpenLayer()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                acadDb.Database.OpenAILayer("0");
                acadDb.Database.OpenAILayer(FGLGLayer);  
            }
        }
        private string GetCurrentLayer()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                return acadDb.Database.GetCurrentLayer();
            }
        }

        private void ImportBlocks()
        {
            using (var acadDb = AcadDatabase.Active())
            using (var blockDb = AcadDatabase.Open(ThCADCommon.HvacPipeDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(FGLGBlkName), true);
            }
        }
    }
}
