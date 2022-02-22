using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Engine
{
    public class ThBlockPipeRecognitionEngine : ThFlowSegmentRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            throw new NotImplementedException();
        }

        public override void Recognize(List<ThRawIfcFlowSegmentData> datas, Point3dCollection polygon)
        {
            throw new NotImplementedException();
        }

        public override void RecognizeEditor(Point3dCollection polygon)
        {
            throw new NotImplementedException();
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            throw new NotImplementedException();
        }
    }
}
