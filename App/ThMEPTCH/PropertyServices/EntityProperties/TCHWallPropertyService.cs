using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
using System.Linq;
using ThMEPTCH.PropertyServices.PropertyEnums;
using ThMEPTCH.PropertyServices.PropertyModels;

namespace ThMEPTCH.PropertyServices.EntityProperties
{
    class TCHWallPropertyService : PropertyServiceBase
    {
        protected override PropertyBase DefaultProperties(ObjectId objectId)
        {
            var property = new TCHWallProperty(objectId)
            {
                Material = "材质",
                EnumMaterial = EnumTCHWallMaterial.Aeratedconcrete,
                Height = -1,
                BottomElevation = 0.0,
            };
            return property;
        }
        protected override TypedValueList PropertyToXDataValue(PropertyBase property)
        {
            var tchWallProp = property as TCHWallProperty;
            TypedValueList valueList = new TypedValueList
            {
                { (int)DxfCode.ExtendedDataAsciiString, ((int)tchWallProp.EnumMaterial).ToString()},
                { (int)DxfCode.ExtendedDataAsciiString, tchWallProp.Height.ToString()},
                { (int)DxfCode.ExtendedDataAsciiString, tchWallProp.BottomElevation.ToString()},
            };
            return valueList;
        }
        protected override PropertyBase XDataProperties(ObjectId objectId, TypedValueList typedValues)
        {
            var property = new TCHWallProperty(objectId);
            //读取XData（第0个是AppName，不需要读取）,这边读数据的顺序要和写入数据的顺序一致
            for (int i = 1; i < typedValues.Count; i++)
            {
                var strData = typedValues.ElementAt(i).Value.ToString();
                switch (i)
                {
                    case 1:
                        var enumInt = int.Parse(strData);
                        property.Material = strData;
                        property.EnumMaterial = (EnumTCHWallMaterial)enumInt;
                        break;
                    case 2:
                        property.Height = double.Parse(strData);
                        break;
                    case 3:
                        property.BottomElevation = double.Parse(strData);
                        break;
                }
            }
            return property;
        }
    }
}
