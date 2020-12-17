﻿using System;
using Linq2Acad;
using DotNetARX;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service.Hvac
{
    public static class ThHvacDbModelDynBlockExtension
    {
        public static void SetModelName(this ObjectId obj, string name)
        {
            var dynamicProperties = obj.GetDynProperties();
            if (dynamicProperties.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_SPECIFICATION_MODEL))
            {
                dynamicProperties.SetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_SPECIFICATION_MODEL, name);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static void SetModelTextHeight(this ObjectId obj)
        {
            var dynamicProperties = obj.GetDynProperties();
            if (dynamicProperties.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_MODEL_TEXT_HEIGHT))
            {
                dynamicProperties.SetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_MODEL_TEXT_HEIGHT, 375.0);
            }
            if (dynamicProperties.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_ANNOTATION_TEXT_HEIGHT))
            {
                dynamicProperties.SetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_ANNOTATION_TEXT_HEIGHT, 375.0);
            }
        }

        public static string GetModelName(this ObjectId obj)
        {
            var dynamicProperties = obj.GetDynProperties();
            if (dynamicProperties.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_SPECIFICATION_MODEL))
            {
                return dynamicProperties.GetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_SPECIFICATION_MODEL) as string;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static Point3d GetModelBasePoint(this ObjectId obj)
        {
            double position_x = 0, position_y = 0;
            var dynamicProperties = obj.GetDynProperties();
            if (dynamicProperties.Contains(ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_BASE_POINT_X))
            {
                position_x = (double)dynamicProperties.GetValue(ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_BASE_POINT_X);
            }
            if (dynamicProperties.Contains(ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_BASE_POINT_Y))
            {
                position_y = (double)dynamicProperties.GetValue(ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_BASE_POINT_Y);
            }
            return new Point3d(position_x, position_y, 0);
        }

        public static Point3d GetModelAnnoationBasePoint(this ObjectId obj)
        {
            double position_x = 0, position_y = 0;
            var dynamicProperties = obj.GetDynProperties();
            if (dynamicProperties.Contains(ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_ANNOTATION_BASE_POINT_X))
            {
                position_x = (double)dynamicProperties.GetValue(ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_ANNOTATION_BASE_POINT_X);
            }
            if (dynamicProperties.Contains(ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_ANNOTATION_BASE_POINT_Y))
            {
                position_y = (double)dynamicProperties.GetValue(ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_ANNOTATION_BASE_POINT_Y);
            }
            return new Point3d(position_x, position_y, 0);
        }

        public static void SetModelCustomPropertiesFrom(this ObjectId obj, DynamicBlockReferencePropertyCollection properties)
        {
            var dynamicProperties = obj.GetDynProperties();
            foreach (var property in new string[] {
                ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_ANGLE1,
                ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_ANGLE2,
                ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_ROTATE1,
                ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_ROTATE2,
                ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_DIAMETER,
                ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_POSITION1_X,
                ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_POSITION1_Y,
                ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_BASE_POINT_X,
                ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_BASE_POINT_Y,
                ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_MODEL_TEXT_HEIGHT,
                ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_ANNOTATION_TEXT_HEIGHT,
                ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_ANNOTATION_BASE_POINT_X,
                ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_ANNOTATION_BASE_POINT_Y,
                ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_INLET_X,
                ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_INLET_Y,
                ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_OUTLET_X,
                ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_OUTLET_Y,
                ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_INLET_VERTICAL,
                ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_INLET_HORIZONTAL,
                ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_OUTLET_VERTICAL,
                ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_OUTLET_HORIZONTAL,
            })
            {
                if (dynamicProperties.Contains(property) && properties.Contains(property))
                {
                    dynamicProperties.SetValue(property, properties.GetValue(property));
                }
            }
        }
    }
}