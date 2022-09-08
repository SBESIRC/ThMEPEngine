using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
using System.Collections.Generic;
using System.Linq;
using ThMEPTCH.PropertyServices.PropertyModels;
using ThMEPTCH.PropertyServices.PropertyVMoldels;

namespace ThMEPTCH.PropertyServices.EntityProperties
{
    [Property("栏杆", "")]
    class RailingPropertiesService : PropertyServiceBase
    {
        public override string ShowTypeName => "栏杆-900";

        public override string XDataAppName => "THProperty";

        public override bool CheckVaild(ObjectId objectId)
        {
            return CheckVaild(objectId, "TH-栏杆");
        }

        public override PropertyVMBase MergePropertyVM(List<PropertyVMBase> properties)
        {
            //这里暂时还没有处理完，后面继续处理
            PropertyVMBase propertyVM = null;
            var allRailingVMs = properties.OfType<RailingPropertyVM>().ToList();
            if (allRailingVMs.Count < 1)
            {
                return null;
            }
            propertyVM = allRailingVMs.First().Clone() as RailingPropertyVM;
            return propertyVM;
        }

        protected override PropertyBase DefaultProperties(ObjectId objectId)
        {
            var property = new RailingProperty(objectId)
            {
                RailingBottomHeight = 0.0,
                RailingHeight = 900.0,
                RailingThickness = 40.0,
            };
            return property;
        }

        protected override PropertyVMBase PropertyToVM(PropertyBase property)
        {
            var railingProp = property as RailingProperty;
            var vmProp = new RailingPropertyVM(ShowTypeName, railingProp);
            return vmProp;
        }

        protected override TypedValueList PropertyToXDataValue(PropertyBase property)
        {
            var railingProp = property as RailingProperty;
            TypedValueList valueList = new TypedValueList
            {
                { (int)DxfCode.ExtendedDataAsciiString, railingProp.RailingBottomHeight.ToString()},
                { (int)DxfCode.ExtendedDataAsciiString, railingProp.RailingHeight.ToString()},
                { (int)DxfCode.ExtendedDataAsciiString, railingProp.RailingThickness.ToString()},
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
                        property.RailingBottomHeight = double.Parse(strData);
                        break;
                    case 2:
                        property.RailingHeight = double.Parse(strData);
                        break;
                    case 3:
                        property.RailingThickness = double.Parse(strData);
                        break;
                }
            }
            return property;
        }
    }
}
