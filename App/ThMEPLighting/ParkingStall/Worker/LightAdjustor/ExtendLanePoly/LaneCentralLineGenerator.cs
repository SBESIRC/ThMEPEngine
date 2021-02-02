using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPLighting.ParkingStall.Geometry;
using ThMEPLighting.ParkingStall.Model;

namespace ThMEPLighting.ParkingStall.Worker.LightAdjustor
{
    public class LaneCentralLineGenerator
    {
        private List<Polyline> m_lanePolys;
        private PolygonInfo m_polygonInfo;

        private double m_extendLength;

        public List<Polyline> ExtendPolylines
        {
            get;
            set;
        } = new List<Polyline>();

        private List<Polyline> m_noExtendPolys = new List<Polyline>();

        public LaneCentralLineGenerator(List<Polyline> polylines, PolygonInfo polygonInfo, double extendLength)
        {
            m_lanePolys = polylines;
            m_polygonInfo = polygonInfo;
            m_extendLength = extendLength;
        }


        public static List<Polyline> MakeLaneCentralPolys(List<Polyline> polylines, PolygonInfo polygonInfo, double extendLength)
        {
            var laneCentralLineGenerator = new LaneCentralLineGenerator(polylines, polygonInfo, extendLength);
            laneCentralLineGenerator.Do();
            return laneCentralLineGenerator.ExtendPolylines;
        }

        public void Do()
        {
            var pointEdgeInfos = NodeDegreeCalculator.MakeLanePolylineNodeDegree(m_lanePolys);
            var validPointEdgeInfos = CalculateValidEdgeInfos(pointEdgeInfos);

            // 生成延长信息
            CalculatePointEdgeInfo(validPointEdgeInfos);

            // 延长需要延长的边信息
            var extendPolys = ExtendValidPolylines(validPointEdgeInfos);

            // 修剪延长的车道线
            TrimExtendPolys(extendPolys);

            ExtendPolylines.AddRange(m_noExtendPolys);
        }


        private void TrimExtendPolys(List<Polyline> polylines)
        {
            var wallExternalPoly = m_polygonInfo.ExternalProfile;

            var wallHolePolys = m_polygonInfo.InnerProfiles;

            var firstStepTrimPolys = new List<Polyline>();
            foreach (var poly in polylines)
            {
                foreach (var entity in wallExternalPoly.Trim(poly))
                {
                    if (entity is Polyline polyline)
                        firstStepTrimPolys.Add(polyline);
                }
            }

            foreach (var hole in wallHolePolys)
            {
                firstStepTrimPolys = DiffHole(hole, firstStepTrimPolys);
            }

            //ExtendPolylines.AddRange(firstStepTrimPolys);
            foreach (var trimedPoly in firstStepTrimPolys)
            {
                if (IsInValidTrimedPoly(trimedPoly, wallHolePolys))
                    continue;

                ExtendPolylines.Add(trimedPoly);
            }
        }

        private bool IsInValidTrimedPoly(Polyline trimedPoly, List<Polyline> polylines)
        {
            foreach (var hole in polylines)
            {
                if (IsPolyinFromHole(trimedPoly, hole) && trimedPoly.Length < 1000)
                    return true;
            }

            return false;
        }

        private bool IsPolyinFromHole(Polyline polyline, Polyline hole)
        {
            var ptLst = new Point3dCollection();
            hole.IntersectWith(polyline, Intersect.OnBothOperands, ptLst, (IntPtr)0, (IntPtr)0);
            if (ptLst.Count > 0)
                return true;

            return false;
        }

        private List<Polyline> DiffHole(Polyline hole, List<Polyline> polylines)
        {
            var resPolys = new List<Polyline>();
            foreach (var poly in polylines)
            {
                foreach (Entity entity in hole.Trim(poly, true))
                {
                    if (entity is Polyline polyline)
                        resPolys.Add(polyline);
                }
            }

            return resPolys;
        }

        private void CalculatePointEdgeInfo(List<PointEdgeInfo> pointEdgeInfos)
        {
            foreach (var pointEdgeInfo in pointEdgeInfos)
            {
                var startPoint = pointEdgeInfo.Point;
                var lanePolys = pointEdgeInfo.Polylines;

                foreach (LanePolyline lanePoly in lanePolys)
                {
                    var poly = lanePoly.Poly;
                    var vec = poly.GetFirstDerivative(startPoint).Negate().GetNormal();

                    var endPt = startPoint + vec * m_extendLength;
                    lanePoly.ExtendLines.Add(new Line(startPoint, endPt));
                }
            }
        }

        private List<Polyline> ExtendValidPolylines(List<PointEdgeInfo> pointEdgeInfos)
        {
            var extendPolys = new List<Polyline>();
            foreach (var pointEdgeInfo in pointEdgeInfos)
            {
                foreach (var lanePoly in pointEdgeInfo.Polylines)
                {
                    if (lanePoly.IsUsed)
                        continue;

                    lanePoly.IsUsed = true;

                    var dbCol = new DBObjectCollection();
                    lanePoly.ExtendLines.ForEach(extendLine => dbCol.Add(extendLine));

                    dbCol.Add(lanePoly.Poly);

                    var symLanePoly = lanePoly.Sym;
                    if (!symLanePoly.IsUsed)
                    {
                        symLanePoly.ExtendLines.ForEach(extendLine => dbCol.Add(extendLine));
                        symLanePoly.IsUsed = true;
                    }

                    foreach (DBObject entity in dbCol.LineMerge())
                    {
                        if (entity is Polyline polyline)
                            extendPolys.Add(polyline);
                    }
                }
            }

            return extendPolys;
        }

        private List<PointEdgeInfo> CalculateValidEdgeInfos(List<PointEdgeInfo> pointEdgeInfos)
        {
            var resPointEdgeInfos = new List<PointEdgeInfo>();
            pointEdgeInfos.ForEach(pointEdgeInfo =>
            {
                if (pointEdgeInfo.Degree < 3)
                    resPointEdgeInfos.Add(pointEdgeInfo);
                else
                {
                    var outLanePolys = pointEdgeInfo.Polylines;

                    foreach (var outLanePoly in outLanePolys)
                    {
                        // 计算每一个出去的边，终点的度关系
                        var endPoint = outLanePoly.EndPoint;

                        if (IsInvalidNodePoint(endPoint, pointEdgeInfos))
                        {
                            // 两端都不延伸
                            if (IsNoSamePoly(outLanePoly.Poly))
                                m_noExtendPolys.Add(outLanePoly.Poly);
                        }
                    }
                }
            });

            return resPointEdgeInfos;
        }

        private bool IsNoSamePoly(Polyline extendPoly)
        {
            foreach (var poly in m_noExtendPolys)
            {
                var plStart = poly.StartPoint;
                var plEnd = poly.EndPoint;

                var extendStartPt = extendPoly.StartPoint;
                var extendEndPt = extendPoly.EndPoint;

                if ((GeomUtils.Point3dIsEqualPoint3d(plStart, extendStartPt) && GeomUtils.Point3dIsEqualPoint3d(plEnd, extendEndPt))
                    || (GeomUtils.Point3dIsEqualPoint3d(plStart, extendEndPt) && GeomUtils.Point3dIsEqualPoint3d(plEnd, extendStartPt)))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsInvalidNodePoint(Point3d point, List<PointEdgeInfo> pointEdgeInfos)
        {
            foreach (var pointEdgeInfo in pointEdgeInfos)
            {
                if (GeomUtils.Point3dIsEqualPoint3d(point, pointEdgeInfo.Point) && pointEdgeInfo.Degree > 2)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
