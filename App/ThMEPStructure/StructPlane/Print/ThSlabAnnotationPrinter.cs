using System.Collections.Generic;
using DotNetARX;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPStructure.StructPlane.Service;

namespace ThMEPStructure.StructPlane.Print
{
    internal class ThSlabAnnotationPrinter
    {
        public static ObjectIdCollection Print(AcadDatabase acadDb, DBText text)
        {
            var results = new ObjectIdCollection();
            var positon = text.Position;
            var textString = text.TextString;
            var attNameValues = new Dictionary<string, string>() { };
            attNameValues.Add("BTH", textString);
            var blkId = acadDb.ModelSpace.ObjectId.InsertBlockReference(
                                    ThPrintLayerManager.SlabTextLayerName,
                                    ThPrintBlockManager.BthBlkName,
                                    positon,
                                    new Scale3d(1.0),
                                    0.0,
                                    attNameValues);
            results.Add(blkId);
            return results;
        }
    }
}
