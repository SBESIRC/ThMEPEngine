using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
using System.Linq;
using ThMEPTCH.PropertyServices.EntityProperties;
using ThMEPTCH.PropertyServices.PropertyModels;
using ThMEPTCH.PropertyServices.PropertyVMoldels;
using ThMEPEngineCore.Algorithm;

namespace ThMEPTCH.PropertyServices.EntityUIProperties
{
    [Property("天正普通窗", "")]
    class TCHWindowPropertyUIService : PropertyUIServiceBase
    {
        public override string ShowTypeName => "天正普通窗";
        public TCHWindowPropertyUIService()
        {
            serviceBase = new TCHWindowPropertyService();
        }
        public override bool CheckVaild(ObjectId objectId)
        {
            return CheckTCHEntityVaild(objectId, (e) => { return e.IsTCHWindow(); });
        }
        public override PropertyVMBase MergePropertyVM(List<PropertyVMBase> properties)
        {
            //这里暂时还没有处理完，后面继续处理
            PropertyVMBase propertyVM = null;
            var allTCHWallVMs = properties.OfType<TCHWindowPropertyVM>().ToList();
            if (allTCHWallVMs.Count < 1)
                return null;
            propertyVM = allTCHWallVMs.First().Clone() as TCHWindowPropertyVM;
            return propertyVM;
        }
        protected override PropertyVMBase PropertyToVM(PropertyBase property)
        {
            var tchWindowProp = property as TCHWindowProperty;
            var vmProp = new TCHWindowPropertyVM(ShowTypeName, tchWindowProp);
            return vmProp;
        }
    }
}
