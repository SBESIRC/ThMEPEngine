using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
using System.Linq;
using ThMEPTCH.PropertyServices.EntityProperties;
using ThMEPTCH.PropertyServices.PropertyModels;
using ThMEPTCH.PropertyServices.PropertyVMoldels;

namespace ThMEPTCH.PropertyServices.EntityUIProperties
{
    [Property("栏杆", "")]
    class RailingPropertyUIService : PropertyUIServiceBase
    {
        public RailingPropertyUIService() 
        {
            serviceBase = new RailingPropertyService();
        }
        public override string ShowTypeName => "栏杆";
        public override bool CheckVaild(ObjectId objectId)
        {
            return CheckCurveLayerVaild(objectId, "TH-栏杆");
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
        protected override PropertyVMBase PropertyToVM(PropertyBase property)
        {
            var railingProp = property as RailingProperty;
            var vmProp = new RailingPropertyVM(ShowTypeName, railingProp);
            return vmProp;
        }
    }
}
