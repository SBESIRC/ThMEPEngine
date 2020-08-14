using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Model;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPElectrical.Business
{
    // 计算探测区域
    public class CalculateDetection
    {
        private Polyline OuterProfile = null;
        private List<Polyline> InnerProfiles = null;
        private List<BeamProfile> SecondBeams = null;

        public static void MakeDetections(Polyline polyline, List<Polyline> polylines, List<BeamProfile> secondBeams)
        {
            var dectection = new CalculateDetection(polyline, polylines, secondBeams);

        }

        public CalculateDetection(Polyline wallProfile, List<Polyline> innerHoles, List<BeamProfile> secondBeams = null)
        {
            OuterProfile = wallProfile;
            InnerProfiles = innerHoles;
            SecondBeams = secondBeams;
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
                foreach (var secondBeam in SecondBeams)
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
            foreach (var profile in InnerProfiles)
            {
                dbLst.Add(profile);
            }

            var resProfiles = new List<Polyline>();
            foreach (Polyline item in OuterProfile.Difference(dbLst))
            {
                resProfiles.Add(item);
            }

            return resProfiles;
        }
    }
}
