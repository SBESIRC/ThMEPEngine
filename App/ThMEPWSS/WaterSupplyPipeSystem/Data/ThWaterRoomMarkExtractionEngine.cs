using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;

namespace ThMEPWSS.WaterSupplyPipeSystem.Data
{
    class ThWaterRoomMarkExtractionEngine: ThAIRoomMarkExtractionEngine
    {
        public override void Extract(Database database)
        {
            var visitor = new ThWaterRoomMarkExtractVisitor
            {
                LayerFilter = ThRoomMarkLayerManager.TextModelSpaceLayers(database),
            };
            var extractor = new ThAnnotationElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            Results = visitor.Results;
        }
    }
}
