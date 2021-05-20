using System;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public abstract class ThDistributionElementRecognitionEngine : IDisposable
    {
        public List<ThIfcDistributionFlowElement> Elements { get; set; }
        protected ThDistributionElementRecognitionEngine()
        {
            Elements = new List<ThIfcDistributionFlowElement>();
        }

        public void Dispose()
        {
        }

        public abstract void Recognize(Database database, Point3dCollection polygon);
        public abstract void RecognizeMS(Database database, Point3dCollection polygon);
    }
}
