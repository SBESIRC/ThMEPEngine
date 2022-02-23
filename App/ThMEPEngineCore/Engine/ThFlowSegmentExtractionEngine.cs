using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThRawIfcFlowSegmentData
    {
        public object Data { get; set; }
        public Entity Geometry { get; set; }
    }

    public abstract class ThFlowSegmentExtractionEngine
    {
        public List<ThRawIfcFlowSegmentData> Results { get; protected set; }
        public ThFlowSegmentExtractionEngine()
        {
            Results = new List<ThRawIfcFlowSegmentData>();
        }
        public abstract void Extract(Database database);
        public abstract void ExtractFromMS(Database database);
        public abstract void ExtractFromEditor(Point3dCollection frame);
    }
}