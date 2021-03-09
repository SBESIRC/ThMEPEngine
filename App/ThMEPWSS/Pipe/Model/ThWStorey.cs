using ThCADExtension;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Pipe.Model
{
    public class ThWStoreys : ThIfcSpatialStructureElement
    { 
        public ThBlockReferenceData Data { get; set; }
        public ThWStoreys(ObjectId obj)
        {
            Data = new ThBlockReferenceData(obj);
        }
    }
}
