using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThRawIfcFlowFittingData
    {
        public object Data { get; set; }
        public Entity Geometry { get; set; }
    }

    public abstract class ThFlowFittingExtractionEngine
    {
        public List<ThRawIfcFlowFittingData> Results { get; protected set; }
        public ThFlowFittingExtractionEngine()
        {
            Results = new List<ThRawIfcFlowFittingData>();
        }
        public abstract void Extract(Database database);
        public abstract void ExtractFromMS(Database database);
        public abstract void ExtractFromEditor(Point3dCollection frame);

    }
}
