using System;
using AcHelper;
using DotNetARX;
using Linq2Acad;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using ThMEPHVAC;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Command;
using System.Collections.Generic;

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
            OpenLayer(FGLGLayer);
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
                    InsertBlock(wcsPt, 0.0);
                }
                else
                {
                    break;
                }
            }
        }

        private void InsertBlock(Point3d position,double rotatAngle)
        {
            using (var acadDb = AcadDatabase.Active())
            {
                var spaceId = acadDb.ModelSpace.ObjectId;
                var attrs = new Dictionary<string, string> { { "截面尺寸", "1000x400" },{ "风管编号","EA-01"},{ "风量","3000m3/h"} };
                spaceId.InsertBlockReference(FGLGLayer, FGLGBlkName, position, new Scale3d(1.0), rotatAngle, attrs);
            }
        }

        private void OpenLayer(string layerName)
        {
            using (var acadDb = AcadDatabase.Active())
            {
                acadDb.Database.CreateLayer(layerName);  
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
