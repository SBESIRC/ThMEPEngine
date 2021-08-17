using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Data
{
    public abstract class ThMEPDataSetFactory
    {
        protected ThMEPOriginTransformer Transformer { get; set; }
        public ThMEPDataSetFactory()
        {
            Transformer = new ThMEPOriginTransformer(Point3d.Origin);
        }
        /// <summary>
        /// 创建数据集
        /// </summary>
        /// <returns></returns>
        public ThMEPDataSet Create(Database database, Point3dCollection collection)
        {
            // 获取原材料
            GetElements(database, collection);

            // 加工原材料
            return BuildDataSet();
        }

        /// <summary>
        /// 获取建筑元素
        /// </summary>
        protected abstract void GetElements(Database database, Point3dCollection collection);

        /// <summary>
        /// 创建数据集
        /// </summary>
        protected abstract ThMEPDataSet BuildDataSet();

        protected void UpdateTransformer(Point3dCollection pts)
        {
            var center = pts.Envelope().CenterPoint();
            Transformer = new ThMEPOriginTransformer(center);
        }
    }
}
