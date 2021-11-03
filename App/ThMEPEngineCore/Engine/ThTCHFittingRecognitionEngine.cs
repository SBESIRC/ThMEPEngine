using System;
using Linq2Acad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Model.Hvac;

namespace ThMEPEngineCore.Engine
{
    public class ThTCHFittingExtractionEngine : ThDistributionElementExtractionEngine
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
                var visitor = new ThTCHFittingExtractionVisitor();
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
                var visitor = new ThTCHFittingExtractionVisitor();
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

    public class ThTCHFittingRecognitionEngine : ThDistributionElementRecognitionEngine
    {
        //弯头
        public List<ThIfcDuctElbow> Elbows { get; set; }
        //三通
        public List<ThIfcDuctTee> Tees { get; set; }
        //四通
        public List<ThIfcDuctCross> Crosses { get; set; }
        //变径
        public List<ThIfcDuctReducing> Reducings { get; set; }

        public ThTCHFittingRecognitionEngine()
        {
            Elbows = new List<ThIfcDuctElbow>();
            Tees = new List<ThIfcDuctTee>();
            Crosses = new List<ThIfcDuctCross>();
            Reducings = new List<ThIfcDuctReducing>();
        }

        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var engine = new ThTCHFittingExtractionEngine();
            engine.Extract(database);
            Recognize(engine.Results, polygon);
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            var engine = new ThTCHFittingExtractionEngine();
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

                var dictionary = data.Data as Dictionary<string, object>;
                var category = Convert.ToString(dictionary["类型"]);
                switch (category)
                {
                    case "2":
                        RecoginzeElbow(data);
                        break;
                    case "5":
                        RecoginzeTee(data);
                        break;
                    case "7":
                        RecoginzeCross(data);
                        break;
                }
            }
        }

        private void RecoginzeElbow(ThRawIfcDistributionElementData data)
        {
            var dictionary = data.Data as Dictionary<string, object>;
            var bigEndWidth = Convert.ToDouble(dictionary["始端宽度"]);
            var smallEndWidth = Convert.ToDouble(dictionary["末端宽度"]);
            if (bigEndWidth < smallEndWidth)
            {
                var temp = bigEndWidth;
                bigEndWidth = smallEndWidth;
                smallEndWidth = temp;
            }
            if (bigEndWidth - smallEndWidth < 1.0)
            {
                var elbow = new ThIfcDuctElbowParameters
                {
                    Outline = data.Geometry as Polyline,
                    PipeOpenWidth = bigEndWidth
                };
                Elbows.Add(new ThIfcDuctElbow(elbow));
            }
            else
            {
                var reducing = new ThIfcDuctReducingParameters
                {
                    Outline = data.Geometry as Polyline,
                    BigEndWidth = bigEndWidth,
                    SmallEndWidth = smallEndWidth
                };
                Reducings.Add(new ThIfcDuctReducing(reducing));
            }
        }

        private void RecoginzeTee(ThRawIfcDistributionElementData data)
        {
            var dictionary = data.Data as Dictionary<string, object>;
            var mainBigDiameter = Convert.ToDouble(dictionary["始端宽度"]);
            var mainSmallDiameter = Convert.ToDouble(dictionary["末端宽度"]);
            var branchDiameter = Convert.ToDouble(dictionary["支管宽度"]);
            if (mainBigDiameter < mainSmallDiameter)
            {
                var temp = mainBigDiameter;
                mainBigDiameter = mainSmallDiameter;
                mainSmallDiameter = temp;
            }
            var tee = new ThIfcDuctTeeParameters
            {
                Outline = data.Geometry as Polyline,
                MainBigDiameter = mainBigDiameter,
                MainSmallDiameter = mainSmallDiameter,
                BranchDiameter = branchDiameter
            };
            Tees.Add(new ThIfcDuctTee(tee));
        }

        private void RecoginzeCross(ThRawIfcDistributionElementData data)
        {
            var dictionary = data.Data as Dictionary<string, object>;
            var bigEndWidth = Convert.ToDouble(dictionary["始端宽度"]);
            var mainSmallEndWidth = Convert.ToDouble(dictionary["末端宽度"]);
            var sideBigEndWidth = Convert.ToDouble(dictionary["支管1宽度"]);
            var sideSmallEndWidth = Convert.ToDouble(dictionary["支管2宽度"]);
            var cross = new ThIfcDuctCrossParameters
            {
                Outline = data.Geometry as Polyline,
                BigEndWidth = bigEndWidth,
                MainSmallEndWidth = mainSmallEndWidth,
                SideBigEndWidth = sideBigEndWidth,
                SideSmallEndWidth = sideSmallEndWidth
            };
            Crosses.Add(new ThIfcDuctCross(cross));
        }
    }
}
