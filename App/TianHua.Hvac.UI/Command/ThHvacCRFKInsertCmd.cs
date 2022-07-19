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
using System.Collections.Generic;
using ThMEPEngineCore.Service.Hvac;

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
            var dynProperties = CreateDynProperties(AirPortParameter);
            var customAttributes = CreateAttributes(AirPortParameter);
            for (int i=0;i< AirPortParameter.AirPortNum;i++)
            {
                var basePt = position + vec.MultiplyBy(i * AirPortParameter.Length);
                DrawPortService.InsertPort1(basePt, angle,dynProperties,customAttributes);
            }
        }
        private Dictionary<string,object> CreateDynProperties(ThAirPortParameter parameter)
        {
            var result = new Dictionary<string,object>();
            result.Add(ThHvacCommon.BLOCK_DYNAMIC_PORT_RANGE, parameter.AirPortType);
            switch (parameter.AirPortType)
            {
                case "下送风口":
                case "下回风口":
                case "侧送风口":
                case "侧回风口":
                    result.Add(ThHvacCommon.BLOCK_DYNAMIC_PORT_WIDTH_OR_DIAMETER, parameter.Length * 1.0);
                    result.Add(ThHvacCommon.BLOCK_DYNAMIC_PORT_HEIGHT, parameter.Width * 1.0);
                    break;                             
                case "方形散流器":
                    result.Add(ThHvacCommon.SQUARE_DIFFUSER_THROAT_WIDTH, parameter.Length * 1.0);
                    break;
                case "圆形风口":
                    result.Add(ThHvacCommon.CIRCULAR_AIRPORT_DIAMETER, parameter.Length * 1.0);
                    break;
            }
            return result;
        }
        private Dictionary<string,string> CreateAttributes(ThAirPortParameter parameter)
        {
            var attr = new Dictionary<string, string> { { "风量", parameter.SingleAirPortAirVolume.ToString() + "m3/h" } };
            return attr;
        }
    }
}
