using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
using Tianhua.Platform3D.UI.PropertyServices.PropertyModels;
using Tianhua.Platform3D.UI.PropertyServices.PropertyVMoldels;

namespace Tianhua.Platform3D.UI.PropertyServices
{
    public interface ITHProperty
    {
        string XDataAppName { get; }
        string ShowTypeName { get; }
        bool CheckVaild(ObjectId objectId);
        bool GetProperty(ObjectId objectId, out PropertyBase property);
        bool GetVMProperty(ObjectId objectId, out PropertyVMBase property);
        bool SetProperty(ObjectId objectId, PropertyBase property);
        PropertyVMBase MergePropertyVM(List<PropertyVMBase> properties);
    }
}
