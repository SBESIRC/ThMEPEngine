using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Assistant;
using ThMEPElectrical.Geometry;
using ThMEPElectrical.Model;


namespace ThMEPElectrical.Business
{
    /// <summary>
    /// 一列异形布置
    /// </summary>
    class OneColMultiSegmentsPlace
    {
        private PlaceParameter m_parameter;

        private PlaceRect m_placeRectInfo;

        public static List<Point3d> MakeOneColPlacePolygon(PlaceParameter parameter, PlaceRect placeRectInfo)
        {
            var oneColPlace = new OneColMultiSegmentsPlace(parameter, placeRectInfo);
            return oneColPlace.DoPlace();
        }

        private OneColMultiSegmentsPlace(PlaceParameter parameter, PlaceRect placeRectInfo)
        {
            m_parameter = parameter;
            m_placeRectInfo = placeRectInfo;
        }

        private List<Point3d> DoPlace()
        {
            var pts = OneColPlace();
            return pts;
        }

        private List<Point3d> OneColPlace()
        {
            var moveDis = m_placeRectInfo.BottomLine.Length * 0.5;
            var verticalMidLine = GeomUtils.MoveLine(m_placeRectInfo.LeftLine, Vector3d.XAxis, moveDis);

            var leftBottomPt = m_placeRectInfo.LeftBottomPt;
            var vertexProtectRadius = m_parameter.FirstBottomProtectRadius;
            var leftTopPt = m_placeRectInfo.LeftTopPt;

            // 左下顶点
            var leftBottomCircle = new Circle(leftBottomPt, Vector3d.ZAxis, vertexProtectRadius);
            var tempLeftBottomFirstPtNode = CalculateIntersectPt(leftBottomCircle, verticalMidLine);
            if (!tempLeftBottomFirstPtNode.HasValue)
                return null;

            var tempLeftBottomFirstPt = CalculateValidPoint(tempLeftBottomFirstPtNode.Value, m_placeRectInfo);
            if (!tempLeftBottomFirstPt.HasValue)
                return null;

            var leftBottomFirstPt = tempLeftBottomFirstPt.Value;

            // 左上顶点
            var leftTopCircle = new Circle(leftTopPt, Vector3d.ZAxis, vertexProtectRadius);
            var tempLeftTopLastPtNode = CalculateIntersectPt(leftTopCircle, verticalMidLine);
            if (!tempLeftTopLastPtNode.HasValue)
                return null;

            var tempLeftTopLastPt = CalculateValidPoint(tempLeftTopLastPtNode.Value, m_placeRectInfo);
            if (!tempLeftTopLastPt.HasValue)
                return null;

            var leftTopLastPt = tempLeftTopLastPt.Value;

            var verticalMaxGap = m_parameter.ProtectArea / 4.0 / moveDis * 2;

            // 计算垂直间隔长度
            var verticalLength = (leftTopLastPt - leftBottomFirstPt).Length;
            var verticalCount = Math.Ceiling(verticalLength / verticalMaxGap);
            var verticalPosGap = verticalLength / verticalCount;

            var ptLst = new List<Point3d>();
            ptLst.Add(leftBottomFirstPt);

            var tempStartPt = tempLeftBottomFirstPtNode.Value;
            for (int i = 1; i < verticalCount; i++)
            {
                var moveGap = i * verticalPosGap;
                var tempMidPt = tempStartPt + Vector3d.YAxis * moveGap;
                var ptNode = CalculateValidPoint(tempMidPt, m_placeRectInfo);
                if (!ptNode.HasValue)
                    continue;

                ptLst.Add(ptNode.Value);
            }

            ptLst.Add(leftTopLastPt);

            return ptLst;
        }

        /// <summary>
        /// 计算出有效点
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="placeRectInfo"></param>
        /// <returns></returns>
        private Point3d? CalculateValidPoint(Point3d pt, PlaceRect placeRectInfo)
        {
            var srcPoly = placeRectInfo.srcPolyline;

            if (GeomUtils.PtInLoop(srcPoly, pt.ToPoint2D()))
            {
                return pt;
            }
            else
            {
                var XExtendValue = Math.Abs(pt.X - placeRectInfo.LeftBottomPt.X) + 100;

                var vecExtend = Vector3d.XAxis * XExtendValue;
                var leftPt = pt - vecExtend;

                var rightPt = pt + vecExtend;

                var line = new Line(leftPt, rightPt);

                var midPt = IntersectMidPtHorizontal(srcPoly, line);

                if (!midPt.HasValue)
                    return null;

                return midPt.Value;
            }
        }

        /// <summary>
        /// 垂直方向上面计算有效的点
        /// </summary>
        /// <param name="curveFir"></param>
        /// <param name="curveSec"></param>
        /// <returns></returns>
        private Point3d? IntersectMidPtHorizontal(Curve curveFir, Curve curveSec)
        {
            var ptLst = CurveIntersectCurve(curveFir, curveSec);

            if (ptLst.Count != 0)
            {
                ptLst.Sort((p1, p2) => { return p1.X.CompareTo(p2.X); });
                return GeomUtils.GetMidPoint(ptLst.First(), ptLst.Last());
            }

            return null;
        }

        private List<Point3d> CurveIntersectCurve(Curve curveFir, Curve curveSec)
        {
            var ptCol = new Point3dCollection();
            curveFir.IntersectWith(curveSec, Intersect.OnBothOperands, ptCol, (IntPtr)0, (IntPtr)0);

            return ptCol.toPointList();
        }

        private Point3d? CalculateIntersectPt(Circle circle, Line line)
        {
            var ptLst = new Point3dCollection();
            circle.IntersectWith(line, Intersect.OnBothOperands, ptLst, (IntPtr)0, (IntPtr)0);
            if (ptLst.Count == 1)
            {
                return ptLst[0];
            }
            else if (ptLst.Count == 0)
            {
                return null;
            }
            else
            {
                return ptLst[0];
            }
        }
    }
}
