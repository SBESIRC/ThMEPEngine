using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Model;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using ThMEPElectrical.Assistant;

namespace ThMEPElectrical.Business
{
    // 计算探测区域
    public class BeamDetectionCalculator : DetectionCalculator
    {
        private List<Polyline> m_innerProfiles = null; // 用于扣减
        private List<SecondBeamProfileInfo> m_secondBeamInfos = null; // 生成关系组


        /// <summary>
        /// 计算主梁或者次梁的数据结构
        /// </summary>
        /// <param name="polyline"> 墙</param>
        /// <param name="polylines">内轮廓</param>
        /// <param name="secondBeams"> 次梁</param>
        public static List<PlaceInputProfileData> MakeDetectionData(Polyline polyline, List<Polyline> polylines, List<SecondBeamProfileInfo> secondBeams)
        {
            var detection = new BeamDetectionCalculator(polyline, polylines, secondBeams);
            detection.DoBeamSpan();
            return detection.RegionBeamSpanProfileData;
        }

        public BeamDetectionCalculator(Polyline wallProfile, List<Polyline> innerHoles, List<SecondBeamProfileInfo> secondBeams = null)
            : base(wallProfile)
        {
            m_innerProfiles = innerHoles;
            m_secondBeamInfos = secondBeams;
        }
        
        /// <summary>
        /// 计算梁跨信息
        /// </summary>
        public void DoBeamSpan()
        {
            // 外墙边界轮廓扣减后的主梁构成的轮廓
            var mainBeamProfiles = CalculateMainBeamProfiles();

            // 主次梁关系
            var detectRegion = CalculateDetectionRelations(mainBeamProfiles, m_secondBeamInfos);

            // 根据主次梁的高度信息进行区域的划分, 如次梁高度 > 600mm的
            var divideDetectRegion = DivisionMainDetectionRegion(detectRegion);

            // 数据转换
            RegionBeamSpanProfileData = DetectRegion2ProfileData(divideDetectRegion);
            //DrawUtils.DrawGroup(RegionBeamSpanProfileData);
        }

        /// <summary>
        /// 对次梁高度大于600高度的进行再次划分
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
        /// </summary>
        /// <param name="detectRegion"></param>
        /// <returns></returns>
        private List<DetectionRegion> CalculateDivision(DetectionRegion detectRegion)
        {
            var resDetectRegions = new List<DetectionRegion>();

            var calculateMidRegions = new List<DetectionRegion>();
            calculateMidRegions.Add(detectRegion);

            while (calculateMidRegions.Count > 0)
            {
                var singleDetectRegion = calculateMidRegions.First();
                calculateMidRegions.RemoveAt(0);

                var resRegions = Division(singleDetectRegion, ref calculateMidRegions);

                if (resRegions.Count != 0)
                {
                    resDetectRegions.AddRange(resRegions);
                }
            }

            return resDetectRegions;
        }

        /// <summary>
        /// 对当前一个数据进行，分别收集不再需要细分的数据和需要细分的数据
        /// </summary>
        /// <param name="detectRegion"></param>
        /// <returns></returns>
        private List<DetectionRegion> Division(DetectionRegion detectRegion, ref List<DetectionRegion> srcDetectRegions)
        {
            // 收集不再需要进行再次划分的探测区域
            var resDivisionRegions = new List<DetectionRegion>();
            if (detectRegion.secondBeams.Count == 0)
            {
                resDivisionRegions.Add(detectRegion);
                return resDivisionRegions;
            }

            var secondBeams = detectRegion.secondBeams;
            var mainBeamPoly = detectRegion.DetectionProfile;
            var greaterHeightBeamInfo = GetValidBeamProfile(secondBeams);

            if (greaterHeightBeamInfo == null)
            {
                resDivisionRegions.Add(detectRegion);
            }
            else
            {
                // 划分
                var resPolys = DividePolylines(mainBeamPoly, greaterHeightBeamInfo.Profile);
                var divideRegions = CalculateDetectionRelations(resPolys, secondBeams);
                foreach (var divideRegion in divideRegions)
                {
                    if (IsNeedDivide(divideRegion))
                        srcDetectRegions.Add(divideRegion);
                    else
                        resDivisionRegions.Add(divideRegion);
                }
            }

            return resDivisionRegions;
        }

        private bool IsNeedDivide(DetectionRegion detectRegion)
        {
            var secondBeamInfos = detectRegion.secondBeams;
            if (secondBeamInfos.Count == 0)
                return false;

            foreach (var singleBeamProfile in secondBeamInfos)
            {
                if (singleBeamProfile.Height >= ThMEPCommon.SecondBeamDivideHeight)
                {
                    return true;
                }
            }

            return false;
        }


        private List<Polyline> DividePolylines(Polyline poly, Polyline innerPoly)
        {
            var dbLst = new DBObjectCollection();
            dbLst.Add(innerPoly);

            var resProfiles = new List<Polyline>();
            foreach (Polyline item in poly.Difference(dbLst))
            {
                resProfiles.Add(item);
            }

            return resProfiles;
        }

        /// <summary>
        /// 找到第一个大于600高度的次梁， 没有返回空， 从原有的集合中删除已经找到的梁信息
        /// </summary>
        /// <param name="srcBeamProfiles"></param>
        /// <returns></returns>
        private SecondBeamProfileInfo GetValidBeamProfile(List<SecondBeamProfileInfo> srcBeamProfiles)
        {
            foreach (var singleBeamProfile in srcBeamProfiles)
            {
                if (singleBeamProfile.Height >= ThMEPCommon.SecondBeamDivideHeight)
                {
                    srcBeamProfiles.Remove(singleBeamProfile);
                    return singleBeamProfile;
                }
            }

            return null;
        }

        /// <summary>
        /// 计算主梁等构成的主探测区域
        /// </summary>
        /// <returns></returns>
        public List<Polyline> CalculateMainBeamProfiles()
        {
            var dbLst = new DBObjectCollection();
            foreach (var profile in m_innerProfiles)
            {
                dbLst.Add(profile);
            }

            var resProfiles = new List<Polyline>();
            foreach (Polyline item in m_wallProfile.Difference(dbLst))
            {
                resProfiles.Add(item);
            }

            return resProfiles;
        }
    }
}
