using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
using System.Linq;
using ThMEPTCH.PropertyServices.EntityProperties;
using ThMEPTCH.PropertyServices.PropertyModels;
using ThMEPTCH.PropertyServices.PropertyVMoldels;
using ThMEPEngineCore.Algorithm;

namespace ThMEPTCH.PropertyServices.EntityUIProperties
{
    [PropertyAttribute("天正墙体", "")]
    class TCHWallPropertyUIService : PropertyUIServiceBase
    {
        public override string ShowTypeName => "天正墙体";
        public TCHWallPropertyUIService()
        {
            serviceBase = new TCHWallPropertyService();
        }
        public override bool CheckVaild(ObjectId objectId)
        {
            return CheckTCHEntityVaild(objectId, (e) => { return e.IsTCHWall(); });
        }
        public override PropertyVMBase MergePropertyVM(List<PropertyVMBase> properties)
        {
            //这里暂时还没有处理完，后面继续处理
            PropertyVMBase propertyVM = null;
            var allTCHWallVMs = properties.OfType<TCHWallPropertyVM>().ToList();
            if (allTCHWallVMs.Count < 1)
                return null;
            propertyVM = allTCHWallVMs.First().Clone() as TCHWallPropertyVM;
            return propertyVM;
        }
        protected override PropertyVMBase PropertyToVM(PropertyBase property)
        {
            var tchWallProp = property as TCHWallProperty;
            var vmProp = new TCHWallPropertyVM(ShowTypeName, tchWallProp);
            return vmProp;
        }
    }
}
