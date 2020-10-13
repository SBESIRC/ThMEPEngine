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
using ThCADCore.NTS;

namespace ThMEPElectrical.Business.MainBeam
{
    /// <summary>
    /// 一列异形布置
    /// </summary>
    class OneColMultiSegmentsPlaceEx
    {
        private PlaceParameter m_parameter;

        private PlaceRect m_placeRectInfo;

        public static List<Point3d> MakeOneColPlacePolygon(PlaceParameter parameter, PlaceRect placeRectInfo)
        {
            var oneColPlace = new OneColMultiSegmentsPlaceEx(parameter, placeRectInfo);
            return oneColPlace.DoPlace();
        }

        protected OneColMultiSegmentsPlaceEx(PlaceParameter parameter, PlaceRect placeRectInfo)
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

            var leftBottomFirstPt = tempLeftBottomFirstPtNode.Value;

            // 左上顶点
            var leftTopCircle = new Circle(leftTopPt, Vector3d.ZAxis, vertexProtectRadius);
            var tempLeftTopLastPtNode = CalculateIntersectPt(leftTopCircle, verticalMidLine);
            if (!tempLeftTopLastPtNode.HasValue)
                return ptLst;

            var leftTopLastPt = tempLeftTopLastPtNode.Value;

            var verticalMaxGap = m_parameter.ProtectArea / 4.0 / moveDis * 2;

            // 计算垂直间隔长度
            var verticalLength = (leftTopLastPt - leftBottomFirstPt).Length;
            var verticalCount = Math.Ceiling(verticalLength / verticalMaxGap);
            var verticalPosGap = verticalLength / verticalCount;

            ptLst.Add(leftBottomFirstPt);

            for (int i = 1; i < verticalCount; i++)
            {
                var moveGap = i * verticalPosGap;
                var tempMidPt = leftBottomFirstPt + Vector3d.YAxis * moveGap;
                ptLst.Add(tempMidPt);
            }

            ptLst.Add(leftTopLastPt);

            //// 是否需要进行后处理判断
            //if (IsNeedPostProcess(placeNodes))
            //{
            //    var validVerticalMidLine = GeomUtils.CalculateMidLine(verticalMidLine, m_placeRectInfo.srcPolyline);
            //    ptLst = PlacePointAdjustor.MakePlacePointAdjustor(ptLst, validVerticalMidLine, ShapeConstraintType.NONREGULARSHAPE);
            //}

            return CalculateCentroidPoint(ptLst, m_placeRectInfo);
        }

        /// <summary>
        /// 分成一个一个区域计算质心位置, 大于2
        /// </summary>
        /// <param name="srcPts"></param>
        /// <param name="placeRectInfo"></param>
        /// <returns></returns>
        private List<Point3d> CalculateCentroidPoint(List<Point3d> srcPts, PlaceRect placeRectInfo)
        {
            var pts = new List<Point3d>();
            var width = placeRectInfo.BottomLine.Length + 10;
            var midStartPoint = GeomUtils.GetMidPoint(placeRectInfo.LeftBottomPt, placeRectInfo.RightBottomPt);
            var midEndPoint = GeomUtils.GetMidPoint(placeRectInfo.LeftTopPt, placeRectInfo.RightTopPt);

            var polys = new List<Polyline>();
            Polyline regionPoly;
            Point3d nextPoint;
            Point3d beforePoint;
            for (int i = 0; i < srcPts.Count; i++)
            {
                var curPoint = srcPts[i];
                if (i == 0)
                {
                    // 起点
                    nextPoint = srcPts[i + 1];
                    regionPoly = CreateRegion(midStartPoint, GeomUtils.GetMidPoint(curPoint, nextPoint), width);
                }
                else if (i == srcPts.Count - 1)
                {
                    // 终点
                    beforePoint = srcPts[i - 1];
                    regionPoly = CreateRegion(GeomUtils.GetMidPoint(beforePoint, curPoint), midEndPoint, width);
                }
                else
                {
                    // 中间点数据
                    beforePoint = srcPts[i - 1];
                    nextPoint = srcPts[i + 1];
                    regionPoly = CreateRegion(GeomUtils.GetMidPoint(beforePoint, curPoint), GeomUtils.GetMidPoint(curPoint, nextPoint), width);
                }
                polys.Add(regionPoly);
            }

            var splitPolys = SplitRegions(polys, placeRectInfo.srcPolyline);
            foreach (var splitPoly in splitPolys)
            {
                pts.AddRange(GeomUtils.CalculateCentroidFromPoly(splitPoly));
            }
            
            return pts;
        }

        /// <summary>
        /// 分割区域生成
        /// </summary>
        /// <param name="srcPolys"></param>
        /// <param name="dividePoly"></param>
        /// <returns></returns>
        private List<Polyline> SplitRegions(List<Polyline> srcPolys, Polyline dividePoly)
        {
            var polys = new List<Polyline>();

            foreach (var poly in srcPolys)
            {
                var resPoly = GenerateIntersectRegion(poly, dividePoly);
                if (resPoly != null)

                polys.Add(resPoly);
            }

            return polys;
        }

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
        /// 生成矩形区域
        /// </summary>
        /// <param name="bottomMidPt"></param>
        /// <param name="topMidPt"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        private Polyline CreateRegion(Point3d bottomMidPt, Point3d topMidPt, double width)
        {
            var point3dCollection = new Point3dCollection();
            var vec = Vector3d.XAxis * width;
            var leftBottomPt = bottomMidPt - vec;
            var rightBottomPt = bottomMidPt + vec;
            var leftTopPt = topMidPt - vec;
            var rightTopPt = topMidPt + vec;
            point3dCollection.Add(leftBottomPt);
            point3dCollection.Add(rightBottomPt);
            point3dCollection.Add(rightTopPt);
            point3dCollection.Add(leftTopPt);
            return point3dCollection.ToPolyline();
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

