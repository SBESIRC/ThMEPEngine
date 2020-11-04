using AcHelper;
using ThCADExtension;
using GeometryExtensions;
using ThMEPElectrical.Geometry;
using ThMEPElectrical.Assistant;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPElectrical.Business
{
    public static class PreWindowSelector
    {
        /// <summary>
        /// 获取选择矩形框线点集
        /// </summary>
        /// <returns></returns>
        public static Point3dCollection GetSelectRectPoints()
        {
            using (PointCollector pc = new PointCollector(PointCollector.Shape.Window))
            {
                try
                {
                    pc.Collect();
                }
                catch
                {
                    return new Point3dCollection();
                }

                Point3dCollection winCorners = pc.CollectedPoints;
                var points = GeomUtils.CalculateRectangleFromPoints(winCorners[0], winCorners[1]);
                return points.PointsTransform(Active.Editor.UCS2WCS());
            }
        }
    }
}
