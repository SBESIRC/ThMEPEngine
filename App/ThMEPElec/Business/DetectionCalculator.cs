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
    public class DetectionCalculator
    {
        private Polyline m_wallProfile = null;
        private List<Polyline> m_innerProfiles = null;
        private List<BeamProfile> m_secondBeams = null;

        /// <summary>
        /// 梁跨数据信息
        /// </summary>
        public List<PlaceInputProfileData> BeamSpanProfileData
        {
            get;
            set;
        }

        /// <summary>
        /// 计算主梁或者次梁的数据结构
        /// </summary>
        /// <param name="polyline"> 墙</param>
        /// <param name="polylines">内轮廓</param>
        /// <param name="secondBeams"> 次梁</param>
        public static List<PlaceInputProfileData> MakeDetectionData(Polyline polyline, List<Polyline> polylines, List<BeamProfile> secondBeams)
        {
            var detection = new DetectionCalculator(polyline, polylines, secondBeams);
            detection.DoBeamSpan();
            return detection.BeamSpanProfileData;
        }

        public DetectionCalculator(Polyline wallProfile, List<Polyline> innerHoles, List<BeamProfile> secondBeams = null)
        {
            m_wallProfile = wallProfile;
            m_innerProfiles = innerHoles;
            m_secondBeams = secondBeams;
        }

        /// <summary>
        /// 计算梁跨信息
        /// </summary>
        public void DoBeamSpan()
        {
            //主梁
            var mainBeamProfiles = CalculateMainBeamProfiles();

            // 主次梁关系
            var detectRegion = CalculateDetectionRelations(mainBeamProfiles);

            // 数据转换
            BeamSpanProfileData = DetectRegion2ProfileData(detectRegion);
            DrawUtils.DrawGroup(BeamSpanProfileData);
        }

        /// <summary>
        /// 数据转换
        /// </summary>
        /// <param name="srcRegions"></param>
        /// <returns></returns>
        private List<PlaceInputProfileData> DetectRegion2ProfileData(List<DetectionRegion> srcRegions)
        {
            var inputProfileDatas = new List<PlaceInputProfileData>();
            srcRegions.ForEach(e => inputProfileDatas.Add(new PlaceInputProfileData(e.DetectionProfile, BeamProfiles2Polylines(e.secondBeams))));
            return inputProfileDatas;
        }

        private List<Polyline> BeamProfiles2Polylines(List<BeamProfile> srcPolylines)
        {
            var polys = new List<Polyline>();
            srcPolylines.ForEach(e => polys.Add(e.Profile));
            return polys;
        }

        /// <summary>
        /// 轮廓预处理筛选
        /// </summary>
        /// <param name="wallPoly"></param>
        /// <param name="srcPolylines"></param>
        /// <returns></returns>
        public List<Polyline> PreProcessProfiles(Polyline wallPoly, List<Polyline> srcPolylines)
        {
            var resPolys = new List<Polyline>();
            return resPolys;
        }

        public void Do()
        {
            var mainBeamProfiles = CalculateMainBeamProfiles();

            var detectRegion = CalculateDetectionRelations(mainBeamProfiles);

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

                var resRegion = Division(singleDetectRegion, ref calculateMidRegions);

                if (resRegion != null)
                {
                    resDetectRegions.Add(resRegion);
                }
            }

            return resDetectRegions;
        }

        /// <summary>
        /// 细分
        /// </summary>
        /// <param name="detectRegion"></param>
        /// <returns></returns>
        private DetectionRegion Division(DetectionRegion detectRegion, ref List<DetectionRegion> srcDetectRegion)
        {
            if (detectRegion.secondBeams.Count == 0)
                return detectRegion;

            var resDivisionRegions = new List<DetectionRegion>();
            var secondBeams = detectRegion.secondBeams;

            var validBeam = GetValidBeamProfile(secondBeams);


            return detectRegion;
        }

        private List<DetectionRegion> Division1(DetectionRegion detectRegion)
        {
            if (detectRegion.secondBeams.Count == 0)
                return new List<DetectionRegion>() { detectRegion };

            var resDivisionRegions = new List<DetectionRegion>();
            var secondBeams = detectRegion.secondBeams;

            var validBeam = GetValidBeamProfile(secondBeams);

            if (validBeam == null)
            {
                return new List<DetectionRegion>() { detectRegion };
            }
            else
            {

            }

            return resDivisionRegions;
        }

        /// <summary>
        /// 找到第一个大于600高度的次梁， 没有返回空
        /// </summary>
        /// <param name="srcBeamProfiles"></param>
        /// <returns></returns>
        private BeamProfile GetValidBeamProfile(List<BeamProfile> srcBeamProfiles)
        {
            foreach (var singleBeamProfile in srcBeamProfiles)
            {
                if (singleBeamProfile.Height > 600)
                {
                    srcBeamProfiles.Remove(singleBeamProfile);
                    return singleBeamProfile;
                }
            }

            return null;
        }

        public List<DetectionRegion> CalculateDetectionRelations(List<Polyline> profiles)
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

                // 次梁
                foreach (var secondBeam in m_secondBeams)
                {
                    var secondBeamProfile = secondBeam.Profile;

                    if (IsIntersect(profile, secondBeamProfile))
                    {
                        detectRegion.secondBeams.Add(secondBeam);
                    }
                }
            }

            return detectRegions;
        }

        private bool IsIntersect(Polyline firstPly, Polyline secPly)
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

        private bool IsIntersectValid(Polyline firstPly, Polyline secPly)
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
