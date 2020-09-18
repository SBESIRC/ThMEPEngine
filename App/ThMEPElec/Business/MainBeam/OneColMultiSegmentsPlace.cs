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
using ThMEPElectrical.PostProcess;

namespace ThMEPElectrical.Business.MainBeam
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

        protected OneColMultiSegmentsPlace(PlaceParameter parameter, PlaceRect placeRectInfo)
        {
            m_parameter = parameter;
            m_placeRectInfo = placeRectInfo;
        }

        protected List<Point3d> DoPlace()
        {
            var pts = OneColPlace();
            return pts;
        }

        protected virtual List<Point3d> OneColPlace()
        {
            var ptLst = new List<Point3d>();
            var moveDis = m_placeRectInfo.BottomLine.Length * 0.5;
            var verticalMidLine = GeomUtils.MoveLine(m_placeRectInfo.LeftLine, Vector3d.XAxis, moveDis);

            var leftBottomPt = m_placeRectInfo.LeftBottomPt;
            var vertexProtectRadius = m_parameter.FirstBottomProtectRadius;
            var leftTopPt = m_placeRectInfo.LeftTopPt;

            // 左下顶点
            var leftBottomCircle = new Circle(leftBottomPt, Vector3d.ZAxis, vertexProtectRadius);
            var tempLeftBottomFirstPtNode = CalculateIntersectPt(leftBottomCircle, verticalMidLine);
            if (!tempLeftBottomFirstPtNode.HasValue)
                return ptLst;

            var tempLeftBottomFirstPt = CalculateValidPoint(tempLeftBottomFirstPtNode.Value, m_placeRectInfo);
            if (tempLeftBottomFirstPt == null)
                return ptLst;

            var leftBottomFirstPt = tempLeftBottomFirstPt.InsertPt;

            // 左上顶点
            var leftTopCircle = new Circle(leftTopPt, Vector3d.ZAxis, vertexProtectRadius);
            var tempLeftTopLastPtNode = CalculateIntersectPt(leftTopCircle, verticalMidLine);
            if (!tempLeftTopLastPtNode.HasValue)
                return ptLst;

            var tempLeftTopLastPt = CalculateValidPoint(tempLeftTopLastPtNode.Value, m_placeRectInfo);
            if (tempLeftTopLastPt == null)
                return ptLst;

            var leftTopLastPt = tempLeftTopLastPt.InsertPt;

            var verticalMaxGap = m_parameter.ProtectArea / 4.0 / moveDis * 2;

            // 计算垂直间隔长度
            var verticalLength = (leftTopLastPt - leftBottomFirstPt).Length;
            var verticalCount = Math.Ceiling(verticalLength / verticalMaxGap);
            var verticalPosGap = verticalLength / verticalCount;

            var placeNodes = new List<PlacePoint>();
            placeNodes.Add(tempLeftBottomFirstPt);

            var tempStartPt = tempLeftBottomFirstPtNode.Value;
            for (int i = 1; i < verticalCount; i++)
            {
                var moveGap = i * verticalPosGap;
                var tempMidPt = tempStartPt + Vector3d.YAxis * moveGap;
                var ptNode = CalculateValidPoint(tempMidPt, m_placeRectInfo);
                if (ptNode == null)
                    continue;

                placeNodes.Add(ptNode);
            }

            placeNodes.Add(tempLeftTopLastPt);

            placeNodes.ForEach(e => ptLst.Add(e.InsertPt));

            // 是否需要进行后处理判断
            if (IsNeedPostProcess(placeNodes))
            {
                var validVerticalMidLine = GeomUtils.CalculateMidLine(verticalMidLine, m_placeRectInfo.srcPolyline);
                ptLst = PlacePointAdjustor.MakePlacePointAdjustor(ptLst, validVerticalMidLine, ShapeConstraintType.NONREGULARSHAPE);
            }

            return ptLst;
        }

        /// <summary>
        /// 是否需要进行后处理调整
        /// </summary>
        /// <param name="placeNodes"></param>
        /// <returns></returns>
        private bool IsNeedPostProcess(List<PlacePoint> placeNodes)
        {
            if (placeNodes.Count == 2)
            {
                foreach (var singlePlaceNode in placeNodes)
                {
                    if (singlePlaceNode.IsMoved)
                        return false;
                }

                return true;
            }

            return false;
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
                var XExtendValue = Math.Abs(pt.X - placeRectInfo.LeftBottomPt.X) + 100;

                var vecExtend = Vector3d.XAxis * XExtendValue;
                var leftPt = pt - vecExtend;

                var rightPt = pt + vecExtend;

                var line = new Line(leftPt, rightPt);

                var midPt = IntersectMidPtHorizontal(srcPoly, line);

                if (!midPt.HasValue)
                    return null;

                return new PlacePoint(midPt.Value, true);
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
            var ptLst = GeomUtils.CurveIntersectCurve(curveFir, curveSec);

            if (ptLst.Count != 0)
            {
                ptLst.Sort((p1, p2) => { return p1.X.CompareTo(p2.X); });
                return GeomUtils.GetMidPoint(ptLst.First(), ptLst.Last());
            }

            return null;
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
