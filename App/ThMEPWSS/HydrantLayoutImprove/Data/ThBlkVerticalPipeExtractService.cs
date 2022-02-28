using System;
using System.Linq;
using System.Collections.Generic;

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
using ThMEPEngineCore.Model.Hvac;

namespace ThMEPWSS.HydrantLayoutImprove.Data
{
    public class ThBlkVerticalPipeExtractService 
    {
        //---input
        //public string LayerName { get; set; }

        //---output
        public List<ThIfcTchVPipe> VerticalPipe { get; private set; }

        public ThBlkVerticalPipeExtractService()
        {
            VerticalPipe = new List<ThIfcTchVPipe>();
        }

        public void Extract(Database database, Point3dCollection pts)
        {
            var blkResult = ExtractBlkVPipe(pts);
            VerticalPipe.AddRange(blkResult);
        }
        private List<ThIfcTchVPipe> ExtractBlkVPipe(Point3dCollection pts)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var blkPipeRecognize = new ThBlockVPipeRecognitionEngine()
                {
                    LayerFilter = new List<string> { ThHydrantCommon.Layer_Vertical },
                };
                blkPipeRecognize.RecognizeMS(acadDatabase.Database, pts);
                var blkResult = blkPipeRecognize.Elements.OfType<ThIfcTchVPipe>().ToList();

                return blkResult;
            }
        }

    }
}
