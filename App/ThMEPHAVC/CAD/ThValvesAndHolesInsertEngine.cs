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

namespace ThMEPHVAC.CAD
{
    public class ThValvesAndHolesInsertEngine
    {
        public static ObjectId InsertWallHole(ThValve HoleModel,string heightpropertyname, string widthpropertyname)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var blockName = HoleModel.ValveBlockName;
                var layerName = HoleModel.ValveBlockLayer;
                Active.Database.ImportValve(blockName, layerName);
                var objId = Active.Database.InsertValve(blockName, layerName);
                objId.SetValveWidth(HoleModel.Width, widthpropertyname);
                objId.SetValveHeight(HoleModel.Length, heightpropertyname);
                objId.SetValveModel(HoleModel.ValveVisibility);

                // 设置开洞位置
                var blockRef = acadDatabase.Element<BlockReference>(objId, true);
                Point3d holecenter = blockRef.Position.TransformBy(Matrix3d.Displacement(new Vector3d(0.5 * HoleModel.Width, HoleModel.ValveOffsetFromCenter, 0)));
                blockRef.TransformBy(Matrix3d.Displacement(holecenter.GetVectorTo(HoleModel.ValvePosition)) * Matrix3d.Rotation(HoleModel.RotationAngle, Vector3d.ZAxis, holecenter));

                // 返回开洞图块
                return objId;
            }
        }
    }
}
