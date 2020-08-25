using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Model;
using ThMEPElectrical.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPElectrical.Assistant;

namespace ThMEPElectrical.Business
{
    /// <summary>
    /// 异形轮廓布置
    /// </summary>
    public class PolygonProfilePlace
    {
        private LayoutProfileData m_layoutProfileData; // 轮廓数据
        private PlaceParameter m_parameter; // 用户界面输入的参数数据

        private List<Point3d> m_singlePlacePts; // 插入点集

        private List<Point3d> PlacePts
        {
            get { return m_singlePlacePts; }
        }

        /// <summary>
        /// 异形布置
        /// </summary>
        /// <param name="layoutProfileData"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public static List<Point3d> MakePolygonProfilePoints(LayoutProfileData layoutProfileData, PlaceParameter parameter)
        {
            var polygonPlace = new PolygonProfilePlace(layoutProfileData, parameter);

            polygonPlace.DoPlace();

            return polygonPlace.PlacePts;
        }

        private PolygonProfilePlace(LayoutProfileData layoutProfileData, PlaceParameter parameter)
        {
            m_layoutProfileData = layoutProfileData;
            m_parameter = parameter;
        }

        private void DoPlace()
        {
            // 坐标转换
            var postPoly = m_layoutProfileData.PostPolyline;

            var coordinateTransform = new CoordinateTransform(postPoly);
            coordinateTransform.DataTrans();
            var transPostPoly = coordinateTransform.TransPolyline;
            var matrixs = coordinateTransform.TransMatrixs;

            // 原始多段线进行矩阵转换后的数据
            var srcPoly = GeometryTrans.TransByMatrix(m_layoutProfileData.SrcPolyline, matrixs);

            // 布置矩形信息
            var placeRectInfo = GeomUtils.CalculateProfileRectInfo(transPostPoly);

            placeRectInfo.srcPolyline = srcPoly;

            var inverseMatrixs = coordinateTransform.InverseMatrixs;
            // 计算布置点
            var pts = CalculatePts(placeRectInfo, srcPoly);

            // 转回来
            m_singlePlacePts = GeometryTrans.TransByMatrix(pts, inverseMatrixs);
        }

        /// <summary>
        /// 计算布置点
        /// </summary>
        /// <param name="placeRectInfo"></param>
        private List<Point3d> CalculatePts(PlaceRect placeRectInfo, Polyline srcPolyline)
        {
            if (placeRectInfo == null)
                return null;

            var leftLine = placeRectInfo.LeftLine;
            var bottomLine = placeRectInfo.BottomLine;

            var rectArea = leftLine.Length * bottomLine.Length;

            // 一个可以布置完的
            if (leftLine.Length < 2 * m_parameter.ProtectRadius && bottomLine.Length < 2 * m_parameter.ProtectRadius && rectArea < m_parameter.ProtectArea)
            {
                var center = GeomUtils.GetMidPoint(bottomLine.EndPoint, leftLine.EndPoint);
                return new List<Point3d>() { center };
            }

            var verticalCount = Math.Ceiling(leftLine.Length / m_parameter.VerticalMaxGap);
            var verticalGap = leftLine.Length / verticalCount;

            // 布置一行的
            if (verticalCount == 1)
            {
                var midLine = GeomUtils.MoveLine(bottomLine, Vector3d.YAxis, leftLine.Length * 0.5);
                return OneRowPlace(midLine, placeRectInfo, leftLine.Length * 0.5);
            }
            else
            {
                return MultiRowPlace(placeRectInfo, verticalCount, verticalGap);
            }
        }



        /// <summary>
        /// 单行布置
        /// </summary>
        /// <param name="midLine"></param>
        /// <param name="placeRectInfo"></param>
        private List<Point3d> OneRowPlace(Line midLine, PlaceRect placeRectInfo, double verticalA)
        {
            var ptNodes = BottomRowPlacePts(midLine, placeRectInfo, verticalA);
            
            if (ptNodes != null && ptNodes.Count != 0)
            {
                var pts = ptNodes.Select(e => e.InsertPt).ToList();
                return pts;
            }

            return null;
        }

        /// <summary>
        /// 计算出有效点
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="placeRectInfo"></param>
        /// <returns></returns>
        private PlacePoint CalculateValidPoint(Point3d pt, PlaceRect placeRectInfo)
        {
            var srcPoly = placeRectInfo.srcPolyline;

            if (GeomUtils.PtInLoop(srcPoly, pt.ToPoint2D()))
            {
                return new PlacePoint(pt, false);
            }
            else
            {
                var yExtendValue = Math.Abs(pt.Y - placeRectInfo.LeftBottomPt.Y) + 100;

                var downPt = pt - Vector3d.YAxis * yExtendValue;

                var line = new Line(pt, downPt);

                return CalculateIntersectPt(srcPoly, line);
            }
        }

        private PlacePoint CalculateValidPoint(Point3d pt, Line bottomLine, Polyline srcPoly)
        {
            if (GeomUtils.PtInLoop(srcPoly, pt.ToPoint2D()))
            {
                return new PlacePoint(pt, false);
            }
            else
            {
                var yExtendValue = Math.Abs(pt.Y - bottomLine.StartPoint.Y) + 100;

                var downPt = pt - Vector3d.YAxis * yExtendValue;

                var line = new Line(pt, downPt);

                return CalculateIntersectPt(srcPoly, line);
            }
        }

        /// <summary>
        /// 重新计算点
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        private PlacePoint CalculateIntersectPt(Polyline poly, Line line)
        {
            var ptLst = new Point3dCollection();
            poly.IntersectWith(line, Intersect.OnBothOperands, ptLst, (IntPtr)0, (IntPtr)0);
            if (ptLst.Count == 1)
            {
                return new PlacePoint(ptLst[0], true);
            }
            else
            {
                // 与原始的多段线相交， 然后取出边界的点，然后最大的边界点计算中间的有效点
                var pts = ptLst.toPointList();
                pts.Sort((pt1, pt2) => { return pt1.Y.CompareTo(pt2.Y); });

                var midPt = GeomUtils.GetMidPoint(pts.First(), pts.Last());
                return new PlacePoint(midPt, true);
            }

            throw new Exception(ptLst.Count.ToString());
        }


        private List<PlacePoint> BottomRowPlacePts(Line line, PlaceRect placeRectInfo, double verticalA)
        {
            var vertexProtectRadius = m_parameter.FirstBottomProtectRadius;
            var leftBottomPt = placeRectInfo.LeftBottomPt;
            var rightBottomPt = placeRectInfo.RightBottomPt;

            // 计算最大水平间隔
            var horizontalMaxGap = m_parameter.ProtectArea / 4.0 / verticalA * 2;
            // 左下顶点
            var leftBottomPtCircle = new Circle(leftBottomPt, Vector3d.ZAxis, vertexProtectRadius);
            var tempLeftFirstPt = CalculateLeftIntersectPt(leftBottomPtCircle, line);
            var leftFirstPtNode = CalculateValidPoint(tempLeftFirstPt, placeRectInfo);

            // 右下顶点
            var rightBottomCircle = new Circle(rightBottomPt, Vector3d.ZAxis, vertexProtectRadius);
            var tempRightLastPt = CalculateRightIntersectPt(rightBottomCircle, line);
            var rightLastPtNode = CalculateValidPoint(tempRightLastPt, placeRectInfo);

            // 计算水平间隔长度
            var horizontalLength = (leftFirstPtNode.InsertPt - rightLastPtNode.InsertPt).Length;

            var horizontalCount = Math.Ceiling(horizontalLength / horizontalMaxGap);

            // 整数布置后的水平间隔距离
            var horizontalPosGap = horizontalLength / horizontalCount;

            var ptLst = new List<PlacePoint>();
            ptLst.Add(leftFirstPtNode);

            for (int i = 1; i < horizontalCount; i++)
            {
                var moveGap = i * horizontalPosGap;
                var pt = tempLeftFirstPt + Vector3d.XAxis * moveGap;
                var validPt = CalculateValidPoint(pt, placeRectInfo);
                ptLst.Add(validPt);
            }

            ptLst.Add(rightLastPtNode);
            return ptLst;
        }

        private Point3d CalculateLeftIntersectPt(Circle circle, Line line)
        {
            var ptLst = new Point3dCollection();
            circle.IntersectWith(line, Intersect.OnBothOperands, ptLst, (IntPtr)0, (IntPtr)0);
            if (ptLst.Count == 1)
            {
                return ptLst[0];
            }
            else if (ptLst.Count == 2)
            {
                return (ptLst[0].X < ptLst[1].X) ? ptLst[1] : ptLst[0];
            }

            throw new Exception(ptLst.Count.ToString());
        }

        private Point3d CalculateRightIntersectPt(Circle circle, Line line)
        {
            var ptLst = new Point3dCollection();
            circle.IntersectWith(line, Intersect.OnBothOperands, ptLst, (IntPtr)0, (IntPtr)0);
            if (ptLst.Count == 1)
            {
                return ptLst[0];
            }
            else if (ptLst.Count == 2)
            {
                return (ptLst[0].X < ptLst[1].X) ? ptLst[0] : ptLst[1];
            }
            
            throw new Exception(ptLst.Count.ToString());
        }

        /// <summary>
        /// 多行布置
        /// </summary>
        /// <param name="placeRectInfo"></param>
        /// <param name="verticalCount"></param>
        /// <param name="verticalGap"></param>
        private List<Point3d> MultiRowPlace(PlaceRect placeRectInfo, double verticalCount, double verticalGap)
        {
            var placePoints = MultiRowPlacePts(placeRectInfo, verticalCount, verticalGap);

            if (placePoints == null || placePoints.Count == 0)
                return null;

            var pts = placePoints.Select(e => e.InsertPt).ToList();
            return pts;
        }

        private List<PlacePoint> MultiRowPlacePts(PlaceRect placeRectInfo, double verticalCount, double verticalGap)
        {
            var verticalA = verticalGap / 2.0;

            var resPlacePoints = new List<PlacePoint>();
            // 最低行的数据
            var firstLine = GeomUtils.MoveLine(placeRectInfo.BottomLine, Vector3d.YAxis, verticalA);

            var bottomPlacePts = BottomRowPlacePts(firstLine, placeRectInfo, verticalA);

            var lastPlacePts = bottomPlacePts;
            resPlacePoints.AddRange(lastPlacePts);
            // 计算所有列的数据
            for (int i = 1; i < verticalCount; i++)
            {
                var nextLine = GeomUtils.MoveLine(firstLine, Vector3d.YAxis, i * verticalGap);

                // nextLine 是lastPlacePts 的上面一行可能的布置水平线
                lastPlacePts = NextRowPlacePts(nextLine, lastPlacePts, placeRectInfo, verticalGap);

                if (lastPlacePts != null || lastPlacePts.Count != 0)
                    resPlacePoints.AddRange(lastPlacePts);
            }

            return resPlacePoints;
        }


        /// <summary>
        /// 返回左边点
        /// </summary>
        /// <param name="placePoints"></param>
        /// <returns></returns>
        private PlacePoint GetLeftPlacePoint(List<PlacePoint> placePoints)
        {
            if (placePoints == null || placePoints.Count == 0)
                return null;

            if (placePoints.Count == 1)
                return placePoints.First();

            var leftPlacePoint = placePoints.First();

            for (int i = 1; i < placePoints.Count; i++)
            {
                var curPlacePoint = placePoints[i];
                if (!curPlacePoint.IsMoved)
                {
                    return curPlacePoint;
                }
            }

            return leftPlacePoint;
        }


        /// <summary>
        /// 返回右边点
        /// </summary>
        /// <param name="placePoints"></param>
        /// <returns></returns>
        private PlacePoint GetRightPlacePoint(List<PlacePoint> placePoints)
        {
            if (placePoints == null || placePoints.Count == 0)
                return null;

            if (placePoints.Count == 1)
                return placePoints.First();

            var rightPlacePoint = placePoints.Last();

            // 倒数第二个起
            for (int i = placePoints.Count - 2; i > 0; i--)
            {
                var curPlacePoint = placePoints[i];

                if (!curPlacePoint.IsMoved)
                {
                    return curPlacePoint;
                }
            }

            return rightPlacePoint;
        }

        /// <summary>
        /// 基于上一行的布置
        /// </summary>
        /// <param name="bottomLine"></param>
        /// <param name="placePts"></param>
        /// <param name="placeRectInfo"></param>
        /// <param name="verticalA"></param>
        /// <returns></returns>
        private List<PlacePoint> NextRowPlacePts(Line upLine, List<PlacePoint> placePts, PlaceRect placeRectInfo, double verticalA)
        {
            var protectRadius = m_parameter.ProtectRadius;

            // 计算最大水平间隔
            var horizontalMaxGap = m_parameter.ProtectArea / 4.0 / verticalA * 2;
            
            var leftPlaceNode = GetLeftPlacePoint(placePts); // 左边第一个插入点
            var rightPlaceNode = GetRightPlacePoint(placePts); // 右边第一个插入点
            if (GeomUtils.Point3dIsEqualPoint3d(leftPlaceNode.InsertPt, rightPlaceNode.InsertPt))
                return null;

            var leftBottomPt = leftPlaceNode.InsertPt;
            var rightBottomPt = placeRectInfo.RightBottomPt;

            var upLineDown = GeomUtils.MoveLine(upLine, Vector3d.YAxis, -verticalA);
            // 左下顶点
            var leftBottomPtCircle = new Circle(leftBottomPt, Vector3d.ZAxis, protectRadius);
            var tempLeftFirstPt = CalculateLeftIntersectPt(leftBottomPtCircle, upLine);
            var leftFirstPtNode = CalculateValidPoint(tempLeftFirstPt, upLineDown, placeRectInfo.srcPolyline);

            // 右下顶点
            var rightBottomCircle = new Circle(rightBottomPt, Vector3d.ZAxis, protectRadius);
            var tempRightLastPt = CalculateRightIntersectPt(rightBottomCircle, upLine);

            var rightLastPtNode = CalculateValidPoint(tempRightLastPt, upLineDown, placeRectInfo.srcPolyline);

            // 计算水平间隔长度
            var horizontalLength = (leftFirstPtNode.InsertPt - rightLastPtNode.InsertPt).Length;

            var horizontalCount = Math.Ceiling(horizontalLength / horizontalMaxGap);

            // 整数布置后的水平间隔距离
            var horizontalPosGap = horizontalLength / horizontalCount;

            var ptLst = new List<PlacePoint>();
            ptLst.Add(leftFirstPtNode);

            for (int i = 1; i < horizontalCount; i++)
            {
                var moveGap = i * horizontalPosGap;
                var pt = tempLeftFirstPt + Vector3d.XAxis * moveGap;
                var validPt = CalculateValidPoint(pt, upLineDown, placeRectInfo.srcPolyline);
                ptLst.Add(validPt);
            }

            ptLst.Add(rightLastPtNode);
            return ptLst;
        }
    }
}
