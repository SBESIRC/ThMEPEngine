using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using GeometryExtensions;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPEngineCore.Command;
using ThMEPHVAC.IndoorFanLayout;
using ThMEPHVAC.IndoorFanLayout.Models;
using ThMEPHVAC.IndoorFanModels;
using ThMEPHVAC.ParameterService;

namespace ThMEPHVAC.Command
{
    /// <summary>
    /// 室内机放置模式
    /// </summary>
    class IndoorFanPlace: ThMEPBaseCommand
    {
        public IndoorFanPlace() 
        {
            CommandName = "THSNJFZ";
            ActionName = "室内机放置";
        }
        public override void SubExecute()
        {
            if (IndoorFanParameter.Instance.PlaceModel == null)
                return;
            using (Active.Document.LockDocument())
            using (var acdb = AcadDatabase.Active())
            {
                IndoorFanBlockServices.LoadBlockLayerToDocument(acdb.Database);
            }
            
            var fanData = IndoorFanParameter.Instance.PlaceModel.TargetFanInfo;
            FanLoadBase fanLoad = null;
            switch (IndoorFanParameter.Instance.PlaceModel.FanType) 
            {
                case EnumFanType.FanCoilUnitFourControls:
                case EnumFanType.FanCoilUnitTwoControls:
                    fanLoad = new CoilFanLoad(fanData, IndoorFanParameter.Instance.PlaceModel.FanType, EnumHotColdType.Cold, IndoorFanParameter.Instance.PlaceModel.CorrectionFactor);
                    break;
                case EnumFanType.IntegratedAirConditionin:
                    fanLoad = new AirConditionFanLoad(fanData, IndoorFanParameter.Instance.PlaceModel.FanType, EnumHotColdType.Cold, IndoorFanParameter.Instance.PlaceModel.CorrectionFactor);
                    break;
                case EnumFanType.VRFConditioninConduit:
                case EnumFanType.VRFConditioninFourSides:
                    fanLoad = new VRFImpellerFanLoad(fanData, IndoorFanParameter.Instance.PlaceModel.FanType, EnumHotColdType.Cold, IndoorFanParameter.Instance.PlaceModel.CorrectionFactor);
                    break;
            }
            var connectorDynAttrs = new Dictionary<string, object>();
            var connectorAttrs = IndoorFanBlockServices.GetFanBlockAttrDynAttrs(fanLoad, out connectorDynAttrs);

            string layerName = "";
            string blockName = "";
            blockName = IndoorFanBlockServices.GetBlockLayerNameTextAngle(IndoorFanParameter.Instance.PlaceModel.FanType, out layerName, out double textAngle);

            var xVector = Active.Editor.CurrentUserCoordinateSystem.CoordinateSystem3d.Xaxis;
            var angle = Vector3d.XAxis.GetAngleTo(xVector, Vector3d.ZAxis);
            angle %= (Math.PI * 2);
            while (true)
            {
                //using放到while外部会有using未结束，ucs下显示文字重叠问题
                using (var acadDatabase = AcadDatabase.Active())
                {
                    var opt = new PromptPointOptions("点击进行放置风机");
                    var propmptResult = Active.Editor.GetPoint(opt);
                    if (propmptResult.Status != PromptStatus.OK)
                        break;
                    var createPoint = propmptResult.Value.TransformBy(Active.Editor.UCS2WCS());
                    var addId = acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                        layerName, blockName,
                        createPoint,
                        new Scale3d(1, 1, 1),
                        angle,
                        connectorAttrs);
                    if (null == addId || !addId.IsValid)
                        continue;
                    SetBlockDynAttrs(addId, connectorDynAttrs);
                    ChangeBlockTextAttrAngle(addId, connectorAttrs.Select(c => c.Key).ToList(), angle);
                    ChangeBlockTextAttrAngle(addId, new List<string> { "设备编号" }, angle + textAngle);
                }
            }
        }
        private void SetBlockDynAttrs(ObjectId blockId, Dictionary<string, object> dynAttr)
        {
            if (null == blockId || !blockId.IsValid)
                return;
            foreach (var dyAttr in dynAttr)
            {
                if (dyAttr.Key == null || dyAttr.Value == null)
                    continue;
                blockId.SetDynBlockValue(dyAttr.Key, dyAttr.Value);
            }
        }
        private void ChangeBlockTextAttrAngle(ObjectId blockId, List<string> changeAngleAttrs, double angle)
        {
            var block = blockId.GetDBObject<BlockReference>();
            // 遍历块参照的属性
            foreach (ObjectId attId in block.AttributeCollection)
            {
                AttributeReference attRef = attId.GetDBObject<AttributeReference>();
                if (!changeAngleAttrs.Any(c => c.Equals(attRef.Tag)))
                    continue;
                attRef.Rotation = angle;
            }
        }
    }
}
