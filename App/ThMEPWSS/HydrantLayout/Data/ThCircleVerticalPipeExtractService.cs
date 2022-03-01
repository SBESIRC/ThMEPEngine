using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using NFox.Cad;
using Dreambuild.AutoCAD;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThMEPEngineCore.Model.Hvac;

namespace ThMEPWSS.HydrantLayout.Data
{
    public class ThCircleVerticalPipeExtractService
    {
        //---input
        //public ThMEPOriginTransformer Transformer { get => transformer; set => transformer = value; }
        //public List<double> Radius { get; set; }
        //public string LayerName { get; set; }
        //---output
        public List<ThIfcVirticalPipe> VerticalPipe { get; private set; } //key:origin blkreference, value: blk postition dbpoint

        public ThCircleVerticalPipeExtractService()
        {
            VerticalPipe = new List<ThIfcVirticalPipe>();
        }

        public void Extract(Database database, Point3dCollection pts)
        {
            var cResult = ExtractCVPipe(pts);
            VerticalPipe.AddRange(cResult);
        }
        private List<ThIfcVirticalPipe> ExtractCVPipe(Point3dCollection pts)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var cPipeRecognize = new ThCircleVPipeRecognitionEngine()
                {
                    LayerFilter = new List<string> { ThHydrantCommon.Layer_Vertical },
                    Radius = new List<double> { 100 / 2, 150 / 2 },
                };
                cPipeRecognize.RecognizeMS(acadDatabase.Database, pts);
                var cResult = cPipeRecognize.Elements.OfType<ThIfcVirticalPipe>().ToList();

                return cResult;
            }
        }

    }
}

