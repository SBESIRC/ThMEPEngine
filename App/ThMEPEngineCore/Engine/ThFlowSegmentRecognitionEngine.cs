using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public abstract class ThFlowSegmentRecognitionEngine
    {
        public List<ThIfcFlowSegment> Elements { get; set; }
        public ThFlowSegmentRecognitionEngine()
        {
            Elements = new List<ThIfcFlowSegment>();
        }

        public abstract void Recognize(Database database, Point3dCollection polygon);

        public abstract void RecognizeMS(Database database, Point3dCollection polygon);

        public abstract void RecognizeEditor(Point3dCollection polygon);

        public abstract void Recognize(List<ThRawIfcFlowSegmentData> datas, Point3dCollection polygon);
    }
}
