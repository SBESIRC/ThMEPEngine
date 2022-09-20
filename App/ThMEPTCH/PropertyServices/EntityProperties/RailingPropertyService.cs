using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
using System.Linq;
using ThMEPTCH.PropertyServices.PropertyModels;

namespace ThMEPTCH.PropertyServices.EntityProperties
{
    class RailingPropertyService : PropertyServiceBase
    {
        protected override PropertyBase DefaultProperties(ObjectId objectId)
        {
            var property = new RailingProperty(objectId)
            {
                BottomElevation = 0.0,
                Height = 900.0,
                Thickness = 40.0,
            };
            return property;
        }
        protected override TypedValueList PropertyToXDataValue(PropertyBase property)
        {
            var railingProp = property as RailingProperty;
            TypedValueList valueList = new TypedValueList
            {
                { (int)DxfCode.ExtendedDataAsciiString, railingProp.BottomElevation.ToString()},
                { (int)DxfCode.ExtendedDataAsciiString, railingProp.Height.ToString()},
                { (int)DxfCode.ExtendedDataAsciiString, railingProp.Thickness.ToString()},
            };
            return valueList;
        }
        protected override PropertyBase XDataProperties(ObjectId objectId, TypedValueList typedValues)
        {
            var property = new RailingProperty(objectId);
            //读取XData（第0个是AppName，不需要读取）,这边读数据的顺序要和写入数据的顺序一致
            for (int i = 1; i < typedValues.Count; i++)
            {
                var strData = typedValues.ElementAt(i).Value.ToString();
                switch (i)
                {
                    case 1:
                        property.BottomElevation = double.Parse(strData);
                        break;
                    case 2:
                        property.Height = double.Parse(strData);
                        break;
                    case 3:
                        property.Thickness = double.Parse(strData);
                        break;
                }
            }
            return property;
        }
    }
}
