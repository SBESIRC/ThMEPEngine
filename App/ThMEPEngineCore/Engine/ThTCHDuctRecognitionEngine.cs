﻿using System;
using NFox.Cad;
using Linq2Acad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Algorithm;
using System.Collections.Generic;
using ThMEPEngineCore.Model.Hvac;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThTCHDuctExtractionEngine : ThDistributionElementExtractionEngine
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
                var visitor = new ThTCHDuctExtractionVisitor();
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

        private List<ThRawIfcDistributionElementData> DoExtract(List<ThRawIfcDistributionElementData> elements, BlockReference blkRef, Matrix3d matrix)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(blkRef.Database))
            {
                var results = new List<ThRawIfcDistributionElementData>();
                var visitor = new ThTCHDuctExtractionVisitor();
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

    public class ThTCHDuctRecognitionEngine : ThDistributionElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var engine = new ThTCHDuctExtractionEngine();
            engine.Extract(database);
            Recognize(engine.Results, polygon);
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            var engine = new ThTCHDuctExtractionEngine();
            engine.ExtractFromMS(database);
            Recognize(engine.Results, polygon);
        }

        public override void Recognize(List<ThRawIfcDistributionElementData> dataList, Point3dCollection polygon)
        {
            foreach (var data in dataList)
            {
                var spatialIndex = new ThCADCoreNTSSpatialIndex(new DBObjectCollection { data.Geometry });
                if (polygon.Count > 0)
                {
                    var sprinklerFilter = spatialIndex.SelectCrossingPolygon(polygon);
                    if (sprinklerFilter.Count == 0)
                    {
                        continue;
                    }
                }

                var parameter = new ThIfcDuctSegmentParameters();
                parameter.Outline = data.Geometry as Polyline;
                var dictionary = data.Data as Dictionary<string, object>;
                parameter.Width = Convert.ToDouble(dictionary["宽度"]);
                parameter.Height = Convert.ToDouble(dictionary["厚度"]);
                var start_x = Convert.ToDouble(dictionary["始端 X 坐标"]);
                var start_y = Convert.ToDouble(dictionary["始端 Y 坐标"]);
                var start_z = Convert.ToDouble(dictionary["始端 Z 坐标"]);
                var end_x = Convert.ToDouble(dictionary["末端 X 坐标"]);
                var end_y = Convert.ToDouble(dictionary["末端 Y 坐标"]);
                parameter.Length = Math.Sqrt(Math.Pow(start_x - end_x, 2) + Math.Pow(start_y - end_y, 2));
                parameter.MarkHeight = (start_z - parameter.Height / 2.0) / 1000.0;
                Elements.Add(new ThIfcDuctSegment(parameter));
            }
        }
    }
}
