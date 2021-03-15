using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public abstract class ThSpatialElementExtractionEngine
    {
        public List<ThRawIfcBuildingElementData> Results { get; protected set; }

        public abstract void Extract(Database database);
    }
}
