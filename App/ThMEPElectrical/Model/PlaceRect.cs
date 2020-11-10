using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.Model
{
    /// <summary>
    /// 布置矩形转换后的X-Y坐标信息, Line 的方向都是从左到右，从下到上的
    /// </summary>
    public class PlaceRect
    {
        public Line BottomLine; // 下边
        public Line LeftLine; // 左边

        public Line TopLine; // 上边
        public Line RightLine; // 右边

        public Point3d LeftBottomPt; // 左下点
        public Point3d LeftTopPt; // 左上点

        public Point3d RightBottomPt; // 右下点
        public Point3d RightTopPt; // 右上点

        public Polyline srcPolyline; // 原始的经过矩阵转换后的多段线

        public PlaceRect(Line bottomLine, Line leftLine, Line topLine, Line rightLine, Polyline srcPoly = null)
        {
            BottomLine = bottomLine;
            LeftLine = leftLine;

            TopLine = topLine;
            RightLine = rightLine;

            LeftBottomPt = bottomLine.StartPoint;
            LeftTopPt = leftLine.EndPoint;

            RightBottomPt = bottomLine.EndPoint;
            RightTopPt = rightLine.EndPoint;

            srcPolyline = srcPoly;
        }
    }
}
