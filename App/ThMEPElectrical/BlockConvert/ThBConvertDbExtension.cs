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

        /// <summary>
        /// 提取图纸中某个范围内所有的特点块的引用
        /// </summary>
        /// <param name="database"></param>
        /// <param name="block"></param>
        /// <param name="extents"></param>
        /// <returns></returns>
        public static ObjectIdCollection GetBlockReferences(this Database database, 
            ThBlockConvertBlock block, Extents3d extents)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var objs = new ObjectIdCollection();
                var name = (string)block.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_NAME];
                var blkRefs = acadDatabase.ModelSpace
                    .OfType<BlockReference>()
                    .Where(o => o.GetEffectiveName() == name);
                foreach(var blkRef in blkRefs)
                {
                    Plane XYPlane = new Plane(Point3d.Origin, Vector3d.ZAxis);
                    Matrix3d matrix = Matrix3d.Projection(XYPlane, XYPlane.Normal);
                    var projectExtents = new Extents3d(
                        extents.MinPoint.TransformBy(matrix),
                        extents.MaxPoint.TransformBy(matrix));
                    if (blkRef.Position.TransformBy(matrix).IsInside(projectExtents))
                    {
                        objs.Add(blkRef.ObjectId);
                    }
                }
                return objs;
            }
        }
    }
}