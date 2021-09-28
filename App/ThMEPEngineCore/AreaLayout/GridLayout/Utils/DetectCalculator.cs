using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPEngineCore.AreaLayout.GridLayout.Method
{
    public static class DetectCalculator
    {
        //计算圆形保护区域
        public static Polygon CalculateRoundDetect(Coordinate position,Polygon region,double radius,int numPoints=20)
        {
            if (!FireAlarmUtils.PolygonContainPoint(region, position))
                return Polygon.Empty;
            var circle = new Circle(new Point3d(position.X, position.Y, 0), Vector3d.ZAxis, radius);
            var circle_polygon = circle.ToNTSPolygon(numPoints);
            var detect = circle_polygon.Intersection(region);
            if (detect is Polygon polygon)
                return polygon;
            else if(detect is MultiPolygon multi)
            {
                foreach (Polygon poly in multi)
                    if (FireAlarmUtils.PolygonContainPoint(region, position))
                        return poly;
            }
            return Polygon.Empty;
        }
        //计算可见圆形保护区域
        public static Polygon CalculateVisibleDetect(Coordinate position, Polygon region, double radius, int numPoints = 20)
        {
            if (!FireAlarmUtils.PolygonContainPoint(region, position))
                return Polygon.Empty;
            else return VisiblePolygon.ComputeWithRadius(position, region, radius,numPoints);
        }
        /// <summary>
        /// 计算盲区
        /// </summary>
        /// <param name="position">圆心</param>
        /// <param name="region">带洞探测区域</param>
        /// <param name="radius">保护半径</param>
        /// <param name="isVisible">true是可见盲区，false是圆形盲区</param>
        /// <param name="numPoints">离散圆顶点数</param>
        /// <returns></returns>
        public static Polygon CalculateDetect(Coordinate position,Polygon region,double radius,bool isVisible,int numPoints=20)
        {
            if (isVisible)
                return CalculateVisibleDetect(position, region, radius, numPoints);
            else
                return CalculateRoundDetect(position, region, radius, numPoints);
        }
    }
}
