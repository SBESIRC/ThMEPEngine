using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
using System.Linq;
using ThMEPTCH.PropertyServices.PropertyModels;

namespace ThMEPTCH.PropertyServices.EntityProperties
{
    class HolePropertyService : PropertyServiceBase
    {
        protected override PropertyBase DefaultProperties(ObjectId objectId)
        {
            var property = new HoleProperty(objectId)
            {
                ShowDimension = false,
                Hidden = false,
                BottomElevation = 1000.0,
                Height = 800.0,
                NumberPrefix = "C",
                NumberPostfix = "",
                ElevationDisplay = true,
            };
            return property;
        }
        protected override TypedValueList PropertyToXDataValue(PropertyBase property)
        {
            var holeProp = property as HoleProperty;
            TypedValueList valueList = new TypedValueList
            {
                { (int)DxfCode.ExtendedDataAsciiString, holeProp.ShowDimension.ToString()},
                { (int)DxfCode.ExtendedDataAsciiString, holeProp.Hidden.ToString()},
                { (int)DxfCode.ExtendedDataAsciiString, holeProp.BottomElevation.ToString()},
                { (int)DxfCode.ExtendedDataAsciiString, holeProp.Height.ToString()},
                { (int)DxfCode.ExtendedDataAsciiString, holeProp.NumberPrefix.ToString()},
                { (int)DxfCode.ExtendedDataAsciiString, holeProp.NumberPostfix.ToString()},
                { (int)DxfCode.ExtendedDataAsciiString, holeProp.ElevationDisplay.ToString()},
            };
            return valueList;
        }
        protected override PropertyBase XDataProperties(ObjectId objectId, TypedValueList typedValues)
        {
            var property = new HoleProperty(objectId);
            //读取XData（第0个是AppName，不需要读取）,这边读数据的顺序要和写入数据的顺序一致
            for (int i = 1; i < typedValues.Count; i++)
            {
                var strData = typedValues.ElementAt(i).Value.ToString();
                switch (i)
                {
                    case 1:
                        property.ShowDimension = strData.Equals("1");
                        break;
                    case 2:
                        property.Hidden = strData.Equals("1");
                        break;
                    case 3:
                        property.BottomElevation = double.Parse(strData);
                        break;
                    case 4:
                        property.Height = double.Parse(strData);
                        break;
                    case 5:
                        property.NumberPrefix = strData;
                        break;
                    case 6:
                        property.NumberPostfix = strData;
                        break;
                    case 7:
                        property.ElevationDisplay = strData.Equals("1");
                        break;
                }
            }
            return property;
        }
    }
}
