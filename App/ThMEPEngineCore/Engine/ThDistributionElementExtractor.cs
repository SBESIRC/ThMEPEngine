using Linq2Acad;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;

namespace ThMEPEngineCore.Engine
{
    public class ThDistributionElementExtractor
    {
        private List<ThDistributionElementExtractionVisitor> Visitors { get; set; }

        public ThDistributionElementExtractor()
        {
            Visitors = new List<ThDistributionElementExtractionVisitor>();
        }

        public void Accept(ThDistributionElementExtractionVisitor visitor)
        {
            Visitors.Add(visitor);
        }

        public void Extract(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                foreach (var ent in acadDatabase.ModelSpace)
                {
                    if (ent is BlockReference blkRef)
                    {
                        if (blkRef.BlockTableRecord.IsNull)
                        {
                            continue;
                        }
                        var mcs2wcs = blkRef.BlockTransform.PreMultiplyBy(Matrix3d.Identity);
                        Visitors.ForEach(o =>
                        {
                            o.Results.AddRange(DoExtract(blkRef, mcs2wcs, o));
                        });
                    }
                }
            }
        }

        public void ExtractFromMS(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                acadDatabase.ModelSpace
                    .OfType<BlockReference>()
                    .ForEach(e =>
                    {
                        Visitors.ForEach(v =>
                        {
                            if (!e.BlockTableRecord.IsNull)
                            {
                                var blockTableRecord = acadDatabase.Blocks.Element(e.BlockTableRecord);
                                if (v.IsBuildElementBlock(blockTableRecord))
                                {
                                    v.Results.AddRange(DoExtract(e, v));
                                }
                            }
                        });
                    });
            }
        }

        private List<ThRawIfcDistributionElementData> DoExtract(
            BlockReference e, ThDistributionElementExtractionVisitor visitor)
        {
            var results = new List<ThRawIfcDistributionElementData>();
            if (visitor.CheckLayerValid(e) && visitor.IsDistributionElement(e))
            {
                visitor.DoExtract(results, e, Matrix3d.Identity);
            }
            return results;
        }

        private List<ThRawIfcDistributionElementData> DoExtract(BlockReference blockReference, Matrix3d matrix, 
            ThDistributionElementExtractionVisitor visitor)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(blockReference.Database))
            {
                var results = new List<ThRawIfcDistributionElementData>();
                if (visitor.IsBuildElementBlockReference(blockReference))
                {                    
                    var blockTableRecord = acadDatabase.Blocks.Element(blockReference.BlockTableRecord);
                    if (visitor.IsBuildElementBlock(blockTableRecord))
                    {
                        var data = new ThBlockReferenceData(blockReference.ObjectId);
                        var objs = data.VisibleEntities();
                        if (objs.Count == 0)
                        {
                            foreach (var objId in blockTableRecord)
                            {
                                var dbObj = acadDatabase.Element<Entity>(objId);
                                if (dbObj.Visible)
                                {
                                    objs.Add(objId);
                                }
                            }
                        }
                        foreach (ObjectId objId in objs)
                        {
                            var dbObj = acadDatabase.Element<Entity>(objId);  
                            if (dbObj is BlockReference blockObj)
                            {
                                if (blockObj.BlockTableRecord.IsNull)
                                {
                                    continue;
                                }
                                if (visitor.IsBuildElementInnerBlockReference(blockObj))
                                {
                                    if (visitor.CheckLayerValid(blockObj) && visitor.IsDistributionElement(blockObj))
                                    {
                                        visitor.DoExtract(results, blockObj, matrix);
                                        continue;
                                    }
                                    var mcs2wcs = blockObj.BlockTransform.PreMultiplyBy(matrix);
                                    results.AddRange(DoExtract(blockObj, mcs2wcs, visitor));
                                }
                            }
                            else if (dbObj.IsTCHElement())
                            {
                                if (visitor.CheckLayerValid(dbObj) && visitor.IsDistributionElement(dbObj))
                                {
                                    visitor.DoExtract(results, dbObj, matrix);
                                }
                            }
                        }

                        // 过滤XClip外的图元信息
                        visitor.DoXClip(results, blockReference, matrix);
                    }
                }
                return results;
            }
        }
    }
}
