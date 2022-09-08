using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
using System.Linq;
using ThMEPTCH.PropertyServices.EntityProperties;
using ThMEPTCH.PropertyServices.PropertyModels;
using ThMEPTCH.PropertyServices.PropertyVMoldels;

namespace ThMEPTCH.PropertyServices.EntityUIProperties
{
    [Property("墙体洞口", "")]
    class HolePropertiesUIService : PropertyUIServiceBase
    {
        public HolePropertiesUIService() 
        {
            serviceBase = new HolePropertiesService();
        }
        public override string ShowTypeName => "墙体洞口";
        public override bool CheckVaild(ObjectId objectId)
        {
            return CheckCurveLayerVaild(objectId, "TH-墙洞");
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
        protected override PropertyVMBase PropertyToVM(PropertyBase property)
        {
            var slabHoleProp = property as HoleProperty;
            var vmProp = new HolePropertyVM(ShowTypeName, slabHoleProp);
            return vmProp;
        }
    }
}
