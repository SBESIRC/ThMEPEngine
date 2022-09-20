using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
using System.Linq;
using ThMEPTCH.PropertyServices.EntityProperties;
using ThMEPTCH.PropertyServices.PropertyModels;
using ThMEPTCH.PropertyServices.PropertyVMoldels;
using ThMEPEngineCore.Algorithm;

namespace ThMEPTCH.PropertyServices.EntityUIProperties
{
    [Property("天正普通门", "")]
    class TCHDoorPropertyUIService : PropertyUIServiceBase
    {
        public override string ShowTypeName => "天正普通门";
        public TCHDoorPropertyUIService()
        {
            serviceBase = new TCHDoorPropertyService();
        }
        public override bool CheckVaild(ObjectId objectId)
        {
            return CheckTCHEntityVaild(objectId, (e) => { return e.IsTCHDoor(); });
        }
        public override PropertyVMBase MergePropertyVM(List<PropertyVMBase> properties)
        {
            //这里暂时还没有处理完，后面继续处理
            PropertyVMBase propertyVM = null;
            var allTCHWallVMs = properties.OfType<TCHDoorPropertyVM>().ToList();
            if (allTCHWallVMs.Count < 1)
                return null;
            propertyVM = allTCHWallVMs.First().Clone() as TCHDoorPropertyVM;
            return propertyVM;
        }
        protected override PropertyVMBase PropertyToVM(PropertyBase property)
        {
            var tchDoorProp = property as TCHDoorProperty;
            var vmProp = new TCHDoorPropertyVM(ShowTypeName, tchDoorProp);
            return vmProp;
        }
    }
}
