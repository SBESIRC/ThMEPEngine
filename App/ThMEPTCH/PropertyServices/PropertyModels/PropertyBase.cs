using Autodesk.AutoCAD.DatabaseServices;
using System;

namespace ThMEPTCH.PropertyServices.PropertyModels
{
    public abstract class PropertyBase : ICloneable
    {
        public ObjectId ObjId { get; }
        public PropertyBase(ObjectId objectId) 
        {
            ObjId = objectId;
        }

        public abstract object Clone();
    }
}
