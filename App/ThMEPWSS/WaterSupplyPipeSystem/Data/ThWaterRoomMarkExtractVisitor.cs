using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Engine;

namespace ThMEPWSS.WaterSupplyPipeSystem.Data
{
    public class ThWaterRoomMarkExtractVisitor: ThAIRoomMarkExtractionVisitor
    {
        public override bool IsAnnotationElementBlock(BlockTableRecord blockTableRecord)
        {
            return 
                base.IsAnnotationElementBlock(blockTableRecord) && 
                !blockTableRecord.IsFromExternalReference && 
                !blockTableRecord.IsFromOverlayReference;
        }
    }
}
