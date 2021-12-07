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
    public class ThMEPDuctExtractor
    {
        public List<ThIfcDistributionFlowElement> Elements { get; set; }

        public ThMEPDuctExtractor()
        {
            Elements = new List<ThIfcDistributionFlowElement>();
        }

        public void Recognize(Database database, Point3dCollection polygon)
        {
            DuctRecognizeFromCurrentDB(polygon, database, Matrix3d.Identity);
            DuctRecognize(polygon, database);

        }

        private void DuctRecognizeFromCurrentDB(Point3dCollection polygon, Database database, Matrix3d matrix)
        {
            var dictionary = new Dictionary<Line, DuctModifyParam>();
            ThMEPDuctService.GetDuctsParam(out dictionary, database, matrix);

            var spatialIndex = new ThCADCoreNTSSpatialIndex(dictionary.Keys.ToCollection());
            var filter = spatialIndex.SelectCrossingPolygon(polygon);

            dictionary.Where(o => filter.Contains(o.Key)).ForEach(o =>
            {
                var strs = o.Value.ductSize.Split('x');
                var parameter = new ThIfcDuctSegmentParameters
                {
                    Width = Convert.ToDouble(strs[0]),
                    Height = Convert.ToDouble(strs[1]),
                    Length = o.Value.sp.DistanceTo(o.Value.ep),
                    MarkHeight = o.Value.elevation
                };
                parameter.Outline = o.Key.Buffer(parameter.Width / 2);
                Elements.Add(new ThIfcDuctSegment(parameter)
                {
                    Outline = o.Key,
                });
            });
        }

        private void DuctRecognize(Point3dCollection polygon, Database database)
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
                            DuctExtract(database, blkRef, mcs2wcs, polygon);
                        }
                    }
                }
            }
        }

        private void DuctExtract(Database localDb, BlockReference blockReference, Matrix3d matrix, Point3dCollection polygon)
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
                            DuctRecognizeFromCurrentDB(polygon, xRefDatabase, matrix);

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
                                    DuctExtract(localDb, blockObj, mcs2wcs, polygon);
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
    }
}
