using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using Linq2Acad;
using DotNetARX;

using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Data;
using ThMEPEngineCore.Extension;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Hvac;

using ThMEPWSS.Service;
using ThMEPWSS.Sprinkler.Data;
using ThMEPWSS.SprinklerConnect.Data;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.Pipe.Engine;

namespace ThMEPWSS.DrainageADPrivate.Data
{
    internal class ThVerticalPipeExtractService
    {
        //---input
        public List<string> LayerFilter { get; set; } = new List<string>();

        //---output
        public List<ThIfcVirticalPipe> VerticalPipe { get; protected set; }

        public ThVerticalPipeExtractService()
        {
            VerticalPipe = new List<ThIfcVirticalPipe>();
        }

        public void Extract(Database database, Point3dCollection pts)
        {
            var TCHResult = ExtractTCHVPipe(pts);
            VerticalPipe.AddRange(TCHResult);
        }

        private List<ThIfcVirticalPipe> ExtractTCHVPipe(Point3dCollection pts)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var TCHPipeRecognize = new ThTCHVPipeRecognitionEngine()
                {
                    LayerFilter = LayerFilter,
                };
                TCHPipeRecognize.RecognizeMS(acadDatabase.Database, pts);
                var TCHResult = TCHPipeRecognize.Elements.OfType<ThIfcVirticalPipe>().ToList();

                return TCHResult;
            }
        }
    }
}
