using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThBlockPipeExtractionEngine : ThFlowSegmentExtractionEngine
    {
        public override void Extract(Database database)
        {
            throw new System.NotImplementedException();
        }

        public override void ExtractFromEditor(Point3dCollection frame)
        {


            throw new System.NotImplementedException();
        }

        public override void ExtractFromMS(Database database)
        {
            throw new System.NotImplementedException();
        }
    }
}
