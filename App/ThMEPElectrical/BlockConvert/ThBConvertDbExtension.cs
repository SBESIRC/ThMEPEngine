using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADExtension;
using GeometryExtensions;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.BlockConvert
{
    public static class ThBConvertDbExtension
    {
        /// <summary>
        ///获取图纸中的块转换映射表
        /// </summary>
        /// <param name="database"></param>
        /// <returns></returns>
        public static List<ThBConvertRule> Rules(this Database database, ConvertMode mode)
        {
            var engine = new ThBConvertRuleEngine();
            return engine.Acquire(database, mode);
        }

        /// <summary>
        /// 提取图纸中的块引用
        /// </summary>
        /// <param name="database"></param>
        /// <param name="blkRef"></param>
        /// <returns></returns>
        public static ThBlockReferenceData GetBlockReference(this Database database, ObjectId blkRef)
        {
            return new ThBlockReferenceData(blkRef);
        }

        public static ThBlockReferenceData GetBlockReference(this Database database, BlockReference blkRef)
        {
            return new ThBlockReferenceData(database, blkRef);
        }

        /// <summary>
        /// 提取图纸中某个范围内所有的特点块的引用
        /// </summary>
        /// <param name="database"></param>
        /// <param name="block"></param>
        /// <param name="extents"></param>
        /// <returns></returns>
        public static DBObjectCollection GetBlockReferences(this Database database, 
            ThBlockConvertBlock block, Extents3d extents)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var objs = new DBObjectCollection();
                var name = (string)block.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_NAME];
                foreach(BlockReference blkRef in Extract(acadDatabase.Database, name))
                {
                    Plane XYPlane = new Plane(Point3d.Origin, Vector3d.ZAxis);
                    Matrix3d matrix = Matrix3d.Projection(XYPlane, XYPlane.Normal);
                    var projectExtents = new Extents3d(
                        extents.MinPoint.TransformBy(matrix),
                        extents.MaxPoint.TransformBy(matrix));
                    if (blkRef.Position.TransformBy(matrix).IsInside(projectExtents))
                    {
                        objs.Add(blkRef);
                    }
                }
                return objs;
            }
        }

        /// <summary>
        /// 获取转换图块的OBB
        /// </summary>
        /// <param name="br"></param>
        /// <param name="ecs2Wcs"></param>
        /// <returns></returns>
        public static Polyline GetBlockReferenceOBB(this BlockReference br, Matrix3d ecs2Wcs)
        {
            using (var acadDatabase = AcadDatabase.Use(br.Database))
            {
                // 业务需求: 去除在“DEFPOINTS”上的图元
                var blockTableRecord = acadDatabase.Blocks.Element(br.BlockTableRecord);
                var entities = blockTableRecord.GetEntities().Where(e => e.Layer != "DEFPOINTS");
                var rectangle = entities.ToCollection().GeometricExtents().ToRectangle();
                rectangle.TransformBy(ecs2Wcs);
                return rectangle;
            }
        }

        private static List<BlockReference> Extract(Database database, string name)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var results = new List<BlockReference>();
                foreach (var ent in acadDatabase.ModelSpace)
                {
                    if (ent is BlockReference blkRef)
                    {
                        if (blkRef.BlockTableRecord.IsNull)
                        {
                            continue;
                        }
                        var mcs2wcs = blkRef.BlockTransform.PreMultiplyBy(Matrix3d.Identity);
                        results.AddRange(DoExtract(blkRef, mcs2wcs, name));
                    }
                }
                return results;
            }
        }

        private static List<BlockReference> DoExtract(BlockReference blockReference, Matrix3d matrix, string name)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(blockReference.Database))
            {
                var results = new List<BlockReference>();
                var blockTableRecord = acadDatabase.Blocks.Element(blockReference.BlockTableRecord);                
                foreach (var objId in blockTableRecord)
                {
                    var dbObj = acadDatabase.Element<Entity>(objId);
                    if (dbObj is BlockReference blockObj)
                    {
                        if (blockObj.BlockTableRecord.IsNull)
                        {
                            continue;
                        }
                        if (IsBlockReference(blockObj, name))
                        {
                            results.Add(blockObj.GetTransformedCopy(matrix) as BlockReference);
                            continue;
                        }
                        var mcs2wcs = blockObj.BlockTransform.PreMultiplyBy(matrix);
                        results.AddRange(DoExtract(blockObj, mcs2wcs, name));
                    }
                }
                return results;
            }
        }

        private static bool IsBlockReference(BlockReference blockReference, string name)
        {
            return blockReference.GetEffectiveName() == name;
        }
    }
}