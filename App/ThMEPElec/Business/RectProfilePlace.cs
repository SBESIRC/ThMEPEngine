using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Model;
using ThMEPElectrical.Assistant;
using ThMEPElectrical.Geometry;
using ThMEPElectrical.PostProcess;

namespace ThMEPElectrical.Business
{
    /// <summary>
    /// 单个布置 矩形
    /// </summary>
    public class RectProfilePlace
    {
        private LayoutProfileData m_layoutProfile = null; // 轮廓
        private PlaceParameter m_parameter = null; // 界面用户输入参数

        private List<Point3d> m_singlePlacePts = new List<Point3d>(); // 插入点

        private List<Point3d> SinglePlacePts
        {
            get { return m_singlePlacePts; }
        }

        private RectProfilePlace(LayoutProfileData poly, PlaceParameter placeParameter)
        {
            m_layoutProfile = poly;
            m_parameter = placeParameter;
        }

        /// <summary>
        /// 带有坐标系的布置
        /// </summary>
        /// <param name="layoutData"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public static List<Point3d> MakeRectProfilePlacePoints(LayoutProfileData layoutData, PlaceParameter parameter)
        {
            var singleProfilePlace = new RectProfilePlace(layoutData, parameter);
            singleProfilePlace.Do();

            return singleProfilePlace.SinglePlacePts;
        }

        /// <summary>
        /// ABB矩形布置
        /// </summary>
        /// <param name="layoutData"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public static List<Point3d> MakeABBRectProfilePlacePoints(LayoutProfileData layoutData, PlaceParameter parameter)
        {
            var singleProfilePlace = new RectProfilePlace(layoutData, parameter);
            singleProfilePlace.DoABB();

            return singleProfilePlace.SinglePlacePts;
        }

        /// <summary>
        /// ABB计算
        /// </summary>
        private void DoABB()
        {
            // 坐标转化
            var postPoly = m_layoutProfile.PostPolyline;

            // 布置矩形信息
            var placeRectInfo = GeomUtils.CalculateProfileRectInfo(postPoly);

            // 计算布置点
            CalculatePts(placeRectInfo);
        }

        private void Do()
        {
            // 坐标转化
            var postPoly = m_layoutProfile.PostPolyline;
            var coordinateTransform = new CoordinateTransform(postPoly);
            coordinateTransform.DataTrans();

            var transPoly = coordinateTransform.TransPolyline;

            // 逆矩阵
            var inverseMatrixs = coordinateTransform.InverseMatrixs;

            // 布置矩形信息
            var placeRectInfo = GeomUtils.CalculateProfileRectInfo(transPoly);

            // 计算布置点
            CalculatePts(placeRectInfo);

            var tempPts = GeometryTrans.TransByMatrix(m_singlePlacePts, inverseMatrixs);
            m_singlePlacePts.Clear();
            m_singlePlacePts.AddRange(tempPts);
        }

        /// <summary>
        /// 计算布置点
        /// </summary>
        /// <param name="placeRectInfo"></param>
        private void CalculatePts(PlaceRect placeRectInfo)
        {
            if (placeRectInfo == null)
                return;

            var leftLine = placeRectInfo.LeftLine;
            var bottomLine = placeRectInfo.BottomLine;

            var rectArea = leftLine.Length * bottomLine.Length;

            // 一个可以布置完的
            if (leftLine.Length < 2 * m_parameter.ProtectRadius && bottomLine.Length < 2 * m_parameter.ProtectRadius && rectArea < m_parameter.ProtectArea)
            {
                var center = GeomUtils.GetMidPoint(bottomLine.EndPoint, leftLine.EndPoint);
                m_singlePlacePts.Add(center);
                return;
            }

            var verticalCount = Math.Ceiling(leftLine.Length / m_parameter.VerticalMaxGap);
            var verticalGap = leftLine.Length / verticalCount;

            var horizontalCount = Math.Ceiling(bottomLine.Length / m_parameter.HorizontalMaxGap);

            // 布置一行的
            if (verticalCount == 1)
            {
                var horizontalMidLine = GeomUtils.MoveLine(bottomLine, Vector3d.YAxis, leftLine.Length * 0.5);
                OneRowPlace(horizontalMidLine, placeRectInfo, leftLine.Length * 0.5);
            }
            else if (horizontalCount == 1)
            {
                var pts = OneColRectPlace.MakeOneColPlaceRect(m_parameter, placeRectInfo);
                if (pts != null && pts.Count != 0)
                    m_singlePlacePts.AddRange(pts);
            }
            else
            {
                MultiRowPlace(placeRectInfo, verticalCount, verticalGap);
            }
        }

        /// <summary>
        /// 多行布置
        /// </summary>
        /// <param name="placeRectInfo"></param>
        /// <param name="verticalCount"></param>
        /// <param name="verticalGap"></param>
        private void MultiRowPlace(PlaceRect placeRectInfo, double verticalCount, double verticalGap)
        {
            var verticalA = verticalGap / 2.0;

            // 最低行的数据
            var bottomLine = GeomUtils.MoveLine(placeRectInfo.BottomLine, Vector3d.YAxis, verticalA);
            var srcPts = BottomRowPlacePts(bottomLine, placeRectInfo, verticalA);
            if (srcPts.Count == 2)
            {
                // 规则约束调整，美观调整，距离调整等
                srcPts = PlacePointAdjustor.MakePlacePointAdjustor(srcPts, bottomLine, ShapeConstraintType.REGULARSHAPE);
            }

            m_singlePlacePts.AddRange(srcPts);

            // 计算所有列的数据
            for (int i = 1; i < verticalCount; i++)
            {
                var movePts = MovePts(srcPts, Vector3d.YAxis, i * verticalGap);
                if (movePts != null || movePts.Count != 0)
                    m_singlePlacePts.AddRange(movePts);
            }
        }

        /// <summary>
        /// 点集的平移
        /// </summary>
        /// <param name="srcPts"></param>
        /// <param name="moveDir"></param>
        /// <param name="moveDistance"></param>
        /// <returns></returns>
        private List<Point3d> MovePts(List<Point3d> srcPts, Vector3d moveDir, double moveDistance)
        {
            if (srcPts == null || srcPts.Count == 0)
                return null;

            var matrix = Matrix3d.Displacement(moveDir * moveDistance);
            var postPts = new List<Point3d>();

            foreach (var pt in srcPts)
            {
                var postPt = pt.TransformBy(matrix);
                postPts.Add(postPt);
            }

            return postPts;
        }

        /// <summary>
        /// 单行布置
        /// </summary>
        /// <param name="midLine"></param>
        /// <param name="placeRectInfo"></param>
        private void OneRowPlace(Line midLine, PlaceRect placeRectInfo, double verticalA)
        {
            var ptLst = BottomRowPlacePts(midLine, placeRectInfo, verticalA);

            if (ptLst.Count != 2)
            {
                m_singlePlacePts.AddRange(ptLst);
                return;
            }

            // 规则约束调整，美观调整，距离调整等
            var adjustorPts = PlacePointAdjustor.MakePlacePointAdjustor(ptLst, midLine, ShapeConstraintType.REGULARSHAPE);
            m_singlePlacePts.AddRange(adjustorPts);
        }


        private List<Point3d> BottomRowPlacePts(Line line, PlaceRect placeRectInfo, double verticalA)
        {
            var ptLst = new List<Point3d>();
            var vertexProtectRadius = m_parameter.FirstBottomProtectRadius;
            var leftBottomPt = placeRectInfo.LeftBottomPt;
            var rightBottomPt = placeRectInfo.RightBottomPt;

            // 计算最大水平间隔
            var horizontalMaxGap = m_parameter.ProtectArea / 4.0 / verticalA * 2;
            // 左下顶点
            var leftBottomPtCircle = new Circle(leftBottomPt, Vector3d.ZAxis, vertexProtectRadius);
            var tempLeftFirstPt = CalculateIntersectPt(leftBottomPtCircle, line);

            if (!tempLeftFirstPt.HasValue)
                return ptLst;

            var leftFirstPt = tempLeftFirstPt.Value;

            // 右下顶点
            var rightBottomCircle = new Circle(rightBottomPt, Vector3d.ZAxis, vertexProtectRadius);
            var tempRightLastPt = CalculateIntersectPt(rightBottomCircle, line);

            if (!tempRightLastPt.HasValue)
                return ptLst;

            var rightLastPt = tempRightLastPt.Value;
            // 计算水平间隔长度
            var horizontalLength = (rightLastPt - leftFirstPt).Length;

            var horizontalCount = Math.Ceiling(horizontalLength / horizontalMaxGap);

            // 整数布置后的水平间隔距离
            var horizontalPosGap = horizontalLength / horizontalCount;

            ptLst.Add(leftFirstPt);

            for (int i = 1; i < horizontalCount; i++)
            {
                var moveGap = i * horizontalPosGap;
                var pt = leftFirstPt + Vector3d.XAxis * moveGap;
                ptLst.Add(pt);
            }

            ptLst.Add(rightLastPt);
            return ptLst;
        }

        private Point3d? CalculateIntersectPt(Circle circle, Line line)
        {
            var ptLst = new Point3dCollection();
            circle.IntersectWith(line, Intersect.OnBothOperands, ptLst, (IntPtr)0, (IntPtr)0);
            if (ptLst.Count == 1)
            {
                return ptLst[0];
            }

            return null;
        }
    }
}
