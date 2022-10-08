using System;
using System.Linq;
using System.Collections.Generic;

using DotNetARX;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;

namespace ThMEPWSS.Engine
{
    public class ThTCHSprinklerExtractionEngine : ThDistributionElementExtractionEngine
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
                var visitor = new ThTCHSprinklerExtractionVisitor();
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
                var visitor = new ThTCHSprinklerExtractionVisitor();
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

    public class ThTCHSprinklerRecognitionEngine : ThDistributionElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            throw new NotImplementedException();
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            var engine = new ThTCHSprinklerExtractionEngine();
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
                var transformer = new ThMEPOriginTransformer(polygon[0]);
                frame = new Polyline()
                {
                    Closed = true,
                };
                frame.CreatePolyline(polygon);
                transformer.Transform(frame);
            }
            foreach (var data in dataList)
            {
                var block = data.Geometry as BlockReference;
                if (block == null
                    || !block.Bounds.HasValue
                    || !(block.Name.Contains("$TwtSys$00000131")
                        || block.Name.Contains("$ATTACHMENT$00000094")
                        || block.Name.Contains("$TwtSys$00000125")))
                {
                    continue;
                }

                if (frame != null)
                {
                    var transformer = new ThMEPOriginTransformer(polygon[0]);
                    if (!frame.Intersects(new DBPoint(transformer.Transform(block.Position))))
                    {
                        continue;
                    }
                }

                // 过滤重复喷头
                if (Elements.OfType<ThSprinkler>().Any(o => o.Position.DistanceTo(block.Position) < 1.0))
                {
                    continue;
                }

                var sprinkler = new ThSprinkler()
                {
                    Outline = block.GeometricExtents.ToRectangle(),
                    Position = block.Position,
                };
                var dictionary = data.Data as Dictionary<string, object>;
                if (block.Name.Contains("$TwtSys$00000131") || block.Name.Contains("$ATTACHMENT$00000094"))
                {
                    sprinkler.Category = "侧喷";
                    if (dictionary["横向镜像"] as string == "是")
                    {
                        sprinkler.Direction = Vector3d.YAxis
                            .TransformBy(Matrix3d.Rotation(Convert.ToDouble(dictionary["旋转角度"]) * Math.PI / 180,
                                     Vector3d.ZAxis, Point3d.Origin));
                    }
                    else
                    {
                        sprinkler.Direction = Vector3d.YAxis
                            .TransformBy(Matrix3d.Rotation((Convert.ToDouble(dictionary["旋转角度"]) + 180) * Math.PI / 180,
                                     Vector3d.ZAxis, Point3d.Origin));
                    }
                }
                else if (block.Name.Contains("$TwtSys$00000125"))
                {
                    if (dictionary["遮挡管线"] as string == "是")
                    {
                        sprinkler.Category = "上喷";
                    }
                    else if (dictionary["遮挡管线"] as string == "否")
                    {
                        sprinkler.Category = "下喷";
                    }
                }
                else
                {
                    throw new NotSupportedException();
                }
                Elements.Add(sprinkler);
            }
        }
    }
}
