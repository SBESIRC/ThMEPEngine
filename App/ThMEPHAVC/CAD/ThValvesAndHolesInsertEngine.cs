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
        public static ObjectId InsertValve(ThValve HoleModel)
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

                // 插入图块时，图块被放置在WCS的原点
                // 为了在WCS中正确放置图块，图块需要完成转换：
                //  1. 将图块的中心点移到原点
                //  2. 依据图块的实际放置角度，考虑是否需要旋转一个补偿角度(180)，使文字方向转正
                //  3. 补偿第一步平移变换
                //  4. 基于图块插入点将图块旋转到管线的角度
                //  5. 将图块平移到管线上指定位置
                var blockRef = acadDatabase.Element<BlockReference>(objId, true);
                Point3d holeinsertpoint = blockRef.Position.TransformBy(Matrix3d.Displacement(new Vector3d(0.5 * HoleModel.Width, HoleModel.ValveOffsetFromCenter, 0)));
                Point3d valvecenterpoint = blockRef.Position.TransformBy(Matrix3d.Displacement(new Vector3d(0.5 * HoleModel.Width, -0.5 * HoleModel.Length, 0)));
                Matrix3d rotation = Matrix3d.Identity;
                if (HoleModel.RotationAngle >=0 && HoleModel.RotationAngle <= 0.5 * Math.PI ||
                    HoleModel.RotationAngle > 1.5 * Math.PI && HoleModel.RotationAngle <= 2 * Math.PI)
                {
                    rotation = Matrix3d.Identity;
                }
                else
                {
                    rotation = Matrix3d.Rotation(Math.PI, Vector3d.ZAxis, Point3d.Origin);
                }
                Matrix3d matrix = Matrix3d.Identity
                    .PreMultiplyBy(Matrix3d.Displacement(valvecenterpoint.GetAsVector().Negate()))
                    .PreMultiplyBy(rotation)
                    .PreMultiplyBy(Matrix3d.Displacement(valvecenterpoint.GetAsVector()))
                    .PreMultiplyBy(Matrix3d.Rotation(HoleModel.RotationAngle, Vector3d.ZAxis, holeinsertpoint))
                    .PreMultiplyBy(Matrix3d.Displacement(holeinsertpoint.GetVectorTo(HoleModel.ValvePosition)));
                    
                blockRef.TransformBy(matrix);

                // 返回图块
                return objId;
            }
        }

        public static ObjectId InsertHole(ThValve HoleModel)
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

                // 插入图块时，图块被放置在WCS的原点
                // 为了在WCS中正确放置图块，图块需要完成转换：
                //  1. 基于图块插入点将图块旋转到管线的角度
                //  2. 将图块平移到管线上指定位置
                var blockRef = acadDatabase.Element<BlockReference>(objId, true);
                Point3d holeinsertpoint = blockRef.Position.TransformBy(Matrix3d.Displacement(new Vector3d(0.5 * HoleModel.Width, HoleModel.ValveOffsetFromCenter, 0)));
                Point3d valvecenterpoint = blockRef.Position.TransformBy(Matrix3d.Displacement(new Vector3d(0.5 * HoleModel.Width, -0.5 * HoleModel.Length, 0)));
                Matrix3d matrix = Matrix3d.Identity
                    .PreMultiplyBy(Matrix3d.Rotation(HoleModel.RotationAngle, Vector3d.ZAxis, holeinsertpoint))
                    .PreMultiplyBy(Matrix3d.Displacement(holeinsertpoint.GetVectorTo(HoleModel.ValvePosition)));

                blockRef.TransformBy(matrix);

                // 返回图块
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
