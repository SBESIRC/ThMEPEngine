using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Hvac;

namespace ThMEPWSS.HydrantLayout.Data
{
    public class ThTchVerticalPipeExtractService
    {
        //---input
        //public List<string> LayerName { get; set; } = new List<string>();
        //public ThMEPOriginTransformer Transformer { get; set; }
        //---output
        public List<ThIfcTchVPipe> TCHVerticalPipe { get; protected set; }

        public ThTchVerticalPipeExtractService()
        {
            TCHVerticalPipe = new List<ThIfcTchVPipe>();
        }

        public void Extract(Database database, Point3dCollection pts)
        {
            var TCHResult = ExtractTCHVPipe(pts);
            TCHVerticalPipe.AddRange(TCHResult);
        }

        private List<ThIfcTchVPipe> ExtractTCHVPipe(Point3dCollection pts)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var TCHPipeRecognize = new ThTCHVPipeRecognitionEngine()
                {
                    LayerFilter = new List<string> { ThHydrantCommon.Layer_Vertical },
                };
                TCHPipeRecognize.RecognizeMS(acadDatabase.Database, pts);
                var TCHResult = TCHPipeRecognize.Elements.OfType<ThIfcTchVPipe>().ToList();

                return TCHResult;
            }
        }
    }
}
