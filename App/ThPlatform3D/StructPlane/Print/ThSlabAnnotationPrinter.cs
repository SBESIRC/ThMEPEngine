using System.Collections.Generic;
using DotNetARX;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThPlatform3D.StructPlane.Service;

namespace ThPlatform3D.StructPlane.Print
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
            if(blkId!=ObjectId.Null)
            {
                results.Add(blkId);
            }            
            return results;
        }
        public static bool IsSlabAnnotation(Entity entity)
        {
            if(entity is BlockReference br)
            {
                return br.GetEffectiveName() == ThPrintBlockManager.BthBlkName &&
                    br.Layer == ThPrintLayerManager.SlabTextLayerName;
            }
            else
            {
                return false;
            }
        }

        public static bool IsSlabTableText(Entity entity)
        {
            return entity is DBText && entity.Layer == ThPrintLayerManager.SlabPatternTableTextLayerName;
        }
    }
}
