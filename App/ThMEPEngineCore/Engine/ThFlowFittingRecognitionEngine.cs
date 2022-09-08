using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.Engine
{
    public abstract class ThFlowFittingRecognitionEngine
    {
        public List<ThRawIfcFlowFittingData> Elements { get; set; }
        public ThFlowFittingRecognitionEngine()
        {
            Elements = new List<ThRawIfcFlowFittingData>();
        }

        public abstract void Recognize(Database database, Point3dCollection polygon);

        public abstract void RecognizeMS(Database database, Point3dCollection polygon);

        public abstract void RecognizeEditor(Point3dCollection polygon);

        public abstract void Recognize(List<ThRawIfcFlowFittingData> datas, Point3dCollection polygon);
    }
}
