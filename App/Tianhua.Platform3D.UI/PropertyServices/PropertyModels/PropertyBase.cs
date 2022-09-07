using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tianhua.Platform3D.UI.PropertyServices.PropertyModels
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
