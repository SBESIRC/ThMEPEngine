using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.ParkingStall.Model;

namespace ThMEPLighting.ParkingStall.Worker.LightAdjustor
{
    public abstract class ParkGroupGapDivider
    {
        protected List<LightPlaceInfo> m_lightPlaceInfos;

        protected double m_gapDistance;

        public List<List<LightPlaceInfo>> LightPlaceGroupLst = new List<List<LightPlaceInfo>>();

        public ParkGroupGapDivider(List<LightPlaceInfo> lightPlaceInfos, double gapDistance)
        {
            m_lightPlaceInfos = lightPlaceInfos;
            m_gapDistance = gapDistance;
        }


        public virtual void Do()
        {

        }

        protected void PreProcess()
        {
            for (int i = 0; i < m_lightPlaceInfos.Count; i++)
            {
                var curLightPlaceInfo = m_lightPlaceInfos[i];
                curLightPlaceInfo.IsUsed = false;

                for (int j = i + 1; j < m_lightPlaceInfos.Count; j++)
                {
                    var nextLightPlaceInfo = m_lightPlaceInfos[j];

                    if (IsValidRelatedInfo(curLightPlaceInfo, nextLightPlaceInfo, m_gapDistance))
                    {
                        curLightPlaceInfo.RelatedLightPlaceInfos.Add(nextLightPlaceInfo);
                        nextLightPlaceInfo.RelatedLightPlaceInfos.Add(curLightPlaceInfo);
                    }
                }
            }
        }

        protected void GenerateGroup()
        {
            for (int i = 0; i < m_lightPlaceInfos.Count; i++)
            {
                var curLightPlaceInfo = m_lightPlaceInfos[i];
                if (curLightPlaceInfo.IsUsed)
                    continue;

                CollectFromOneNode(curLightPlaceInfo);
            }
        }

        protected void CollectFromOneNode(LightPlaceInfo relatedNode)
        {
            var relatedNodes = new List<LightPlaceInfo>();
            relatedNodes.Add(relatedNode);

            var collectNodes = new List<LightPlaceInfo>();

            while (relatedNodes.Count != 0)
            {
                var curNode = relatedNodes.First();
                relatedNodes.Remove(curNode);

                if (curNode.IsUsed)
                    continue;

                curNode.IsUsed = true;
                collectNodes.Add(curNode);

                foreach (var childNode in curNode.RelatedLightPlaceInfos)
                {
                    if (childNode.IsUsed)
                        continue;

                    relatedNodes.Add(childNode);
                }
            }

            LightPlaceGroupLst.Add(collectNodes);
        }

        protected virtual bool IsValidRelatedInfo(LightPlaceInfo firstLightpLaceInfo, LightPlaceInfo secondLightPlaceInfo, double gapDistance)
        {
            return false;
        }

        protected bool IsValidNearParkGroup(List<Line> firstGroupLines, List<Line> secondGroupLines, double distance)
        {
            foreach (var firstGroupLine in firstGroupLines)
            {
                foreach (var secondGroupLine in secondGroupLines)
                {
                    var point = firstGroupLine.GetClosestPointTo(secondGroupLine.StartPoint, true);
                    if (point.DistanceTo(secondGroupLine.StartPoint) <= distance)
                        return true;
                }
            }

            return false;
        }
    }
}
