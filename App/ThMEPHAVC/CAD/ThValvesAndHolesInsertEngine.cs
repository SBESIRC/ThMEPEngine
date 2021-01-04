using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using ThMEPEngineCore.Service.Hvac;
using ThMEPHVAC.Entity;
using ThMEPEngineCore.Model.Hvac;
using ThMEPHVAC.Duct;

namespace ThMEPHVAC.CAD
{
    public class ThValvesAndHolesInsertEngine
    {
        public static ObjectId InsertValveAndHole(ThValve HoleModel)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var blockName = HoleModel.ValveBlockName;
                var layerName = HoleModel.ValveBlockLayer;
                Active.Database.ImportValve(blockName, layerName);
                var objId = Active.Database.InsertValve(blockName, layerName);
                objId.SetValveWidth(HoleModel.Width, HoleModel.WidthPropertyName);
                objId.SetValveHeight(HoleModel.Length, HoleModel.LengthPropertyName);
                objId.SetValveModel(HoleModel.ValveVisibility);

                // 设置开洞位置
                var blockRef = acadDatabase.Element<BlockReference>(objId, true);
                Point3d holecenter = blockRef.Position.TransformBy(Matrix3d.Displacement(new Vector3d(0.5 * HoleModel.Width, HoleModel.ValveOffsetFromCenter, 0)));
                blockRef.TransformBy(Matrix3d.Displacement(holecenter.GetVectorTo(HoleModel.ValvePosition)) * Matrix3d.Rotation(HoleModel.RotationAngle, Vector3d.ZAxis, holecenter));

                // 返回开洞图块
                return objId;
            }
        }

        public static ObjectId InsertHose(ThIfcDuctHose hose,string modellayer)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var blockName = ThHvacCommon.HOSE_BLOCK_NAME;
                var layerName = ThDuctUtils.HoseLayerName(modellayer);
                Active.Database.ImportValve(blockName, layerName);
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
                var layerName = holeModel.ValveBlockLayer;
                var layerObj = acadDatabase.Layers.ElementOrDefault(layerName, true);
                if (layerObj != null)
                {
                    EnableLayer(layerObj);
                }
            }
        }

        public static void EnableHoseLayer(ThIfcDuctHose hose, string modellayer)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var layerName = ThDuctUtils.HoseLayerName(modellayer);
                var layerObj = acadDatabase.Layers.ElementOrDefault(layerName, true);
                if (layerObj != null)
                {
                    EnableLayer(layerObj);
                }
            }
        }

        private static void EnableLayer(LayerTableRecord ltr)
        {
            ltr.IsOff = false;
            ltr.IsFrozen = false;
            ltr.IsLocked = false;
        }
    }
}
