using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
using System.Linq;
using ThMEPTCH.PropertyServices.EntityProperties;
using ThMEPTCH.PropertyServices.PropertyModels;
using ThMEPTCH.PropertyServices.PropertyVMoldels;

namespace ThMEPTCH.PropertyServices.EntityUIProperties
{
    [Property("降板", "")]
    class DescendingPropertiesUIService : PropertyUIServiceBase
    {
        public DescendingPropertiesUIService() 
        {
            serviceBase = new DescendingPropertiesService();
        }
        public override string ShowTypeName => "降板";
        public override bool CheckVaild(ObjectId objectId)
        {
            return CheckCurveLayerVaild(objectId, "TH-降板");
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
        protected override PropertyVMBase PropertyToVM(PropertyBase property)
        {
            var descendingProp = property as DescendingProperty;
            var vmProp = new DescendingPropertyVM(ShowTypeName, descendingProp);
            return vmProp;
        }
    }
}
