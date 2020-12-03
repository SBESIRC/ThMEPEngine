using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Geometry;
using ThMEPElectrical.Model;

namespace ThMEPElectrical.Business.MainBeam.Optimize
{
    /// <summary>
    /// 矩形模式2点优化
    /// </summary>
    public class NonRegular2PointsOptimizer
    {
        private PlaceRect m_placeRect;

        private PlaceParameter m_parameter;

        public List<Point3d> PlacePoints
        {
            get;
            set;
        } = new List<Point3d>();

        public static List<Point3d> MakeNonRegular2PointsOptimizer(PlaceRect placeRect, PlaceParameter parameter)
        {
            var nonRegular2PointsOptimizer = new NonRegular2PointsOptimizer(placeRect, parameter);
            nonRegular2PointsOptimizer.DoOptimize();
            return nonRegular2PointsOptimizer.PlacePoints;
        }

        public NonRegular2PointsOptimizer(PlaceRect placeRect, PlaceParameter parameter)
        {
            m_placeRect = placeRect;
            m_parameter = parameter;
        }

        public void DoOptimize()
        {
            var postMinRect = ABBRectangle.MakeABBPolyline(m_placeRect.srcPolyline);
            var layoutProfileData = new LayoutProfileData(m_placeRect.srcPolyline, postMinRect);
            PlacePoints = RectProfilePlace.MakeABBRectProfilePlacePoints(layoutProfileData, m_parameter);
        }
    }
}
