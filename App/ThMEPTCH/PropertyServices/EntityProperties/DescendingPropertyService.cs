using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
using System.Linq;
using ThMEPTCH.PropertyServices.PropertyModels;

namespace ThMEPTCH.PropertyServices.EntityProperties
{
    class DescendingPropertyService : PropertyServiceBase
    {
        protected override PropertyBase DefaultProperties(ObjectId objectId)
        {
            var property = new DescendingProperty(objectId)
            {
                StructureThickness = 100.0,
                SurfaceThickness = 50.0,
                StructureWrapThickness = 100.0,
                WrapSurfaceThickness = 0.0,
            };
            return property;
        }
        protected override TypedValueList PropertyToXDataValue(PropertyBase property)
        {
            var descendingProp = property as DescendingProperty;
            TypedValueList valueList = new TypedValueList
            {
                { (int)DxfCode.ExtendedDataAsciiString, descendingProp.StructureThickness.ToString()},
                { (int)DxfCode.ExtendedDataAsciiString, descendingProp.SurfaceThickness.ToString()},
                { (int)DxfCode.ExtendedDataAsciiString, descendingProp.StructureWrapThickness.ToString()},
                { (int)DxfCode.ExtendedDataAsciiString, descendingProp.WrapSurfaceThickness.ToString()},
            };
            return valueList;
        }
        protected override PropertyBase XDataProperties(ObjectId objectId, TypedValueList typedValues)
        {
            var property = new DescendingProperty(objectId);
            //读取XData（第0个是AppName，不需要读取）,这边读数据的顺序要和写入数据的顺序一致
            for (int i = 1; i < typedValues.Count; i++)
            {
                var strData = typedValues.ElementAt(i).Value.ToString();
                switch (i)
                {
                    case 1:
                        property.StructureThickness = double.Parse(strData);
                        break;
                    case 2:
                        property.SurfaceThickness = double.Parse(strData);
                        break;
                    case 3:
                        property.StructureWrapThickness = double.Parse(strData);
                        break;
                    case 4:
                        property.WrapSurfaceThickness = double.Parse(strData);
                        break;
                }
            }
            return property;
        }
    }
}
