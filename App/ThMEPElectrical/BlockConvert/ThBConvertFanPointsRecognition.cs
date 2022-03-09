using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Linq;
using ThCADExtension;
using ThMEPElectrical.Model;
using ThMEPEngineCore.Service.Hvac;

namespace ThMEPElectrical.BlockConvert
{
    public class ThBConvertFanPointsRecognition
    {
        public ThBConvertFanPoints Recognize(ThBlockReferenceData srcBlockData, Matrix3d transform)
        {
            var position = srcBlockData.Position;
            var srcProperties = srcBlockData.CustomProperties;
            // 入风口坐标，若无则计算obb center
            double inlet_x = 0.0, inlet_y = 0.0;
            // 出风口坐标
            double outlet_x = 0.0, outlet_y = 0.0;
            bool inletExist = false, outletExist = false;
            if (srcProperties.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_INLET_X))
            {
                inletExist = true;
                inlet_x = (double)srcProperties.GetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_INLET_X);
            }
            if (srcProperties.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_INLET_Y))
            {
                inlet_y = (double)srcProperties.GetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_INLET_Y);
            }
            if (srcProperties.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_OUTLET_X))
            {
                outletExist = true;
                outlet_x = (double)srcProperties.GetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_OUTLET_X);
            }
            if (srcProperties.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_OUTLET_Y))
            {
                outlet_y = (double)srcProperties.GetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_OUTLET_Y);
            }

            var fanPoints = new ThBConvertFanPoints();
            if (inletExist)
            {
                // 入风口存在
                fanPoints.Inlet = new Point3d(position.X + inlet_x, position.Y + inlet_y, position.Z).TransformBy(transform);
            }
            else
            {
                fanPoints.Inlet = srcBlockData.GetCentroidPoint().TransformBy(transform);
            }
            if (outletExist)
            {
                fanPoints.OutletExist = outletExist;
                fanPoints.Outlet = new Point3d(position.X + outlet_x, position.Y + outlet_y, position.Z).TransformBy(transform);
            }

            return fanPoints;
        }
    }
}
