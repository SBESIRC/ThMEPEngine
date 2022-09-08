using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
using Linq2Acad;
using System.Collections.Generic;
using System.Linq;
using ThMEPTCH.PropertyServices.PropertyEnums;
using ThMEPTCH.PropertyServices.PropertyModels;
using ThMEPTCH.PropertyServices.PropertyVMoldels;

namespace ThMEPTCH.PropertyServices.EntityProperties
{
    [PropertyAttribute("楼板", "")]
    class SlabPropertyService : PropertyServiceBase
    {
        public override string ShowTypeName => "楼板";
        public override string XDataAppName => "THProperty";
        public override bool CheckVaild(ObjectId objectId)
        {
            bool isVaild = false;
            using (var acadDb = AcadDatabase.Active())
            {
                var entity = acadDb.ModelSpace.Element(objectId);
                if (null == entity || entity.IsErased)
                {
                    return isVaild;
                }
                if (entity is Curve polyline)
                {
                    if (polyline.Layer.Contains("楼板"))
                    {
                        isVaild = true;
                    }
                }
            }
            return isVaild;
        }
        public override PropertyVMBase MergePropertyVM(List<PropertyVMBase> properties)
        {
            //这里暂时还没有处理完，后面继续处理
            PropertyVMBase propertyVM = null;
            var allSlabVMs = properties.OfType<SlabPropertyVM>().ToList();
            if (allSlabVMs.Count < 1)
                return null;
            propertyVM = allSlabVMs.First().Clone() as SlabPropertyVM;
            return propertyVM;
        }
        protected override PropertyBase DefaultProperties(ObjectId objectId)
        {
            var property = new SlabProperty(objectId);
            property.Material = "材质";
            property.EnumMaterial = EnumSlabMaterial.ReinforcedConcrete;
            property.SlabTopElevation = 0.0;
            property.SlabThickness = 100.0;
            property.SlabBuildingSurfaceThickness = 50.0;
            return property;
        }
        protected override PropertyVMBase PropertyToVM(PropertyBase property)
        {
            var slabProp = property as SlabProperty;
            var vmProp = new SlabPropertyVM(ShowTypeName, slabProp);
            return vmProp;
        }
        protected override TypedValueList PropertyToXDataValue(PropertyBase property)
        {
            var slabProp = property as SlabProperty;
            TypedValueList valueList = new TypedValueList
            {
                { (int)DxfCode.ExtendedDataAsciiString, ((int)slabProp.EnumMaterial).ToString()},
                { (int)DxfCode.ExtendedDataAsciiString, slabProp.SlabTopElevation.ToString()},
                { (int)DxfCode.ExtendedDataAsciiString, slabProp.SlabThickness.ToString()},
                { (int)DxfCode.ExtendedDataAsciiString, slabProp.SlabBuildingSurfaceThickness.ToString()},
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
                        property.Material = strData;
                        property.EnumMaterial = (EnumSlabMaterial)enumInt;
                        break;
                    case 2:
                        property.SlabTopElevation = double.Parse(strData);
                        break;
                    case 3:
                        property.SlabThickness = double.Parse(strData);
                        break;
                    case 4:
                        property.SlabBuildingSurfaceThickness = double.Parse(strData);
                        break;
                }
            }
            return property;
        }
    }
}
