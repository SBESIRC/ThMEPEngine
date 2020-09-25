using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.Model
{
    /// <summary>
    /// 布置点的保护区域
    /// </summary>
    public class PlacePointProArea
    {

        private double m_horizontalLength; // 水平长度
        
        private double m_verticalLength; // 垂直高度

        private Point3d m_center; // 中心点

        private RectInfo m_leftTop; // 左上保护矩形

        private RectInfo m_leftBottom; // 左下保护矩形

        private RectInfo m_rightTop; // 右上保护矩形

        private RectInfo m_rightBottom; // 右下保护矩形

        // 左上保护矩形
        public RectInfo LeftTop
        {
            get { return m_leftTop; }
        }

        // 左下保护矩形
        public RectInfo LeftBottom
        {
            get { return m_leftBottom; }
        }

        // 右上保护矩形
        public RectInfo RightTop
        {
            get { return m_rightTop; }
        }

        // 右下保护矩形
        public RectInfo RightBottom
        {
            get { return m_rightBottom; }
        }

        private PlacePointProArea(double horizontalLength, double verticalLength, Point3d center)
        {
            m_horizontalLength = horizontalLength;
            m_verticalLength = verticalLength;
            m_center = center;
        }

        public PlacePointProArea MakeProtectAreaInfo(double horizontalLength, double verticalLength, Point3d center)
        {
            var protectArea = new PlacePointProArea(horizontalLength, verticalLength, center);
            protectArea.CalRectInfo();
            return protectArea;
        }

        private void CalRectInfo()
        {
            //相关顶点计算
            var topLeftPt = m_center + new Vector3d(-m_horizontalLength, m_verticalLength, 0);
            var topMidPt = m_center + new Vector3d(0, m_verticalLength, 0);
            var topRightPt = m_center + new Vector3d(m_horizontalLength, m_verticalLength, 0);

            var centerLeftPt = m_center + new Vector3d(-m_horizontalLength, 0, 0);
            var centerRightPt = m_center + new Vector3d(m_horizontalLength, 0, 0);

            var bottomLeftPt = m_center + new Vector3d(-m_horizontalLength, -m_verticalLength, 0);
            var bottomMidPt = m_center + new Vector3d(0, -m_verticalLength, 0);
            var bottomRightPt = m_center + new Vector3d(m_horizontalLength, -m_verticalLength, 0);

            // 左上保护矩形
            m_leftTop = new RectInfo(topLeftPt, topMidPt, centerLeftPt, m_center);

            // 左下保护矩形
            m_leftBottom = new RectInfo(centerLeftPt, m_center, bottomLeftPt, bottomMidPt);

            // 右上保护矩形
            m_rightTop = new RectInfo(topMidPt, topRightPt, m_center, centerRightPt);

            // 右下保护矩形
            m_rightBottom = new RectInfo(m_center, centerRightPt, bottomMidPt, bottomRightPt);
        }
    }

    public class RectInfo
    {
        public Point3d TopLeftPt; // 左上顶点
        public Point3d TopRightPt; // 右上顶点
        public Point3d BottomLeftPt; // 左下顶点
        public Point3d BottomRightPt; // 右下顶点
        
        public RectInfo(Point3d topLeftPt, Point3d topRightPt, Point3d bottomLeftPt, Point3d bottomRightPt)
        {
            TopLeftPt = topLeftPt;
            TopRightPt = topRightPt;
            BottomLeftPt = bottomLeftPt;
            BottomRightPt = bottomRightPt;
        }
    }
}
