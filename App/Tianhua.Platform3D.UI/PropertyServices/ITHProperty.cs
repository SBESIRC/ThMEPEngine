using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;

namespace Tianhua.Platform3D.UI.PropertyServices
{
    public interface ITHProperty
    {
        bool IsVaild { get; set; }
        string ShowTypeName { get; }
        Dictionary<string, object> Properties { get; set; }
        void InitObjectId(ObjectId objectId);
        void CheckAndGetData();
        Dictionary<string, object> DefaultProperties();
    }
}
