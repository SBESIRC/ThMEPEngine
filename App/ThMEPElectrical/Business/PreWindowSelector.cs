using AcHelper;
using ThCADExtension;
using GeometryExtensions;
using ThMEPElectrical.Geometry;
using ThMEPElectrical.Assistant;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;

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
            var prompts = new List<string>()
            {
                "请输入结构信息识别范围的第一个角点",
                "请输入结构信息识别范围的第二个角点"
            };
            using (PointCollector pc = new PointCollector(PointCollector.Shape.Window, prompts))
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
