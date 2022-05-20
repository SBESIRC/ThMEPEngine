using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;

namespace ThMEPWSS.Engine
{
    public class ThTCHWaterIndicatorExtractionEngine : ThDistributionElementExtractionEngine
    {
        public override void Extract(Database database)
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
                        var elements = new List<ThRawIfcDistributionElementData>();
                        Results.AddRange(DoExtract(elements, blkRef, mcs2wcs));
                    }
                }
            }
        }

        public override void ExtractFromMS(Database database)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var visitor = new ThTCHWaterIndicatorExtractionVisitor();
                var elements = new List<ThRawIfcDistributionElementData>();
                acadDatabase.ModelSpace
                    .OfType<Entity>()
                    .ForEach(e =>
                    {
                        if (visitor.CheckLayerValid(e) && visitor.IsDistributionElement(e))
                        {
                            visitor.DoExtract(elements, e, Matrix3d.Identity);
                        }
                    });
                Results.AddRange(elements);
            }
        }

        public override void ExtractFromEditor(Point3dCollection frame)
        {
            throw new NotSupportedException();
        }

        private List<ThRawIfcDistributionElementData> DoExtract(List<ThRawIfcDistributionElementData> elements, BlockReference blkRef, Matrix3d matrix)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(blkRef.Database))
            {
                var results = new List<ThRawIfcDistributionElementData>();
                var visitor = new ThTCHWaterIndicatorExtractionVisitor();
                if (visitor.IsBuildElementBlockReference(blkRef))
                {
                    var blockTableRecord = acadDatabase.Blocks.Element(blkRef.BlockTableRecord);
                    if (visitor.IsBuildElementBlock(blockTableRecord))
                    {
                        var data = new ThBlockReferenceData(blkRef.ObjectId);
                        var objs = data.VisibleEntities();
                        if (objs.Count == 0)
                        {
                            foreach (var objId in blockTableRecord)
                            {
                                objs.Add(objId);
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
                                if (visitor.IsBuildElementBlockReference(blockObj))
                                {
                                    var mcs2wcs = blockObj.BlockTransform.PreMultiplyBy(matrix);
                                    results.AddRange(DoExtract(elements, blockObj, mcs2wcs));
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
                        visitor.DoXClip(results, blkRef, matrix);
                    }
                }
                return results;
            }
        }
    }

    public class ThTCHWaterIndicatorRecognitionEngine : ThDistributionElementRecognitionEngine
    {
        public List<Polyline> Results { get; set; }

        public ThTCHWaterIndicatorRecognitionEngine()
        {
            Results = new List<Polyline>();
        }

        public override void Recognize(Database database, Point3dCollection polygon)
        {
            throw new NotImplementedException();
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            var engine = new ThTCHWaterIndicatorExtractionEngine();
            engine.ExtractFromMS(database);
            Recognize(engine.Results, polygon);
        }

        public override void RecognizeEditor(Point3dCollection polygon)
        {
            throw new NotSupportedException();
        }

        public override void Recognize(List<ThRawIfcDistributionElementData> dataList, Point3dCollection polygon)
        {
            Polyline frame = null;
            if (polygon.Count != 0)
            {
                frame = new Polyline()
                {
                    Closed = true,
                };
                frame.CreatePolyline(polygon);
            }
            var results = dataList.Select(o => o.Geometry).ToCollection();
            if (!frame.IsNull())
            {
                var index = new ThCADCoreNTSSpatialIndex(results);
                results = index.SelectCrossingPolygon(frame);
            }
            Results = results.OfType<Polyline>().ToList();
        }
    }
}
