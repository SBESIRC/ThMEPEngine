using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.SystemDiagram.Engine
{
    public abstract class ThEntityCommonRecognitionEngine : IDisposable
    {
        public List<ThEntityData> Elements { get; set; }
        public ThEntityCommonRecognitionEngine()
        {
            Elements = new List<ThEntityData>();
        }
        public void Dispose()
        {
        }

        public abstract void Recognize(Database database, Point3dCollection polygon);

        public abstract void RecognizeMS(Database database, Point3dCollection polygon);

        public abstract void Recognize(List<ThEntityData> datas, Point3dCollection polygon);
    }
}
