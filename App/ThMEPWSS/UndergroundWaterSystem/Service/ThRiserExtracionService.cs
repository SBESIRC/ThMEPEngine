using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model.Hvac;
using ThMEPWSS.UndergroundWaterSystem.Model;

namespace ThMEPWSS.UndergroundWaterSystem.Service
{
    public class ThRiserExtracionService
    {
        public List<ThRiserModel> GetRiserModelList(Point3dCollection pts,int index)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var pipeResult = new List<ThIfcVirticalPipe>();
                var TCHPipeRecognize = new ThTCHVPipeRecognitionEngine();
                TCHPipeRecognize.RecognizeMS(acadDatabase.Database, pts);
                pipeResult.AddRange(TCHPipeRecognize.Elements.OfType<ThIfcVirticalPipe>().ToList());

                var cPipeRecognize = new ThCircleVPipeRecognitionEngine()
                {
                    Radius = new List<double> { 100 / 2, 150 / 2 }
                };
                cPipeRecognize.RecognizeMS(acadDatabase.Database, pts);
                pipeResult.AddRange(cPipeRecognize.Elements.OfType<ThIfcVirticalPipe>().ToList());

                var blkPipeRecognize = new ThBlockVPipeRecognitionEngine();
                blkPipeRecognize.RecognizeMS(acadDatabase.Database, pts);
                pipeResult.AddRange(blkPipeRecognize.Elements.OfType<ThIfcVirticalPipe>().ToList());

                var retModel = new List<ThRiserModel>();
                foreach(var pipe in pipeResult)
                {
                    var model = new ThRiserModel();
                    model.Initialization(pipe.Data);
                    model.FloorIndex = index;
                    retModel.Add(model);
                }
                return retModel;

            }
        }
    }
}
