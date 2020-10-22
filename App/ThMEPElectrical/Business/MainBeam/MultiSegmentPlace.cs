using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Model;
using ThMEPElectrical.Assistant;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPElectrical.Geometry;
using ThCADCore.NTS;
using ThMEPElectrical.PostProcess;

namespace ThMEPElectrical.Business.MainBeam
{
    /// <summary>
    /// 异形排布计算
    /// </summary>
    public class MultiSegmentPlace
    {
        protected LayoutProfileData m_layoutProfileData; // 轮廓数据
        protected PlaceParameter m_parameter; // 用户界面输入的参数数据

        protected List<Point3d> m_singlePlacePts; // 插入点集

        protected List<Point3d> PlacePts
        {
            get { return m_singlePlacePts; }
        }

        protected MultiSegmentPlace(LayoutProfileData layoutProfileData, PlaceParameter parameter)
        {
            m_layoutProfileData = layoutProfileData;
            m_parameter = parameter;
        }

        /// <summary>
        /// 带有坐标系的异形布置
        /// </summary>
        /// <param name="layoutProfileData"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public static List<Point3d> MakePolygonProfilePoints(LayoutProfileData layoutProfileData, PlaceParameter parameter)
        {
            var polygonPlace = new MultiSegmentPlace(layoutProfileData, parameter);

            polygonPlace.DoPlace();

            return polygonPlace.PlacePts;
        }

        /// <summary>
        /// ABB异形布置
        /// </summary>
        /// <param name="layoutProfileData"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public static List<Point3d> MakeABBPolygonProfilePoints(LayoutProfileData layoutProfileData, PlaceParameter parameter)
        {
            var polygonPlace = new MultiSegmentPlace(layoutProfileData, parameter);

            polygonPlace.DoABBPlace();

            return polygonPlace.PlacePts;
        }


        protected void DoABBPlace()
        {
            // 坐标转换
            var postPoly = m_layoutProfileData.PostPolyline;

            //DrawUtils.DrawProfile(new List<Curve>() { transPostPoly }, "transPostPoly");
            //DrawUtils.DrawProfile(new List<Curve>() { srcTransPoly }, "srcTransPoly");
            // 布置矩形信息
            var placeRectInfo = GeomUtils.CalculateProfileRectInfo(postPoly);

            placeRectInfo.srcPolyline = m_layoutProfileData.SrcPolyline;

            // 计算布置点
            m_singlePlacePts = CalculatePts(placeRectInfo);
        }

        private void DoPlace()
        {
            // 坐标转换
            var postPoly = m_layoutProfileData.PostPolyline;

            // 根据外接矩形进行坐标转换
            var coordinateTransform = new CoordinateTransform(postPoly);
            coordinateTransform.DataTrans();
            var transPostPoly = coordinateTransform.TransPolyline;
            var matrixs = coordinateTransform.TransMatrixs;

            // 原始多段线进行矩阵转换后的数据
            var srcTransPoly = GeometryTrans.TransByMatrix(m_layoutProfileData.SrcPolyline, matrixs);

            //DrawUtils.DrawProfile(new List<Curve>() { transPostPoly }, "transPostPoly");
            //DrawUtils.DrawProfile(new List<Curve>() { srcTransPoly }, "srcTransPoly");
            // 布置矩形信息
            var placeRectInfo = GeomUtils.CalculateProfileRectInfo(transPostPoly);

            placeRectInfo.srcPolyline = srcTransPoly;

            // 逆矩阵 计算
            var inverseMatrixs = coordinateTransform.InverseMatrixs;

            // 计算布置点
            var pts = CalculatePts(placeRectInfo);

            // 转回来
            m_singlePlacePts = GeometryTrans.TransByMatrix(pts, inverseMatrixs);
        }

        /// <summary>
        /// 一行布置
        /// </summary>
        /// <param name="line"></param>
        /// <param name="placeRectInfo"></param>
        /// <param name="verticalA"></param>
        /// <returns></returns>
        protected virtual List<Point3d> OneRowPlace(Line midLine, PlaceRect placeRectInfo, double verticalA)
        {
            var vertexProtectRadius = m_parameter.FirstBottomProtectRadius;
            var leftBottomPt = placeRectInfo.LeftBottomPt;
            var rightBottomPt = placeRectInfo.RightBottomPt;

            var leftLine = placeRectInfo.LeftLine;
            var bottomLine = placeRectInfo.BottomLine;

            var rectArea = leftLine.Length * bottomLine.Length;

            // 一个可以布置完的
            if (leftLine.Length < 2 * m_parameter.ProtectRadius && bottomLine.Length < 2 * m_parameter.ProtectRadius && rectArea < m_parameter.ProtectArea)
            {
                return GeomUtils.CalculateCentroidFromPoly(placeRectInfo.srcPolyline);
            }

            // 计算最大水平间隔
            var horizontalMaxGap = m_parameter.ProtectArea / 4.0 / verticalA * 2;
            var extremalMaxGap = Math.Sqrt(Math.Pow(m_parameter.ProtectRadius, 2) - Math.Pow(verticalA, 2)) * 2;
            if (horizontalMaxGap > extremalMaxGap)
                horizontalMaxGap = extremalMaxGap;

            // 左下顶点
            var leftBottomPtCircle = new Circle(leftBottomPt, Vector3d.ZAxis, vertexProtectRadius);
            var tempLeftFirstPt = LeftEdgeIntersectHorizontal(leftBottomPtCircle, midLine);

            if (!tempLeftFirstPt.HasValue)
                return null;

            var leftFirstPt = tempLeftFirstPt.Value;
            var leftFirstPtNode = CalculateValidPoint(leftFirstPt, placeRectInfo);

            // 右下顶点
            var rightBottomCircle = new Circle(rightBottomPt, Vector3d.ZAxis, vertexProtectRadius);
            var tempRightLastPt = RightEdgeIntersectHorizontal(rightBottomCircle, midLine);

            if (!tempRightLastPt.HasValue)
                return null;

            var rightLastPt = tempRightLastPt.Value;
            var rightLastPtNode = CalculateValidPoint(rightLastPt, placeRectInfo);

            // 计算水平间隔长度
            var horizontalLength = (leftFirstPtNode.InsertPt - rightLastPtNode.InsertPt).Length;

            var horizontalCount = Math.Ceiling(horizontalLength / horizontalMaxGap);

            // 整数布置后的水平间隔距离
            var horizontalPosGap = horizontalLength / horizontalCount;

            var placeNodes = new List<PlacePoint>();
            var placePoints = new List<Point3d>();
            placeNodes.Add(leftFirstPtNode);

            for (int i = 1; i < horizontalCount; i++)
            {
                var moveGap = i * horizontalPosGap;
                var pt = leftFirstPt + Vector3d.XAxis * moveGap;
                var validPt = CalculateValidPoint(pt, placeRectInfo);
                placeNodes.Add(validPt);
            }

            placeNodes.Add(rightLastPtNode);
            placeNodes.ForEach(e => placePoints.Add(e.InsertPt));

            // 是否需要进行后处理判断
            if (IsNeedPostProcess(placeNodes))
            {
                var validMidLine = CalculateMidLine(midLine, placeRectInfo.srcPolyline);
                placePoints = PlacePointAdjustor.MakePlacePointAdjustor(placePoints, validMidLine, ShapeConstraintType.NONREGULARSHAPE);
            }

            return CalculateRegionPoints(placeRectInfo.srcPolyline, placePoints);
        }

        private List<Point3d> CalculateRegionPoints(Polyline srcPoly, List<Point3d> pts)
        {
            // 计算内轮廓和偏移计算
            var resProfiles = new List<Polyline>();
            foreach (Polyline offsetPoly in srcPoly.Buffer(ThMEPCommon.ShrinkDistance))
                resProfiles.Add(offsetPoly);

            var mainBeamRegion = new MainSecondBeamRegion(resProfiles, pts);
            List<Point3d> resPts;

            if (pts.Count == 1)
            {
                resPts = MainSecondBeamPointAdjustor.MakeMainBeamPointAdjustor(mainBeamRegion, MSPlaceAdjustorType.SINGLEPLACE);
            }
            else if (pts.Count == 2)
            {
                resPts = MainSecondBeamPointAdjustor.MakeMainBeamPointAdjustor(mainBeamRegion, MSPlaceAdjustorType.MEDIUMPLACE);
            }
            else
            {
                resPts = pts;
            }

            return resPts;
        }

        private Line CalculateMidLine(Line first, Polyline sec)
        {
            var pts = GeomUtils.CurveIntersectCurve(first, sec);
            if (pts.Count < 2)
                return first;

            var startPt = first.StartPoint;
            pts.Sort((p1, p2) => { return p1.DistanceTo(startPt).CompareTo(p2.DistanceTo(startPt)); });

            return new Line(pts.First(), pts.Last());
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
                var yExtendValue = Math.Abs(pt.Y - placeRectInfo.LeftBottomPt.Y) + 100;

                var vecExtend = Vector3d.YAxis * yExtendValue;
                var downPt = pt - vecExtend;

                var upPt = pt + vecExtend;

                var line = new Line(upPt, downPt);

                var midPt = IntersectMidPtVertical(srcPoly, line);

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
        private Point3d? IntersectMidPtVertical(Curve curveFir, Curve curveSec)
        {
            var ptLst = GeomUtils.CurveIntersectCurve(curveFir, curveSec);

            if (ptLst.Count != 0)
            {
                ptLst.Sort((p1, p2) => { return p1.Y.CompareTo(p2.Y); });
                return GeomUtils.GetMidPoint(ptLst.First(), ptLst.Last());
            }

            return null;
        }

        public Point3d? LeftEdgeIntersectHorizontal(Curve curveFir, Curve curveSec)
        {
            var ptLst = GeomUtils.CurveIntersectCurve(curveFir, curveSec);

            if (ptLst.Count != 0)
            {
                ptLst.Sort((p1, p2) => { return p1.X.CompareTo(p2.X); });
                return ptLst.Last();
            }

            return null;
        }

        public Point3d? RightEdgeIntersectHorizontal(Curve curveFir, Curve curveSec)
        {
            var ptLst = GeomUtils.CurveIntersectCurve(curveFir, curveSec);

            if (ptLst.Count != 0)
            {
                ptLst.Sort((p1, p2) => { return p1.X.CompareTo(p2.X); });
                return ptLst.First();
            }

            return null;
        }

        /// <summary>
        /// 多行处理
        /// </summary>
        /// <param name="placeRectInfo"></param>
        /// <param name="verticalCount"></param>
        /// <param name="verticalGap"></param>
        /// <returns></returns>
        protected virtual List<Point3d> MultiOneRowPlacePts(PlaceRect placeRectInfo, double verticalCount, double verticalGap)
        {
            var srcPostPoly = placeRectInfo.srcPolyline;

            var bottomLine = placeRectInfo.BottomLine;
            var oneRowPlaceRectInfos = new List<PlaceRect>();
            var ptLst = new List<Point3d>();
            for (int i = 0; i < verticalCount; i++)
            {
                var curBottomLine = GeomUtils.MoveLine(bottomLine, Vector3d.YAxis, i * verticalGap);
                var splitPoly = GenerateSplitPolyline(curBottomLine, Vector3d.YAxis, verticalGap);
                var intersectPoly = GenerateIntersectRegion(splitPoly, srcPostPoly);

                if (intersectPoly == null)
                    continue;

                DrawUtils.DrawProfile(new List<Curve>() { intersectPoly }, "intersectPoly");
                var placeRect = GeomUtils.CalculateProfileRectInfo(intersectPoly);
                //DrawUtils.DrawProfile(new List<Curve>() { splitPoly }, "splitPoly");
                placeRect.srcPolyline = intersectPoly;
                oneRowPlaceRectInfos.Add(placeRect);
            }

            for (int i = 0; i < oneRowPlaceRectInfos.Count; i++)
            {
                var curPlaceRect = oneRowPlaceRectInfos[i];
                var midPosGap = verticalGap * 0.5;
                var midLine = GeomUtils.MoveLine(curPlaceRect.BottomLine, Vector3d.YAxis, midPosGap);
                var oneRowPts = OneRowPlace(midLine, curPlaceRect, midPosGap);
                if (oneRowPts != null && oneRowPts.Count != 0)
                    ptLst.AddRange(oneRowPts);
            }

            return ptLst;
        }

        /// <summary>
        /// 相交多段线
        /// </summary>
        /// <param name="polyFir"></param>
        /// <param name="polySec"></param>
        /// <returns></returns>
        protected Polyline GenerateIntersectRegion(Polyline polyFir, Polyline polySec)
        {
            var polyLst = new List<Polyline>();
            foreach (DBObject singlePolygon in polyFir.GeometryIntersection(polySec))
            {
                if (singlePolygon is Polyline validPoly)
                    polyLst.Add(validPoly);
            }

            if (polyLst.Count == 0)
                return null;

            polyLst.Sort((p1, p2) =>
            {
                return p1.Area.CompareTo(p2.Area);
            });

            return polyLst.Last();
        }

        /// <summary>
        /// 分割图形
        /// </summary>
        /// <param name="bottomLine"></param>
        /// <param name="moveDir"></param>
        /// <param name="moveLength"></param>
        /// <returns></returns>
        protected Polyline GenerateSplitPolyline(Line bottomLine, Vector3d moveDir, double moveLength)
        {
            var ptLst = new List<Point3d>();
            var bottomStart = bottomLine.StartPoint;
            var bottomEnd = bottomLine.EndPoint;

            var vecExtend = moveDir * moveLength;

            var topS = bottomStart + vecExtend;
            var topE = bottomEnd + vecExtend;
            ptLst.Add(bottomStart);
            ptLst.Add(bottomEnd);
            ptLst.Add(topE);
            ptLst.Add(topS);

            var poly = new Polyline()
            {
                Closed = true
            };

            for (int i = 0; i < ptLst.Count; i++)
            {
                poly.AddVertexAt(i, ptLst[i].ToPoint2D(), 0, 0, 0);
            }

            var bufferPoly = GeomUtils.BufferPoly(poly);
            if (bufferPoly == null)
                return poly;
            return bufferPoly;
        }

        /// <summary>
        /// 计算布置点
        /// </summary>
        /// <param name="placeRectInfo"></param>
        protected virtual List<Point3d> CalculatePts(PlaceRect placeRectInfo)
        {
            if (placeRectInfo == null)
                return new List<Point3d>();

            // 原始的经过变化过的多段线
            var srcTransPoly = placeRectInfo.srcPolyline;

            var leftLine = placeRectInfo.LeftLine;
            var bottomLine = placeRectInfo.BottomLine;

            var rectArea = leftLine.Length * bottomLine.Length;

            // 一个可以布置完的
            if (leftLine.Length < 2 * m_parameter.ProtectRadius && bottomLine.Length < 2 * m_parameter.ProtectRadius && rectArea < m_parameter.ProtectArea)
            {
                return GeomUtils.CalculateCentroidFromPoly(srcTransPoly);
            }

            // 垂直个数
            var verticalCount = Math.Ceiling(leftLine.Length / m_parameter.VerticalMaxGap);
            var verticalGap = leftLine.Length / verticalCount;

            // 水平个数
            var horizontalCount = Math.Ceiling(bottomLine.Length / m_parameter.HorizontalMaxGap);

            // 布置一行的
            if (verticalCount == 1)
            {
                // 一行布置
                var midLine = GeomUtils.MoveLine(bottomLine, Vector3d.YAxis, leftLine.Length * 0.5);
                return OneRowPlace(midLine, placeRectInfo, leftLine.Length * 0.5);
            }
            else if (horizontalCount == 1)
            {
                // 一列布置
                return OneColMultiSegmentsPlaceEx.MakeOneColPlacePolygon(m_parameter, placeRectInfo);
            }
            else
            {
                // 多行布置 - 分行处理
                return MultiOneRowPlacePts(placeRectInfo, verticalCount, verticalGap);
            }
        }
    }
}
