using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.ParkingStall.Geometry;
using ThMEPLighting.ParkingStall.Model;

namespace ThMEPLighting.ParkingStall.Worker.LightAdjustor
{
    public class NearLightPlaceCalculator
    {
        class LightPlaceNode
        {
            public LightPlaceInfo LightPlace;

            public bool IsUse = false;

            public LightPlaceNode(LightPlaceInfo lightPlaceInfo)
            {
                LightPlace = lightPlaceInfo;
            }

            public List<LightPlaceNode> RelatedLightPlaceNodes = new List<LightPlaceNode>();
        }

        private List<LightPlaceNode> m_lightPlaceNodes = new List<LightPlaceNode>();

        private List<LightPlaceInfo> m_lightPlaceInfos;

        private List<ParkingDividerGroupInfo> m_parkingDividerGroupInfos = new List<ParkingDividerGroupInfo>();

        public NearLightPlaceCalculator(List<LightPlaceInfo> lightPlaceInfos)
        {
            m_lightPlaceInfos = lightPlaceInfos;
        }

        public static List<ParkingDividerGroupInfo> MakeNearLightPlaceCalculator(List<LightPlaceInfo> lightPlaceInfos)
        {
            var nearLightPlaceCalculator = new NearLightPlaceCalculator(lightPlaceInfos);
            nearLightPlaceCalculator.Do();
            return nearLightPlaceCalculator.m_parkingDividerGroupInfos;
        }

        public void Do()
        {
            CalculatePlaceNodes();

            GenerateGroup();
        }

        private void GenerateGroup()
        {
            for (int i = 0; i < m_lightPlaceNodes.Count; i++)
            {
                var curRelatedNode = m_lightPlaceNodes[i];
                if (curRelatedNode.IsUse)
                    continue;

                CollectFromOneNode(curRelatedNode);
            }
        }

        private void CollectFromOneNode(LightPlaceNode relatedNode)
        {
            var relatedNodes = new List<LightPlaceNode>();
            relatedNodes.Add(relatedNode);

            var collectNodes = new List<LightPlaceInfo>();

            while (relatedNodes.Count != 0)
            {
                var curNode = relatedNodes.First();
                relatedNodes.Remove(curNode);

                if (curNode.IsUse)
                    continue;

                curNode.IsUse = true;
                collectNodes.Add(curNode.LightPlace);

                foreach (var childNode in curNode.RelatedLightPlaceNodes)
                {
                    if (childNode.IsUse)
                        continue;

                    relatedNodes.Add(childNode);
                }
            }

            m_parkingDividerGroupInfos.Add(new ParkingDividerGroupInfo(collectNodes));
        }

        private void CalculatePlaceNodes()
        {
            foreach (var lightInfo in m_lightPlaceInfos)
            {
                m_lightPlaceNodes.Add(new LightPlaceNode(lightInfo));
            }

            for (int i = 0; i < m_lightPlaceNodes.Count; i++)
            {
                var curPlaceNode = m_lightPlaceNodes[i];
                
                for (int j = i + 1; j < m_lightPlaceNodes.Count; j++)
                {
                    var nextPlaceNode = m_lightPlaceNodes[j];
                    if (IsNearGroup(curPlaceNode.LightPlace, nextPlaceNode.LightPlace))
                    {
                        curPlaceNode.RelatedLightPlaceNodes.Add(nextPlaceNode);
                        nextPlaceNode.RelatedLightPlaceNodes.Add(curPlaceNode);
                    }
                }
            }
        }

        private bool IsNearGroup(LightPlaceInfo firstLightPlaceInfo, LightPlaceInfo nextLightPlaceInfo)
        {
            var longLine = firstLightPlaceInfo.LongDirLength;
            var nextLongLine = nextLightPlaceInfo.LongDirLength;

            var longLineVec = (longLine.EndPoint - longLine.StartPoint).GetNormal();

            var nextLongLineVec = (nextLongLine.EndPoint - nextLongLine.StartPoint).GetNormal();

            if (GeomUtils.Rad2Angle(longLineVec.GetAngleTo(nextLongLineVec)) <= 1 
                || GeomUtils.Rad2Angle(longLineVec.Negate().GetAngleTo(nextLongLineVec)) <= 1)
            {
                return true;
            }

            return false;
        }
    }
}
