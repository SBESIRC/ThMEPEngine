using System;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public abstract class ThFlowSegmentRecognitionEngine : IDisposable
    {
        public List<ThIfcFlowSegment> Elements { get; set; }
        public ThFlowSegmentRecognitionEngine()
        {
            Elements = new List<ThIfcFlowSegment>();
        }

        public void Dispose()
        {
            //
        }

        public abstract void Recognize(Database database, Point3dCollection polygon);

        public abstract void RecognizeMS(Database database, Point3dCollection polygon);

        public abstract void Recognize(List<ThRawIfcFlowSegmentData> datas, Point3dCollection polygon);
    }
}
