using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System.Collections.Generic;
using ThMEPStructure.StructPlane.Service;

namespace ThMEPStructure.StructPlane.Print
{
    internal class ThAnnotationPrinter
    {
        private AnnotationPrintConfig Config { get; set; }
        public ThAnnotationPrinter(AnnotationPrintConfig config)
        {
            Config = config;
        }
        public ObjectIdCollection Print(Database db, DBText dbText)
        {
            var results = new ObjectIdCollection();
            var textId = dbText.Print(db, Config);
            results.Add(textId);
            return results;
        }
        public static AnnotationPrintConfig GetAnnotationConfig()
        {
            return new AnnotationPrintConfig
            {
                LayerName = ThPrintLayerManager.BeamTextLayName,
                Height = 250,
                WidthFactor = 0.7,
                TextStyleName = "TH-STYLE3",
            };
        }
    }
    internal class ThSlabAnnotationPrinter
    { 
        public ThSlabAnnotationPrinter()
        {
        }
        public ObjectIdCollection Print(Database db, DBText text)
        {
            using (var acadDb = AcadDatabase.Use(db))
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
}
