using Linq2Acad;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using AcHelper;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;

namespace ThMEPEngineCore.Engine
{
    public class ThFlowSegmentExtractor
    {
        private List<ThFlowSegmentExtractionVisitor> Visitors { get; set; }

        public ThFlowSegmentExtractor()
        {
            Visitors = new List<ThFlowSegmentExtractionVisitor>();
        }

        public void Accept(ThFlowSegmentExtractionVisitor visitor)
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
                    .OfType<Entity>()
                    .ForEach(e =>
                    {
                        Visitors.ForEach(v =>
                        {
                            v.Results.AddRange(DoExtract(e, v));
                        });
                    });
            }
        }

        private List<ThRawIfcFlowSegmentData> DoExtract(Entity e, ThFlowSegmentExtractionVisitor visitor)
        {
            var results = new List<ThRawIfcFlowSegmentData>();
            if (visitor.CheckLayerValid(e))
            {
                visitor.DoExtract(results, e);
            }
            return results;
        }

        private List<ThRawIfcFlowSegmentData> DoExtract(
            BlockReference blockReference, Matrix3d matrix,
            ThFlowSegmentExtractionVisitor visitor)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(blockReference.Database))
            {
                var results = new List<ThRawIfcFlowSegmentData>();
                if (visitor.IsFlowSegmentBlockReference(blockReference))
                {
                    var blockTableRecord = acadDatabase.Blocks.Element(blockReference.BlockTableRecord);
                    if (visitor.IsFlowSegmentBlock(blockTableRecord))
                    {
                        // 提取图元信息
                        foreach (var objId in blockTableRecord)
                        {
                            var dbObj = acadDatabase.Element<Entity>(objId);
                            if (dbObj is BlockReference blockObj)
                            {
                                if (blockObj.BlockTableRecord.IsNull)
                                {
                                    continue;
                                }
                                if (visitor.IsFlowSegmentBlockReference(blockObj))
                                {
                                    if (visitor.CheckLayerValid(blockObj) && visitor.IsFlowSegment(blockObj))
                                    {
                                        visitor.DoExtract(results, blockObj, matrix);
                                        continue;
                                    }
                                    var mcs2wcs = blockObj.BlockTransform.PreMultiplyBy(matrix);
                                    results.AddRange(DoExtract(blockObj, mcs2wcs, visitor));
                                }
                            }
                            else
                            {
                                visitor.DoExtract(results, dbObj, matrix);
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
