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
using ThMEPHVAC.ParameterService;

namespace ThMEPHVAC.Command
{
    /// <summary>
    /// 室内机放置模式
    /// </summary>
    class IndoorFanPlace: ThMEPBaseCommand
    {
        public string FanBlockName { get; set; }
        public string FanType { get; set; }

        public override void SubExecute()
        {
            if (IndoorFanParameter.Instance.PlaceModel == null)
                return;
            using (Active.Document.LockDocument())
            using (var acdb = AcadDatabase.Active())
            {
                LoadFanBlockServices.LoadBlockLayerToDocument(acdb.Database);
            }
            string layerName = LoadFanBlockServices.AirConditionFanLayerName;
            string blockName = LoadFanBlockServices.AirConditionFanBlackName;
            var fanData = IndoorFanParameter.Instance.PlaceModel.TargetFanInfo;
            var fanLoad = new AirConditionFanLoad(fanData, IndoorFanParameter.Instance.PlaceModel.FanType,IndoorFanModels.EnumHotColdType.Cold,1);
            var connectorDynAttrs = new Dictionary<string, object>();
            var connectorAttrs = GetFanBlockAttrDynAttrs(fanLoad, out connectorDynAttrs);

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
                    var addId = acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                        layerName, blockName,
                        propmptResult.Value.TransformBy(Active.Editor.UCS2WCS()),
                        new Scale3d(1, 1, 1),
                        angle,
                        connectorAttrs);
                    if (null == addId || !addId.IsValid)
                        continue;
                    SetBlockDynAttrs(addId, connectorDynAttrs);
                    ChangeBlockTextAttrAngle(addId, connectorAttrs.Select(c => c.Key).ToList(), angle);
                    ChangeBlockTextAttrAngle(addId, new List<string> { "设备编号" }, angle + Math.PI / 2);
                }
            }
        }
        private Dictionary<string, string> GetFanBlockAttrDynAttrs(FanLoadBase fanLoad, out Dictionary<string, object> blockDynAttrs)
        {
            var blockAttrs = new Dictionary<string, string>();
            blockDynAttrs = new Dictionary<string, object>();
            blockAttrs.Add("设备编号", fanLoad.FanNumber);
            if (fanLoad is CoilFanLoad coilFanLoad)
            {;
                string hotColdStr = coilFanLoad.GetCoolHotString(out string waterTempStr);
                blockAttrs.Add("制冷量/制热量", hotColdStr);
                blockAttrs.Add("冷水温差/热水温差", waterTempStr);
            }
            else if (fanLoad is AirConditionFanLoad airCondition) 
            {
                string hotColdStr = airCondition.GetCoolHotString(out string waterTempStr);
                blockAttrs.Add("制冷量/制热量", hotColdStr);
                blockAttrs.Add("冷水温差/热水温差", waterTempStr);
            }
            else
            {
                blockAttrs.Add("制冷量/制热量", string.Format("{0}kW/{1}kW", fanLoad.FanRealCoolLoad, fanLoad.FanRealHotLoad));
            }
            blockAttrs.Add("设备电量", string.Format("{0}W", fanLoad.FanPower));

            blockDynAttrs.Add("设备宽度", fanLoad.FanWidth);
            blockDynAttrs.Add("设备深度", fanLoad.FanLength);
            return blockAttrs;
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
