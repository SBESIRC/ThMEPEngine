using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
using System.Collections.Generic;
using System.Linq;
using ThMEPTCH.PropertyServices.PropertyModels;
using ThMEPTCH.PropertyServices.PropertyVMoldels;

namespace ThMEPTCH.PropertyServices.EntityProperties
{
    [Property("降板", "")]
    class DescendingPropertiesService : PropertyServiceBase
    {
        public override string ShowTypeName => "降板";

        public override string XDataAppName => "THProperty";

        public override bool CheckVaild(ObjectId objectId)
        {
            return CheckVaild(objectId, "TH-降板");
        }

        public override PropertyVMBase MergePropertyVM(List<PropertyVMBase> properties)
        {
            //这里暂时还没有处理完，后面继续处理
            PropertyVMBase propertyVM = null;
            var allDescendingVMs = properties.OfType<DescendingPropertyVM>().ToList();
            if (allDescendingVMs.Count < 1)
            {
                return null;
            }
            propertyVM = allDescendingVMs.First().Clone() as DescendingPropertyVM;
            return propertyVM;
        }

        protected override PropertyBase DefaultProperties(ObjectId objectId)
        {
            var property = new DescendingProperty(objectId)
            {
                DescendingThickness = 100.0,
                DescendingWrapThickness = 100.0,
                DescendingSurfaceThickness = 50.0,
            };
            return property;
        }

        protected override PropertyVMBase PropertyToVM(PropertyBase property)
        {
            var descendingProp = property as DescendingProperty;
            var vmProp = new DescendingPropertyVM(ShowTypeName, descendingProp);
            return vmProp;
        }

        protected override TypedValueList PropertyToXDataValue(PropertyBase property)
        {
            var descendingProp = property as DescendingProperty;
            TypedValueList valueList = new TypedValueList
            {
                { (int)DxfCode.ExtendedDataAsciiString, descendingProp.DescendingThickness.ToString()},
                { (int)DxfCode.ExtendedDataAsciiString, descendingProp.DescendingWrapThickness.ToString()},
                { (int)DxfCode.ExtendedDataAsciiString, descendingProp.DescendingSurfaceThickness.ToString()},
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
                        property.DescendingThickness = double.Parse(strData);
                        break;
                    case 2:
                        property.DescendingWrapThickness = double.Parse(strData);
                        break;
                    case 3:
                        property.DescendingSurfaceThickness = double.Parse(strData);
                        break;
                }
            }
            return property;
        }
    }
}
