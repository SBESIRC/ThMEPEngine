using System;
using System.Collections.Generic;
using AcHelper;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model.Hvac;
using ThMEPEngineCore.Service.Hvac;
using ThMEPHVAC.Model;
using DotNetARX;

namespace ThMEPHVAC.CAD
{
    public class ThValvesAndHolesInsertEngine
    {
        public static ObjectId InsertValve(ThValve ValveModel)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var blockName = ValveModel.ValveBlockName;
                var layerName = ValveModel.ValveBlockLayer;
                Active.Database.ImportLayer(layerName, true);
                Active.Database.ImportValve(blockName, true);
                var objId = Active.Database.InsertValve(blockName, layerName);
                objId.SetValveWidth(ValveModel.Width, ValveModel.WidthPropertyName);
                objId.SetValveModel(ValveModel.ValveVisibility);
                switch (ValveModel.ValveBlockName)
                {
                    case ThHvacCommon.SILENCER_BLOCK_NAME:
                        objId.SetValveTextHeight(250, ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_TEXT_HEIGHT);
                        break;
                    default:
                        objId.SetValveHeight(ValveModel.Length, ValveModel.LengthPropertyName);
                        break;
                }
                if (ValveModel.ValveVisibility == ThHvacCommon.BLOCK_VALVE_VISIBILITY_CHECK)
                {
                    objId.SetValveTextRotate(ValveModel.TextRotateAngle, ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_TEXT_ROTATE_FIRE);
                }

                var blockRef = acadDatabase.Element<BlockReference>(objId, true);
                blockRef.TransformBy(ValveModel.Marix);

                // 返回图块
                return objId;
            }
        }

        public static ObjectId InsertHole(ThValve HoleModel, ThDuctPortsDrawService service, string ductSize, string elevation)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var blockName = HoleModel.ValveBlockName;
                var layerName = HoleModel.ValveBlockLayer;
                var mmElevation = Double.Parse(elevation) * 1000;
                var h = ThMEPHVACService.GetHeight(ductSize);
                // 洞口高 = 风管高 + 100
                var holeSelfEleVec = (mmElevation + h + 100) * Vector3d.ZAxis;
                // 洞底标高 = 风管底标高-0.05
                var holeEle = ((mmElevation - 50) / 1000).ToString("0.00");
                var attr = new Dictionary<string, string> { { "洞口尺寸", "留洞：" + HoleModel.Width.ToString() + "x" + HoleModel.Length.ToString() + "(H)"},
                                                            { "标高", "洞底标高：h+" + holeEle }};
                // 设置框的角度
                var obj = acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                    layerName, blockName, HoleModel.ValvePosition + holeSelfEleVec, new Scale3d(1, 1, 1), -service.ucsAngle, attr);
                // 设置框内字的角度
                ThMEPHVACService.SetAttr(obj, attr, -service.ucsAngle);
                obj.SetValveWidth(HoleModel.Width, HoleModel.WidthPropertyName);
                obj.SetValveHeight(200, HoleModel.LengthPropertyName);// 洞口固定宽为200
                obj.SetValveModel(HoleModel.ValveVisibility);
                // 洞口块本身问题：
                // 洞口标注的旋转角度会影响洞口块本身的旋转角度，所以此处插洞口块时需要减去洞口标注的旋转角度
                obj.SetValveTextRotate(HoleModel.RotationAngle + service.ucsAngle, ThHvacCommon.AI_HOLE_ROTATION);
                obj.SetValveTextHeight(GetTextHeight(service.dimService.scale), ThHvacCommon.AI_HOLE_TEXT_HEIGHT);

                // 返回图块
                return obj;
            }
        }

        private static double GetTextHeight(string scale)
        {
            if (scale == "1:150")
                return 450;
            if (scale == "1:100")
                return 300;
            if (scale == "1:50")
                return 150;
            throw new NotImplementedException("不支持 "+ scale + " 出图比例");
        }

        public static ObjectId InsertHose(ThIfcDuctHose hose,string layerName)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var blockName = ThHvacCommon.HOSE_BLOCK_NAME;
                Active.Database.ImportLayer(layerName);
                Active.Database.ImportValve(blockName);
                var objId = Active.Database.InsertValve(blockName, layerName);
                objId.SetValveWidth(hose.Parameters.Width, ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTHDIA);
                objId.SetValveHeight(hose.Parameters.Length, ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_LENGTH);

                //设置软接位置
                var blockRef = acadDatabase.Element<BlockReference>(objId, true);
                blockRef.TransformBy(hose.Matrix);

                //返回软接图块
                return objId;
            }
        }

        public static void EnableValveAndHoleLayer(ThValve holeModel)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                DbHelper.EnsureLayerOn(holeModel.ValveBlockLayer);
            }
        }

        public static void EnableHoseLayer(string layerName)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                DbHelper.EnsureLayerOn(layerName);
            }
        }
    }
}
