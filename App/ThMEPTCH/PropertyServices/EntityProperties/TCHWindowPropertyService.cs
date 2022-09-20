using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
using System.Linq;
using ThMEPTCH.PropertyServices.PropertyModels;

namespace ThMEPTCH.PropertyServices.EntityProperties
{
    class TCHWindowPropertyService : PropertyServiceBase
    {
        protected override PropertyBase DefaultProperties(ObjectId objectId)
        {
            var property = new TCHWindowProperty(objectId)
            {
                Statistics = false,
                BottomElevation = 900,
                Height = 1200,
                NumberPrefix = "C",
                NumberPostfix = "",
            };
            return property;
        }
        protected override TypedValueList PropertyToXDataValue(PropertyBase property)
        {
            var tchWindowProp = property as TCHWindowProperty;
            TypedValueList valueList = new TypedValueList
            {
                { (int)DxfCode.ExtendedDataAsciiString, tchWindowProp.Statistics.ToString()},
                { (int)DxfCode.ExtendedDataAsciiString, tchWindowProp.BottomElevation.ToString()},
                { (int)DxfCode.ExtendedDataAsciiString, tchWindowProp.Height.ToString()},
                { (int)DxfCode.ExtendedDataAsciiString, tchWindowProp.NumberPrefix.ToString()},
                { (int)DxfCode.ExtendedDataAsciiString, tchWindowProp.NumberPostfix.ToString()},
            };
            return valueList;
        }
        protected override PropertyBase XDataProperties(ObjectId objectId, TypedValueList typedValues)
        {
            var property = new TCHWindowProperty(objectId);
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
                        property.BottomElevation = double.Parse(strData);
                        break;
                    case 3:
                        property.Height = double.Parse(strData);
                        break;
                    case 4:
                        property.NumberPrefix = strData;
                        break;
                    case 5:
                        property.NumberPostfix = strData;
                        break;
                }
            }
            return property;
        }
    }
}
