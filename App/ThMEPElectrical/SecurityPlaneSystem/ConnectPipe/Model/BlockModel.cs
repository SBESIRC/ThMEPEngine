using Linq2Acad;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.SecurityPlaneSystem.ConnectPipe.Model
{
    public class BlockModel
    {
        public BlockModel(BlockReference block)
        {
            blockModel = block;
            position = new Point3d(block.Position.X, block.Position.Y, 0);
            Boundary = ToOBB(block);
        }

        /// <summary>
        /// 原始块
        /// </summary>
        public BlockReference blockModel { get; set; }

        /// <summary>
        /// 块基点
        /// </summary>
        public Point3d position { get; set; }

        /// <summary>
        /// OBB
        /// </summary>
        public Polyline Boundary { get; set; }

        /// <summary>
        /// 获取OBB
        /// </summary>
        /// <param name="br"></param>
        /// <returns></returns>
        private Polyline ToOBB(BlockReference br)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var blockTableRecord = acadDatabase.Blocks.Element(br.BlockTableRecord);
                var rectangle = blockTableRecord.GeometricExtents().ToRectangle();
                return rectangle.GetTransformedRectangle(br.BlockTransform).FlattenRectangle();
            }
        }
    }
}
