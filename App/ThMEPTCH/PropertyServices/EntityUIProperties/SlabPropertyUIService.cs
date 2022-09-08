using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
using System.Linq;
using ThMEPTCH.PropertyServices.EntityProperties;
using ThMEPTCH.PropertyServices.PropertyModels;
using ThMEPTCH.PropertyServices.PropertyVMoldels;

namespace ThMEPTCH.PropertyServices.EntityUIProperties
{
    [PropertyAttribute("楼板", "")]
    class SlabPropertyUIService : PropertyUIServiceBase
    {
        public override string ShowTypeName => "楼板";
        public SlabPropertyUIService()
        {
            serviceBase = new SlabPropertyService();
        }
        public override bool CheckVaild(ObjectId objectId)
        {
            return CheckCurveLayerVaild(objectId,"楼板");
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
        protected override PropertyVMBase PropertyToVM(PropertyBase property)
        {
            var slabProp = property as SlabProperty;
            var vmProp = new SlabPropertyVM(ShowTypeName, slabProp);
            return vmProp;
        }
    }
}
