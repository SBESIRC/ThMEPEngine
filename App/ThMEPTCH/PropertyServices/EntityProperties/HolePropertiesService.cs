using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
using System.Collections.Generic;
using System.Linq;
using ThMEPTCH.PropertyServices.PropertyModels;
using ThMEPTCH.PropertyServices.PropertyVMoldels;

namespace ThMEPTCH.PropertyServices.EntityProperties
{
    [Property("墙体洞口", "")]
    class HolePropertiesService : PropertyServiceBase
    {
        public override string ShowTypeName => "墙体洞口";

        public override string XDataAppName => "THProperty";

        public override bool CheckVaild(ObjectId objectId)
        {
            return CheckVaild(objectId, "TH-墙洞");
        }

        public override PropertyVMBase MergePropertyVM(List<PropertyVMBase> properties)
        {
            //这里暂时还没有处理完，后面继续处理
            PropertyVMBase propertyVM = null;
            var allHoleVMs = properties.OfType<HolePropertyVM>().ToList();
            if (allHoleVMs.Count < 1)
            {
                return null;
            }
            propertyVM = allHoleVMs.First().Clone() as HolePropertyVM;
            return propertyVM;
        }

        protected override PropertyBase DefaultProperties(ObjectId objectId)
        {
            var property = new HoleProperty(objectId)
            {
                ShowDimension = false,
                Hidden = false,
                BottomHeight = 1000.0,
                HoleHeight = 800.0,
                NumberPrefix = "C",
                NumberPostfix = "",
                ElevationDisplay = true,
            };
            return property;
        }

        protected override PropertyVMBase PropertyToVM(PropertyBase property)
        {
            var slabHoleProp = property as HoleProperty;
            var vmProp = new HolePropertyVM(ShowTypeName, slabHoleProp);
            return vmProp;
        }

        protected override TypedValueList PropertyToXDataValue(PropertyBase property)
        {
            var slabHoleProp = property as HoleProperty;
            TypedValueList valueList = new TypedValueList
            {

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

                }
            }
            return property;
        }
    }
}
