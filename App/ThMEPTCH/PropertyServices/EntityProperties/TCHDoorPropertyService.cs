using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
using System.Linq;
using ThMEPTCH.PropertyServices.PropertyModels;

namespace ThMEPTCH.PropertyServices.EntityProperties
{
    class TCHDoorPropertyService : PropertyServiceBase
    {
        protected override PropertyBase DefaultProperties(ObjectId objectId)
        {
            var property = new TCHDoorProperty(objectId)
            {
                Statistics = false,
                BottomHeight = 2200,
                NumberPrefix = "M",
                NumberPostfix = "",
                Entrance = false,
            };
            return property;
        }
        protected override TypedValueList PropertyToXDataValue(PropertyBase property)
        {
            var tchDoorProp = property as TCHDoorProperty;
            TypedValueList valueList = new TypedValueList
            {
                { (int)DxfCode.ExtendedDataAsciiString, tchDoorProp.Statistics.ToString()},
                { (int)DxfCode.ExtendedDataAsciiString, tchDoorProp.BottomHeight.ToString()},
                { (int)DxfCode.ExtendedDataAsciiString, tchDoorProp.NumberPrefix.ToString()},
                { (int)DxfCode.ExtendedDataAsciiString, tchDoorProp.NumberPostfix.ToString()},
                { (int)DxfCode.ExtendedDataAsciiString, tchDoorProp.Entrance.ToString()},
            };
            return valueList;
        }
        protected override PropertyBase XDataProperties(ObjectId objectId, TypedValueList typedValues)
        {
            var property = new TCHDoorProperty(objectId);
            //读取XData（第0个是AppName，不需要读取）,这边读数据的顺序要和写入数据的顺序一致
            for (int i = 1; i < typedValues.Count; i++)
            {
                var strData = typedValues.ElementAt(i).Value.ToString();
                switch (i)
                {
                    case 1:
                        property.Statistics = strData.Equals("1");
                        break;
                    case 2:
                        property.BottomHeight = double.Parse(strData);
                        break;
                    case 3:
                        property.NumberPrefix = strData;
                        break;
                    case 4:
                        property.NumberPostfix = strData;
                        break;
                    case 5:
                        property.Entrance = strData.Equals("1");
                        break;
                }
            }
            return property;
        }
    }
}
