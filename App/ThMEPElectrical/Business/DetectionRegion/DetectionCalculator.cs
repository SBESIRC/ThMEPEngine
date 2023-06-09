﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPElectrical.Assistant;
using ThMEPElectrical.Geometry;
using ThMEPElectrical.Model;

namespace ThMEPElectrical.Business
{
    public abstract class DetectionCalculator
    {
        protected Polyline m_wallProfile = null; // 墙轮廓

        /// <summary>
        ///  探测区域和梁的关系组
        /// </summary>
        public List<PlaceInputProfileData> RegionBeamSpanProfileData
        {
            get;
            set;
        }

        public DetectionCalculator(Polyline wallPoly)
        {
            m_wallProfile = wallPoly;
        }

        /// <summary>
        /// 计算区域和内部数据的关系组, 探测区域带有洞信息
        /// </summary>
        /// <param name="profiles"></param>
        /// <returns></returns>
        protected virtual List<DetectionRegion> CalculateDetectionPolygonRelations(List<DetectionPolygon> detectionPolygons, List<SecondBeamProfileInfo> secondBeamInfos)
        {
            var detectRegions = new List<DetectionRegion>();

            // 探测区域
            foreach (var polygon in detectionPolygons)
            {
                var detectRegion = new DetectionRegion()
                {
                    DetectionProfile = polygon.Shell,
                    DetectionInnerProfiles = polygon.Holes
                };

                detectRegions.Add(detectRegion);

                // 带有洞的区域的组合关系
                foreach (var secondBeam in secondBeamInfos)
                {
                    var secondBeamProfile = secondBeam.Profile;

                    if (IsIntersectOrContains(polygon.Shell, secondBeamProfile))
                    {
                        secondBeam.IsUsed = true;
                        detectRegion.secondBeams.Add(secondBeam);
                    }
                }
            }

            return detectRegions;
        }

        protected List<DetectionPolygon> SplitRegions(Polyline externalPolyline, DBObjectCollection dbLst)
        {
            var detectionPolygons = new List<DetectionPolygon>();
            var drawCurves = new List<Entity>();

            foreach (Entity item in externalPolyline.DifferenceMP(dbLst))
            {
                if (item is Polyline polyline)
                {
                    if (polyline.Area < 100) 
                    {
                        continue;
                    }
                    detectionPolygons.Add(new DetectionPolygon(polyline));
                    drawCurves.Add(polyline);
                }
                else if (item is MPolygon mPolygon)
                {
                    if (mPolygon.Area < 100) 
                    {
                        continue;
                    }
                    detectionPolygons.Add(GeomUtils.MPolygon2PolygonInfo(mPolygon));
                    drawCurves.Add(mPolygon);
                }
            }

            //DrawUtils.DrawEntitiesDebug(drawCurves, "entities");
            return detectionPolygons;
        }

        protected void CalculateDetectionRegionWithHoles(List<DetectionRegion> detectionRegions, List<Polyline> polylines)
        {
            var dbLst = new DBObjectCollection();
            polylines.ForEach(p => dbLst.Add(p));

            foreach (var singleDetectRegion in detectionRegions)
            {
                var detectionPolygons = SplitRegions(singleDetectRegion.DetectionProfile, dbLst);
                var holes = GetHoles(detectionPolygons);
                singleDetectRegion.DetectionInnerProfiles.AddRange(holes);
            }
        }

        protected List<Polyline> GetHoles(List<DetectionPolygon> detectionPolygons)
        {
            var polys = new List<Polyline>();
            foreach (var polygon in detectionPolygons)
            {
                polys.AddRange(polygon.Holes);
            }
            return polys;
        }

        /// <summary>
        /// 计算区域和内部数据的关系组
        /// </summary>
        /// <param name="profiles"></param>
        /// <returns></returns>
        protected virtual List<DetectionRegion> CalculateDetectionRelations(List<Polyline> profiles, List<SecondBeamProfileInfo> secondBeamInfos)
        {
            var detectRegions = new List<DetectionRegion>();

            // 探测区域
            foreach (var profile in profiles)
            {
                var detectRegion = new DetectionRegion()
                {
                    DetectionProfile = profile
                };

                detectRegions.Add(detectRegion);

                // 内部扣减区域
                foreach (var secondBeam in secondBeamInfos)
                {
                    var secondBeamProfile = secondBeam.Profile;

                    if (IsIntersectOrContains(profile, secondBeamProfile))
                    {
                        secondBeam.IsUsed = true;
                        detectRegion.secondBeams.Add(secondBeam);
                    }
                }
            }

            return detectRegions;
        }

        protected bool IsIntersectOrContains(Polyline firPoly, Polyline secPoly)
        {
            var ptLst = secPoly.Polyline2Point2d();
            foreach (var pt in ptLst)
            {
                if (GeomUtils.PtInLoop(firPoly, pt.ToPoint3d()))
                    return true;
            }

            if (firPoly.Intersects(secPoly))
                return true;

            return false;
        }

        /// <summary>
        /// 包含的不算
        /// </summary>
        /// <param name="firstPly"></param>
        /// <param name="secPly"></param>
        /// <returns></returns>
        protected bool IsIntersect(Polyline firstPly, Polyline secPly)
        {
            if (IsIntersectValid(firstPly, secPly))
            {
                var ptLst = new Point3dCollection();
                firstPly.IntersectWith(secPly, Intersect.OnBothOperands, ptLst, (IntPtr)0, (IntPtr)0);
                if (ptLst.Count != 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 数据转换
        /// </summary>
        /// <param name="srcRegions"></param>
        /// <returns></returns>
        protected List<PlaceInputProfileData> DetectRegion2ProfileData(List<DetectionRegion> srcRegions)
        {
            var inputProfileDatas = new List<PlaceInputProfileData>();
            srcRegions.ForEach(e => inputProfileDatas.Add(new PlaceInputProfileData(e.DetectionProfile, BeamProfiles2Polylines(e.secondBeams), e.DetectionInnerProfiles)));
            return inputProfileDatas;
        }

        /// <summary>
        /// 次梁信息提取次梁轮廓
        /// </summary>
        /// <param name="srcPolylines"></param>
        /// <returns></returns>
        private List<Polyline> BeamProfiles2Polylines(List<SecondBeamProfileInfo> srcPolylines)
        {
            var polys = new List<Polyline>();
            srcPolylines.ForEach(e => polys.Add(e.Profile));
            return polys;
        }

        protected bool IsIntersectValid(Polyline firstPly, Polyline secPly)
        {
            // first
            var firstExtend3d = firstPly.Bounds.Value;
            var firMinPt = firstExtend3d.MinPoint;
            var firMaxPt = firstExtend3d.MaxPoint;
            double firLeftX = firMinPt.X;
            double firLeftY = firMinPt.Y;
            double firRightX = firMaxPt.X;
            double firRightY = firMaxPt.Y;

            //second
            var secExtend3d = secPly.Bounds.Value;
            var secMinPt = secExtend3d.MinPoint;
            var secMaxPt = secExtend3d.MaxPoint;
            double secLeftX = secMinPt.X;
            double secLeftY = secMinPt.Y;
            double secRightX = secMaxPt.X;
            double secRightY = secMaxPt.Y;

            firLeftX -= 0.1;
            firLeftY -= 0.1;
            firRightX += 0.1;
            firRightY += 0.1;

            secLeftX -= 0.1;
            secLeftY -= 0.1;
            secRightX += 0.1;
            secRightY += 0.1;

            if (Math.Min(firRightX, secRightX) >= Math.Max(firLeftX, secLeftX)
                && Math.Min(firRightY, secRightY) >= Math.Max(firLeftY, secLeftY))
                return true;

            return false;
        }

        protected List<Polyline> CalculateRegions(List<Polyline> polylines)
        {
            var bufferPls = new DBObjectCollection();
            foreach (var singlePl in polylines)
            {
               foreach (var entity in singlePl.BufferPL(ThMEPCommon.PLbufferLength))
                {
                    if (entity is Polyline poly && poly.Closed)
                    {
                        bufferPls.Add(poly);
                    }
                }
            }

            var resPolys = new List<Polyline>();
            foreach (Entity entity in m_wallProfile.Difference(bufferPls))
            {
                if (entity is Polyline poly && poly.Closed)
                {
                    resPolys.Add(poly);
                }
            }

            return resPolys;
        }

        /// <summary>
        /// 计算轴网线 + 墙线构成的第一次区域
        /// </summary>
        protected List<Polyline> CalculateRegions2(List<Polyline> srcRegionProfiles)
        {
            var objs = srcRegionProfiles.ToCollection();
            var obLst = objs.Polygons();

            var polyLst = new List<Polyline>();
            foreach (Curve singleObj in obLst)
            {
                if (singleObj is Polyline poly && poly.Closed)
                {
                    foreach (DBObject singlePolygon in poly.GeometryIntersection(m_wallProfile))
                    {
                        if (singlePolygon is Polyline validPoly && validPoly.Closed)
                            polyLst.Add(validPoly);
                    }
                }
            }

            return polyLst;
        }
    }
}
