using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPElectrical.Assistant;
using ThMEPElectrical.Geometry;
using ThMEPElectrical.Model;
using ThMEPEngineCore.Algorithm;

namespace ThMEPElectrical.Business
{
    public class BeamDetectionCalculatorEx : DetectionCalculator
    {
        private List<Polyline> m_innerProfiles = null; // 用于扣减 主梁，柱子，剪力墙
        private List<SecondBeamProfileInfo> m_secondBeamInfos = null; // 生成关系组， 次梁
        ThMEPOriginTransformer m_originTransformer;

        /// <summary>
        /// 计算主梁或者次梁的数据结构
        /// </summary>
        /// <param name="polyline"> 墙</param>
        /// <param name="polylines">内轮廓</param>
        /// <param name="secondBeams"> 次梁</param>
        public static List<PlaceInputProfileData> MakeDetectionDataEx(Polyline polyline, List<Polyline> polylines, List<SecondBeamProfileInfo> secondBeams, ThMEPOriginTransformer originTransformer)
        {
            var detection = new BeamDetectionCalculatorEx(polyline, polylines, secondBeams,originTransformer);
            detection.DoBeamSpan();
            return detection.RegionBeamSpanProfileData;
        }

        public BeamDetectionCalculatorEx(Polyline wallProfile, List<Polyline> innerHoles, List<SecondBeamProfileInfo> secondBeams = null, ThMEPOriginTransformer originTransformer=null)
            : base(wallProfile)
        {
            m_innerProfiles = innerHoles;
            m_secondBeamInfos = secondBeams;
            m_originTransformer = originTransformer;
        }

        
        /// <summary>
        /// 计算梁跨信息
        /// </summary>
        public void DoBeamSpan()
        {
            // 外墙边界轮廓扣减后的主梁构成的轮廓, 带有洞信息
            var mainBeamPolygons = CalculateMainBeamProfiles();

            //DrawDetectionRegion(mainBeamPolygons);
            //DrawSecondBeam2Curves(m_secondBeamInfos, "detectionSecondBeam");
            // 主次梁关系
            var detectRegion = CalculateDetectionPolygonRelations(mainBeamPolygons, m_secondBeamInfos);

            //DrawDetectionRegion(detectRegion);
            // 根据主次梁的高度信息进行区域的划分, 如次梁高度 >= 600mm的
            var divideDetectRegion = DivisionMainDetectionRegion(detectRegion);
            //DrawSecondBeam2Curves(divideDetectRegion.First().secondBeams, "divideDetectRegion");

            // 数据转换
            RegionBeamSpanProfileData = DetectRegion2ProfileData(divideDetectRegion);
            DrawUtils.DrawGroup(RegionBeamSpanProfileData,m_originTransformer);
        }

        /// <summary>
        /// 对次梁高度大于等于600的次梁 + 剪力墙+ 柱子等信息高度的进行再次划分
        /// 组合划分逻辑Extend
        /// </summary>
        /// <param name="srcDetectRegion"></param>
        /// <returns></returns>
        private List<DetectionRegion> DivisionMainDetectionRegion(List<DetectionRegion> srcDetectRegion)
        {
            var resMainDetectRegions = new List<DetectionRegion>();

            foreach (var singleDetectRegion in srcDetectRegion)
            {
                var resRegions = CalculateDivision(singleDetectRegion);
                resMainDetectRegions.AddRange(resRegions);
            }

            return resMainDetectRegions;
        }

        /// <summary>
        /// 计算分割
        /// 对次梁高度大于等于600的次梁 + 剪力墙+ 柱子等信息高度的进行再次划分
        /// 组合划分逻辑Extend
        /// </summary>
        /// <param name="detectRegion"></param>
        /// <returns></returns>
        private List<DetectionRegion> CalculateDivision(DetectionRegion detectRegion)
        {
            var resDetectRegions = new List<DetectionRegion>();
            var resRegions = Division(detectRegion);
            if (resRegions.Count != 0)
            {
                resDetectRegions.AddRange(resRegions);
            }
            return resDetectRegions;
        }

        /// <summary>
        /// 对当前一个数据进行，分别收集不再需要细分的数据和需要细分的数据
        /// 计算分割
        /// 对次梁高度大于等于600的次梁 + 剪力墙+ 柱子等信息高度的进行再次划分
        /// 组合划分逻辑Extend
        /// </summary>
        /// <param name="detectRegion"></param>
        /// <returns></returns>
        private List<DetectionRegion> Division(DetectionRegion detectRegion)
        {
            // 收集不再需要进行再次划分的探测区域
            var resDivisionRegions = new List<DetectionRegion>();
            if (detectRegion.secondBeams.Count == 0)
            {
                resDivisionRegions.Add(detectRegion);
                return resDivisionRegions;
            }

            var secondBeams = detectRegion.secondBeams;
            var externalPoly = detectRegion.DetectionProfile;
            

            // 收集分割轮廓信息, 源梁信息中删除原有的次梁信息，
            // 保证次梁信息中无可用于分割的次梁信息
            var splitPolys = SplitDetectionPolygonCalculator.MakeSplitDetectionPolygonCalculator(externalPoly, secondBeams, detectRegion.DetectionInnerProfiles);
            if (splitPolys.Count == 0)
            {
                resDivisionRegions.Add(detectRegion);
                return resDivisionRegions;
            }

            return CalculateDivideDetectionRegions(detectRegion, splitPolys);
        }


        private List<DetectionRegion> CalculateDivideDetectionRegions(DetectionRegion detectionRegion, List<Polyline> polylines)
        {
            var detectionPolygons = DivideDetectionPolygons(detectionRegion, polylines);
            //DrawUtils.DrawDetectionPolygon(detectionPolygons);
            var secondBeams = detectionRegion.secondBeams;

            //DrawUtils.DrawSecondBeam2Curves(secondBeams, "GroupSecondBeams");
            return GenerateRegionsWithGroup(detectionPolygons, secondBeams);
        }

        /// <summary>
        /// 单个变多个
        /// </summary>
        /// <param name="detectionPolygons"></param>
        /// <param name="secondBeamProfileInfos"></param>
        /// <returns></returns>
        private List<DetectionRegion> GenerateRegionsWithGroup(List<DetectionPolygon> detectionPolygons, List<SecondBeamProfileInfo> secondBeamProfileInfos)
        {
            var detectionRegions = new List<DetectionRegion>();

            foreach (var detectionPolygon in detectionPolygons)
            {
                var detectionRegion = new DetectionRegion()
                {
                    DetectionProfile = detectionPolygon.Shell
                };

                detectionRegion.DetectionInnerProfiles.AddRange(detectionPolygon.Holes);

                // 内部扣减区域
                foreach (var secondBeam in secondBeamProfileInfos)
                {
                    var secondBeamProfile = secondBeam.Profile;

                    if (IsIntersectOrContains(detectionPolygon.Shell, secondBeamProfile))
                    {
                        detectionRegion.secondBeams.Add(secondBeam);
                    }
                }

                detectionRegions.Add(detectionRegion);
            }

            return detectionRegions;
        }

        /// <summary>
        /// 扣减梁
        /// </summary>
        /// <param name="detectionPolygon"></param>
        /// <param name="polylines"></param>
        /// <returns></returns>
        private List<DetectionPolygon> DivideDetectionPolygons(DetectionRegion detectionRegion, List<Polyline> polylines)
        {
            var dbLst = new DBObjectCollection();
            polylines.ForEach(e => dbLst.Add(e));
            detectionRegion.DetectionInnerProfiles.ForEach(e => dbLst.Add(e));

            var detectionPolygons = SplitRegions(detectionRegion.DetectionProfile, dbLst);
            return detectionPolygons;
        }

        /// <summary>
        /// 计算主梁等构成的主探测区域
        /// </summary>
        /// <returns></returns>
        public List<DetectionPolygon> CalculateMainBeamProfiles()
        {
            var dbLst = new DBObjectCollection();
            foreach (var profile in m_innerProfiles)
            {
                dbLst.Add(profile);
            }

            var resDetectionPolygons = SplitRegions(m_wallProfile, dbLst);
            return resDetectionPolygons;
        }
    }
}
