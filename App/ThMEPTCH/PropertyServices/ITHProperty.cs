using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
using ThMEPTCH.PropertyServices.PropertyModels;
using ThMEPTCH.PropertyServices.PropertyVMoldels;

namespace ThMEPTCH.PropertyServices
{
    public interface ITHProperty
    {
        string XDataAppName { get; }
        string ShowTypeName { get; }
        bool CheckVaild(ObjectId objectId);
        bool GetProperty(ObjectId objectId, out PropertyBase property, bool checkId);
        bool GetVMProperty(ObjectId objectId, out PropertyVMBase property, bool checkId);
        bool SetProperty(ObjectId objectId, PropertyBase property,bool checkId);
        PropertyVMBase MergePropertyVM(List<PropertyVMBase> properties);
        
    }
}
