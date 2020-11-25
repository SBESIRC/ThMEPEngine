using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Model;
using ThMEPElectrical.Geometry;
using ThMEPElectrical.Assistant;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPElectrical.Business
{
    /// <summary>
    /// 返回大于600且能用于扣减的有效梁的轮廓PL
    /// </summary>
    public class SplitDetectionPolygonCalculator
    {
        private readonly Polyline m_externalPoly;
        private List<SecondBeamProfileInfo> m_secondBeams; // 信息维护

        private List<Polyline> m_holes;

        private List<SplitBeamPath> m_splitBeamPaths = new List<SplitBeamPath>();
        private HashSet<int> m_HasOrders = new HashSet<int>();
        private List<SecondBeamProfileInfo> m_validSplitBeams = new List<SecondBeamProfileInfo>();


        public List<Polyline> ValidBeamPolys
        {
            get;
            set;
        } = new List<Polyline>();


        public static List<Polyline> MakeSplitDetectionPolygonCalculator(Polyline externalPoly, List<SecondBeamProfileInfo> secondBeamProfiles, List<Polyline> holes)
        {
            var splitDetectionPolyCalculator = new SplitDetectionPolygonCalculator(externalPoly, secondBeamProfiles, holes);
            splitDetectionPolyCalculator.Do();
            return splitDetectionPolyCalculator.ValidBeamPolys;
        }

        public SplitDetectionPolygonCalculator(Polyline externalPoly, List<SecondBeamProfileInfo> secondBeamProfileInfos, List<Polyline> holes)
        {
            m_externalPoly = externalPoly;
            m_secondBeams = secondBeamProfileInfos;
            m_holes = holes;
        }

        public void Do()
        {
            // 洞口数据转化
            HoleAddToSeconBeamInfo();

            // 编号
            NumberSecondBeamInfos();

            // 计算相关信息
            CalculateRelatedBeamInfos();
            //var drawGroups = DrawTransformInputProfileDatas(m_secondBeams);
            //DrawUtils.DrawGroup(drawGroups);
            // 潜在的梁轮廓起点
            var possibleStartBeamProfiles = CalculatePossibleStartBeams();

            //var drawGroups = DrawTransformInputProfileDatas(possibleStartBeamProfiles);
            //DrawUtils.DrawGroup(drawGroups);

            //DrawUtils.DrawSecondBeam2Curves(possibleStartBeamProfiles, "splitSecondBeamProfiles");
            // 收集有效的梁
            CollectFromStartBeamInfos(possibleStartBeamProfiles);
            CollectValidBeamNodes();
            CollectValidPolys();
            //DrawUtils.DrawSecondBeam2Curves(m_validSplitBeams, "m_validSplitBeams");
            //DrawUtils.DrawGroupPath(m_splitBeamPaths);
            // 信息维护
            EraseInvalidBeamInfos();
        }

        private void CollectValidPolys()
        {
            foreach (var validBeamInfo in m_validSplitBeams)
            {
                if (validBeamInfo.IsHolePoly)
                    continue;

                ValidBeamPolys.Add(validBeamInfo.Profile);
            }
        }

        /// <summary>
        /// 收集有效的分割梁
        /// </summary>
        private void CollectValidBeamNodes()
        {
            foreach (var splitPath in m_splitBeamPaths)
            {
                FixSingleSplitPath(splitPath);
            }
        }

        private void FixSingleSplitPath(SplitBeamPath splitPath)
        {
            var splitPathNodes = splitPath.pathNodes;

            bool bStart = false;
            for (int i = splitPathNodes.Count - 1; i >= 0; i--)
            {
                var curReverseNode = splitPathNodes[i];
                if (curReverseNode.IsHolePoly)
                    continue;

                if (!bStart && i == 0)
                {
                    var ptLst = new Point3dCollection();
                    curReverseNode.Profile.IntersectWith(m_externalPoly, Intersect.OnBothOperands, ptLst, (IntPtr)0, (IntPtr)0);
                    if (ptLst.Count != 0)
                    {
                        if (IsValidPoints(ptLst.ToPointList()))
                        {
                            if (!m_HasOrders.Contains(curReverseNode.OrderNum))
                            {
                                m_HasOrders.Add(curReverseNode.OrderNum);
                                m_validSplitBeams.Add(curReverseNode);
                            }
                        }
                    }

                    return;
                }

                if (!bStart && GeomUtils.IsIntersect(curReverseNode.Profile, m_externalPoly))
                {
                    bStart = true;
                }

                if (bStart)
                {
                    if (!m_HasOrders.Contains(curReverseNode.OrderNum))
                    {
                        m_HasOrders.Add(curReverseNode.OrderNum);
                        m_validSplitBeams.Add(curReverseNode);
                    }
                }
            }

        }

        private bool IsValidPoints(List<Point3d> ptLst)
        {
            for (int i = 0; i < ptLst.Count; i++)
            {
                var curPt = ptLst[i];
                for (int j = i + 1; j < ptLst.Count; j++)
                {
                    var nextPt = ptLst[j];
                    if (curPt.DistanceTo(nextPt) > ThMEPCommon.ValidBeamLength)
                        return true;
                }
            }

            return false;
        }

        private List<PlaceInputProfileData> DrawTransformInputProfileDatas(List<SecondBeamProfileInfo> secondBeamProfileInfos)
        {
            var placeInputProfileDatas = new List<PlaceInputProfileData>();

            secondBeamProfileInfos.ForEach(e =>
            {
                var relatedPolys = new List<Polyline>();
                e.RelatedSecondBeams.ForEach(r => relatedPolys.Add(r.Profile));
                placeInputProfileDatas.Add(new PlaceInputProfileData(e.Profile, relatedPolys));
            });
            return placeInputProfileDatas;
        }

        public void GroupBeamInfos()
        {

        }

        /// <summary>
        /// 计算相关信息
        /// </summary>
        private void CalculateRelatedBeamInfos()
        {
            for (int i = 0; i < m_secondBeams.Count; i++)
            {
                var curSecondBeamInfo = m_secondBeams[i];
                if (!curSecondBeamInfo.IsHolePoly && curSecondBeamInfo.Height < ThMEPCommon.SecondBeamDivideHeight)
                    continue;

                for (int j = 0; j < m_secondBeams.Count; j++)
                {
                    if (i == j)
                        continue;

                    var nextSecondBeamInfo = m_secondBeams[j];
                    // 不是洞， 且高度小于分割高度
                    if (!nextSecondBeamInfo.IsHolePoly && nextSecondBeamInfo.Height < ThMEPCommon.SecondBeamDivideHeight)
                        continue;

                    if (GeomUtils.IsIntersect(curSecondBeamInfo.Profile, nextSecondBeamInfo.Profile))
                    {
                        curSecondBeamInfo.RelatedSecondBeams.Add(nextSecondBeamInfo);
                    }
                }
            }
        }

        private void EraseInvalidBeamInfos()
        {
            m_secondBeams.RemoveAll(beam =>
            {
                if (beam.IsHolePoly || m_HasOrders.Contains(beam.OrderNum))
                    return true;
                return false;
            });

            foreach (var secondBeam in m_secondBeams)
            {
                secondBeam.RelatedSecondBeams.Clear();
            }
        }

        private void CollectFromStartBeamInfos(List<SecondBeamProfileInfo> secondBeamProfileInfos)
        {
            foreach (var secondBeamProfileInfo in secondBeamProfileInfos)
            {
                SearchFromOneStartBeamInfo(secondBeamProfileInfo);
            }
        }

        private void SearchFromOneStartBeamInfo(SecondBeamProfileInfo secondBeamProfileInfo)
        {
            var splitPath = new SplitBeamPath();
            Search2(secondBeamProfileInfo, splitPath);
        }

        private void Search2(SecondBeamProfileInfo secondBeamProfile, SplitBeamPath splitPath)
        {
            if (ValidChildCountNum(secondBeamProfile, splitPath) == 0)
            {
                splitPath.Add(secondBeamProfile);
                m_splitBeamPaths.Add(splitPath);
                return;
            }

            if (splitPath.pathNums.Contains(secondBeamProfile.OrderNum))
                return;
            splitPath.Add(secondBeamProfile);

            foreach (var childBeam in secondBeamProfile.RelatedSecondBeams)
            {
                if (splitPath.pathNums.Contains(childBeam.OrderNum))
                    continue;
                var splitBeamPath = new SplitBeamPath(splitPath);
                Search2(childBeam, splitBeamPath);
            }
        }

        private void Search(SecondBeamProfileInfo secondBeamProfile, SplitBeamPath splitPath)
        {
            if (ValidChildCountNum(secondBeamProfile, splitPath) == 0)
            {
                if (GeomUtils.IsIntersect(m_externalPoly, secondBeamProfile.Profile))
                {
                    // 单根分割梁
                    if (splitPath.pathNodes.Count == 0)
                    {
                        m_splitBeamPaths.Add(splitPath);
                        return;
                    }
                    else
                    {
                        // 非单根分割情形
                        if (splitPath.pathNums.Contains(secondBeamProfile.OrderNum))
                            return;
                        splitPath.Add(secondBeamProfile);
                        m_splitBeamPaths.Add(splitPath);
                    }
                }
                return;
            }

            if (splitPath.pathNums.Contains(secondBeamProfile.OrderNum))
                return;
            splitPath.Add(secondBeamProfile);
            if (GeomUtils.IsIntersect(m_externalPoly, secondBeamProfile.Profile))
            {
                if (splitPath.pathNodes.Count > 1)
                {
                    var validPath = new SplitBeamPath(splitPath);
                    m_splitBeamPaths.Add(validPath);
                }
            }

            foreach (var childBeam in secondBeamProfile.RelatedSecondBeams)
            {
                var splitBeamPath = new SplitBeamPath(splitPath);
                Search(childBeam, splitBeamPath);
            }
        }

        private int ValidChildCountNum(SecondBeamProfileInfo secondBeamProfile, SplitBeamPath splitBeamPath)
        {
            var nSrcCount = secondBeamProfile.RelatedSecondBeams.Count;

            for (int i = 0; i < nSrcCount; i++)
            {
                var curRelatedSecondBeam = secondBeamProfile.RelatedSecondBeams[i];
                if (splitBeamPath.pathNums.Contains(curRelatedSecondBeam.OrderNum))
                    nSrcCount--;
            }

            return nSrcCount;
        }

        /// <summary>
        /// 编号
        /// </summary>
        private void NumberSecondBeamInfos()
        {
            int nNum = 0;
            foreach (var secondBeam in m_secondBeams)
            {
                nNum++;
                secondBeam.OrderNum = nNum;
            }
        }

        private void HoleAddToSeconBeamInfo()
        {
            foreach (var hole in m_holes)
            {
                var holeBeamInfo = new SecondBeamProfileInfo(hole);
                holeBeamInfo.IsHolePoly = true;
                holeBeamInfo.Height = -100;
                m_secondBeams.Add(holeBeamInfo);
            }
        }

        private List<SecondBeamProfileInfo> CalculatePossibleStartBeams()
        {
            var resSeconBeamProfileInfos = new List<SecondBeamProfileInfo>();
            foreach (var secondBeam in m_secondBeams)
            {
                if (secondBeam.Height >= ThMEPCommon.SecondBeamDivideHeight)
                {
                    if (GeomUtils.IsIntersect(m_externalPoly, secondBeam.Profile))
                        resSeconBeamProfileInfos.Add(secondBeam);
                }
            }

            return resSeconBeamProfileInfos;
        }

    }
}
