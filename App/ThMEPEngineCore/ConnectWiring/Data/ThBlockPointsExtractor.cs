using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using DotNetARX;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.IO.ExcelService;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.ConnectWiring.Data
{
    class ThBlockPointsExtractor : ThExtractorBase
    {
        public List<Point3d> blockPts { get; set; }
        public List<BlockReference> resBlocks { get; set; }
        List<string> configBlockd;
        public ThBlockPointsExtractor(List<string> blockNames)
        {
            configBlockd = blockNames;
            Category = BuiltInCategory.WiringPosition.ToString();
        }

        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            blockPts.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Boundary = new DBPoint(o);
                geos.Add(geometry);
            });
            return geos;
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var blocks = acadDatabase.ModelSpace
                    .OfType<BlockReference>().ToList();
                ThCADCoreNTSSpatialIndex spatialIndex = new ThCADCoreNTSSpatialIndex(blocks.ToCollection());
                blocks = spatialIndex.SelectCrossingPolygon(pts).Cast<BlockReference>().ToList();
                
                var pline = new Polyline()
                {
                    Closed = true,
                };
                pline.CreatePolyline(pts);
                resBlocks = new List<BlockReference>();
                blocks = FilterBlock(blocks); 
                blocks.Where(o =>
                {
                    return pline.Contains(o.Position);
                })
                .ForEachDbObject(o => resBlocks.Add(o));
            }
        }

        /// <summary>
        /// 筛选需要的块
        /// </summary>
        /// <param name="blocks"></param>
        /// <returns></returns>
        private List<BlockReference> FilterBlock(List<BlockReference> blocks)
        {
            List<BlockReference> resBlocks = new List<BlockReference>();
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                foreach (var ent in blocks)
                {
                    if (ent is BlockReference blkRef)
                    {
                        if (blkRef.BlockTableRecord.IsNull)
                        {
                            continue;
                        }
                        var mcs2wcs = blkRef.BlockTransform.PreMultiplyBy(Matrix3d.Identity);
                        resBlocks.AddRange(DoExtract(blkRef, mcs2wcs, acad));
                    }
                }
            }

            return resBlocks;
        }

        /// <summary>
        /// 执行筛选
        /// </summary>
        /// <param name="blockReference"></param>
        /// <param name="matrix"></param>
        /// <returns></returns>
        private List<BlockReference> DoExtract(BlockReference blockReference, Matrix3d matrix, AcadDatabase acadDatabase)
        {
            var results = new List<BlockReference>();
            if (!blockReference.BlockTableRecord.IsNull)
            {
                if (configBlockd.Any(x => x == blockReference.Name))
                {
                    results.Add(blockReference);
                    return results;
                }

                var blockTableRecord = acadDatabase.Blocks.Element(blockReference.BlockTableRecord);
                var objs = new ObjectIdCollection();
                if (IsBuildElementBlock(blockTableRecord))
                {
                    foreach (var objId in blockTableRecord)
                    {
                        objs.Add(objId);
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
                            try
                            {
                                var resBlock = dbObj.Clone() as BlockReference;
                                resBlock.TransformBy(matrix);
                                var mcs2wcs = resBlock.BlockTransform.PreMultiplyBy(matrix);
                                results.AddRange(DoExtract(resBlock, mcs2wcs, acadDatabase));
                            }
                            catch
                            {
                                // 可能遇到NonUniform Scaling异常
                                // 暂时忽略掉这样的图块
                                continue;
                            }
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// 判断是否是不需要的块
        /// </summary>
        /// <param name="blockTableRecord"></param>
        /// <returns></returns>
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
