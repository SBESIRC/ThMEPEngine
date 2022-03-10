using System;
using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.Model.Hvac;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThMEPFittingExtractor
    {
        public List<ThIfcDistributionFlowElement> Elements { get; set; }

        public string Category { get; set; }

        public ThMEPFittingExtractor()
        {
            Elements = new List<ThIfcDistributionFlowElement>();
        }

        public void Recognize(Database database, Point3dCollection polygon)
        {
            FittingRecognizeFromCurrentDB(polygon, database, Matrix3d.Identity);
            FittingRecognize(polygon, database);
        }

        private void FittingRecognizeFromCurrentDB(Point3dCollection polygon, Database database, Matrix3d matrix)
        {
            var dictionary = new Dictionary<Polyline, EntityModifyParam>();
            ThMEPDuctService.GetFittingsParam(out dictionary, database, matrix);
            dictionary.ForEach(o =>
            {
                var spatialIndex = new ThCADCoreNTSSpatialIndex(new DBObjectCollection { o.Key });
                var filter = spatialIndex.SelectCrossingPolygon(polygon);
                if (filter.Count > 0)
                {
                    if (o.Value.type == Category)
                    {
                        switch (o.Value.type)
                        {
                            case "Elbow":
                                {
                                    var parameter = new ThIfcDuctElbowParameters
                                    {
                                        //暂不考虑数据的相对顺序
                                        Outline = o.Key,
                                        PipeOpenWidth = GetWidth(o.Value.portWidths.Values.ToList().FirstOrDefault()),
                                    };
                                    Elements.Add(new ThIfcDuctElbow(parameter));
                                }
                                break;
                            case "Tee":
                                {
                                    var parameter = new ThIfcDuctTeeParameters
                                    {
                                        //暂不考虑数据的相对顺序
                                        Outline = o.Key,
                                        BranchDiameter = GetWidth(o.Value.portWidths.Values.ToList()[1]),
                                        MainBigDiameter = GetWidth(o.Value.portWidths.Values.ToList()[0]),
                                        MainSmallDiameter = GetWidth(o.Value.portWidths.Values.ToList()[2]),
                                    };
                                    Elements.Add(new ThIfcDuctTee(parameter));
                                }
                                break;
                            case "Cross":
                                {
                                    var parameter = new ThIfcDuctCrossParameters
                                    {
                                        //暂不考虑数据的相对顺序
                                        Outline = o.Key,
                                        BigEndWidth = GetWidth(o.Value.portWidths.Values.ToList()[0]),
                                        MainSmallEndWidth = GetWidth(o.Value.portWidths.Values.ToList()[1]),
                                        SideBigEndWidth = GetWidth(o.Value.portWidths.Values.ToList()[2]),
                                        SideSmallEndWidth = GetWidth(o.Value.portWidths.Values.ToList()[3]),
                                    };
                                    Elements.Add(new ThIfcDuctCross(parameter));
                                }
                                break;
                            case "Reducing":
                                {
                                    var parameter = new ThIfcDuctReducingParameters
                                    {
                                        //暂不考虑数据的相对顺序
                                        Outline = o.Key,
                                        BigEndWidth = GetWidth(o.Value.portWidths.Values.ToList()[0]),
                                        SmallEndWidth = GetWidth(o.Value.portWidths.Values.ToList()[1]),
                                    };
                                    Elements.Add(new ThIfcDuctReducing(parameter));
                                }
                                break;
                        }
                    }
                }
            });
        }

        private void FittingRecognize(Point3dCollection polygon, Database database)
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
                        var blockTableRecord = acadDatabase.Blocks.Element(blkRef.BlockTableRecord);
                        if (IsBuildElementBlock(blockTableRecord))
                        {
                            var mcs2wcs = blkRef.BlockTransform.PreMultiplyBy(Matrix3d.Identity);
                            FittingExtract(database, blkRef, mcs2wcs, polygon);
                        }
                    }
                }
            }
        }

        private void FittingExtract(Database localDb, BlockReference blockReference, Matrix3d matrix, Point3dCollection polygon)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(blockReference.Database))
            {
                if (blockReference.BlockTableRecord.IsValid)
                {
                    var blockTableRecord = acadDatabase.Blocks.Element(blockReference.BlockTableRecord);
                    if (IsBuildElementBlock(blockTableRecord))
                    {
                        var xRefDatabase = ThMEPDuctService.QueryXRefDatabase(localDb, blockReference.BlockTableRecord);
                        if (xRefDatabase != null)
                        {
                            FittingRecognizeFromCurrentDB(polygon, xRefDatabase, matrix);

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
                                    var mcs2wcs = blockObj.BlockTransform.PreMultiplyBy(matrix);
                                    FittingExtract(localDb, blockObj, mcs2wcs, polygon);
                                }
                            }
                        }
                    }
                }
            }
        }

        private bool IsBuildElementBlock(BlockTableRecord blockTableRecord)
        {
            // 暂时不支持动态块，外部参照，覆盖
            if (blockTableRecord.IsDynamicBlock)
            {
                return false;
            }

            // 忽略图纸空间和匿名块
            if (blockTableRecord.IsLayout || blockTableRecord.IsAnonymous)
            {
                return false;
            }

            // 忽略不可“炸开”的块
            if (!blockTableRecord.Explodable)
            {
                return false;
            }

            return true;
        }
        private static double GetWidth(string size)
        {
            if (size == null)
                return 0;
            string[] width = size.Split('x');
            if (width.Length != 2)
                throw new NotImplementedException("Duct size info doesn't contain width or height");
            return Double.Parse(width[0]);
        }
    }
}
