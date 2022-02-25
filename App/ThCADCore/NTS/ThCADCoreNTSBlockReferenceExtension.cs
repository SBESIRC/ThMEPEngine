using NFox.Cad;
using System.Linq;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSBlockReferenceExtension
    {
        /// <summary>
        /// 获取块OBB（仅依赖块中的Curve）
        /// 通常的处理方式：
        ///     1. 将块引用“炸掉”获取其WCS下的图元
        ///     2. 将所有WCS下的图元做一个WCS2ECS变换，转换到ECS
        ///     3. 在ECS下获取所有图元的ABB
        ///     4. 将所有图元的ABB做一个ECS2WCS变换，转换到WCS
        /// 但是，若ECS2WCS变换是Non-Uniform Scale，某些图元调用TransformBy()会跑出异常
        /// 这里借用NTS的MinimumDiameter.GetMinimumRectangle计算OBB，所以暂时只支持所有的Curve图元
        /// </summary>
        /// <param name="br"></param>
        /// <param name="ecs2Wcs"></param>
        /// <returns></returns>
        public static Polyline ToOBB(this BlockReference br)
        {
            var entities = new DBObjectCollection();
            ThBlockReferenceExtensions.Burst(br, entities);
            var filters = entities.OfType<Curve>().Where(e => e.Visible && e.Bounds.HasValue).ToCollection();
            if (filters.Count > 0)
            {
                return filters.GetMinimumRectangle();
            }
            return new Polyline();
        }
    }
}
