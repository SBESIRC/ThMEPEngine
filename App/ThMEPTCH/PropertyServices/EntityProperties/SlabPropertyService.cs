using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
using System.Linq;
using ThMEPTCH.PropertyServices.PropertyEnums;
using ThMEPTCH.PropertyServices.PropertyModels;

namespace ThMEPTCH.PropertyServices.EntityProperties
{
    class SlabPropertyService : PropertyServiceBase
    {
        protected override PropertyBase DefaultProperties(ObjectId objectId)
        {
            var property = new SlabProperty(objectId)
            {
                EnumMaterial = EnumSlabMaterial.ReinforcedConcrete,
                TopElevation = 0.0,
                Thickness = 100.0,
                SurfaceThickness = 50.0,
            };
            return property;
        }
        protected override TypedValueList PropertyToXDataValue(PropertyBase property)
        {
            var slabProp = property as SlabProperty;
            TypedValueList valueList = new TypedValueList
            {
                { (int)DxfCode.ExtendedDataAsciiString, ((int)slabProp.EnumMaterial).ToString()},
                { (int)DxfCode.ExtendedDataAsciiString, slabProp.TopElevation.ToString()},
                { (int)DxfCode.ExtendedDataAsciiString, slabProp.Thickness.ToString()},
                { (int)DxfCode.ExtendedDataAsciiString, slabProp.SurfaceThickness.ToString()},
            };
            return valueList;
        }
        protected override PropertyBase XDataProperties(ObjectId objectId, TypedValueList typedValues)
        {
            var property = new SlabProperty(objectId);
            //读取XData（第0个是AppName，不需要读取）,这边读数据的顺序要和写入数据的顺序一致
            for (int i = 1; i < typedValues.Count; i++)
            {
                var strData = typedValues.ElementAt(i).Value.ToString();
                switch (i)
                {
                    case 1:
                        var enumInt = int.Parse(strData);
                        property.EnumMaterial = (EnumSlabMaterial)enumInt;
                        break;
                    case 2:
                        property.TopElevation = double.Parse(strData);
                        break;
                    case 3:
                        property.Thickness = double.Parse(strData);
                        break;
                    case 4:
                        property.SurfaceThickness = double.Parse(strData);
                        break;
                }
            }
            return property;
        }
    }
}
