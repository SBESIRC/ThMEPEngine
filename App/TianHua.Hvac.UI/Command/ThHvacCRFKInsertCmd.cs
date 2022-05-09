using System;
using AcHelper;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using ThCADExtension;
using ThMEPEngineCore;
using ThMEPEngineCore.Command;
using ThMEPHVAC;
using ThMEPHVAC.Model;
using ThMEPHVAC.Service;

namespace TianHua.Hvac.UI.Command
{
    public class ThHvacCRFKInsertCmd : ThMEPBaseCommand, IDisposable
    {
        private string AirPortLayer = "";
        private string AirPortBlkName = "AI-风口";
        private ThAirPortParameter AirPortParameter { get; set; }
        private ThDuctPortsDrawPort DrawPortService;
        public ThHvacCRFKInsertCmd(ThAirPortParameter airPortParameter)
        {
            ActionName = "插风口";
            CommandName = "THCRFK";
            AirPortParameter = airPortParameter;
            AirPortLayer = ThMEPHAVCDataManager.AirPortLayer;
            AirPortBlkName = ThMEPHAVCDataManager.AirPortBlkName;
            DrawPortService = new ThDuctPortsDrawPort(AirPortLayer, AirPortBlkName, 0);
        }
        public void Dispose()
        {
            //
        }
        public override void SubExecute()
        {
            using (var docLock=Active.Document.LockDocument())
            {
                ImportLayers();
                ImportBlocks();
                OpenLayer();
                UserInteract();
            }
        }
        private void ImportBlocks()
        {
            using (var acadDb = AcadDatabase.Active())
            using (var blockDb = AcadDatabase.Open(ThCADCommon.HvacPipeDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(AirPortBlkName), true);
            }
        }
        private void ImportLayers()
        {
            using (var acadDb = AcadDatabase.Active())
            using (var blockDb = AcadDatabase.Open(ThCADCommon.HvacPipeDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                acadDb.Layers.Import(blockDb.Layers.ElementOrDefault(AirPortLayer), true);
            }
        }
        private void OpenLayer()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                acadDb.Database.OpenAILayer("0");
                acadDb.Database.OpenAILayer(AirPortLayer);
            }
        }
        private void UserInteract()
        {
            ThMEPHAVCCommon.FocusToCAD();
            while (true)
            {
                var ppo = Active.Editor.GetPoint("\n选择插入点");
                if (ppo.Status == PromptStatus.OK)
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
            var vec = Vector3d.XAxis.TransformBy(Active.Editor.CurrentUserCoordinateSystem).GetNormal();
            var angle = Vector3d.XAxis.GetAngleTo(vec, Vector3d.ZAxis);
            DrawPortService.ucsAngle = angle;
            for (int i=0;i< AirPortParameter.AirPortNum;i++)
            {
                var basePt = position + vec.MultiplyBy(i * AirPortParameter.Length);
                DrawPortService.InsertPort(basePt, angle,
                AirPortParameter.Length, 
                AirPortParameter.Width,
                AirPortParameter.AirPortType, 
                AirPortParameter.SingleAirPortAirVolume);
            }
        }
    }
}
