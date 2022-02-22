using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Engine
{
    public class ThRawPipeExtractionEngine : ThFlowSegmentExtractionEngine
    {
        public override void Extract(Database database)
        {
            throw new NotImplementedException();
        }

        public override void ExtractFromEditor(Point3dCollection frame)
        {
            throw new NotImplementedException();
        }

        public override void ExtractFromMS(Database database)
        {
            throw new NotImplementedException();
        }
    }
}
