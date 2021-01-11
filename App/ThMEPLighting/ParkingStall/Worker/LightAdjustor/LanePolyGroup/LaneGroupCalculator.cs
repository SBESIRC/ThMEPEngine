using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.ParkingStall.Model;

namespace ThMEPLighting.ParkingStall.Worker.LightAdjustor
{
    public class LaneGroupCalculator
    {
        private List<LightPlaceInfo> m_lightPlaceInfos;
        private List<Polyline> m_extendPolylines;


        public List<LaneGroup> LaneGroups
        {
            get;
            set;
        }

        public LaneGroupCalculator(List<LightPlaceInfo> lightPlaceInfos, List<Polyline> polylines)
        {
            m_lightPlaceInfos = lightPlaceInfos;
            m_extendPolylines = polylines;
        }

        public static List<LaneGroup> MakeLaneGroupCalculator(List<LightPlaceInfo> lightPlaceInfos, List<Polyline> extendLanePolys)
        {
            var laneGroupCalculator = new LaneGroupCalculator(lightPlaceInfos, extendLanePolys);
            laneGroupCalculator.Do();
            return laneGroupCalculator.LaneGroups;
        }

        public void Do()
        {
            // 初步计算再细分
            LaneGroups = CalculateLaneGroupFirstStep();

            // 删除无效的车位块
            DifferentiationGroupInfo(LaneGroups);
        }

        private void DifferentiationGroupInfo(List<LaneGroup> laneGroups)
        {
            foreach (var laneGroup in laneGroups)
            {
                GroupInfoDifferentiation.MakeGroupInfoDifferentiation(laneGroup);
            }
        }

        private List<LaneGroup> CalculateLaneGroupFirstStep()
        {
            var laneGroups = new List<LaneGroup>();
            foreach (var poly in m_extendPolylines)
            {
                var laneGroup = IndexerCalculator.MakeLaneGroupInfo(poly, m_lightPlaceInfos);
                laneGroups.Add(laneGroup);
            }

            return laneGroups;
        }
    }
}
