using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.ParkingStall.Model;

namespace ThMEPLighting.ParkingStall.Worker.PipeConnector
{
    public class LaneGroupReader
    {
        private List<LaneGroup> m_laneGroups;

        public List<PipeLighterInfo> PipeLighterInfos = new List<PipeLighterInfo>();

        public LaneGroupReader(List<LaneGroup> laneGroups)
        {
            m_laneGroups = laneGroups;
        }

        public static List<PipeLighterInfo> MakePipeLaneLighterInfo(List<LaneGroup> laneGroups)
        {
            var laneGroupReader = new LaneGroupReader(laneGroups);
            laneGroupReader.Do();
            return laneGroupReader.PipeLighterInfos;
        }

        public void Do()
        {
            foreach (var laneGroup in m_laneGroups)
            {
                var lanePoly = laneGroup.LanePoly;
                var oneSideLaneLights = laneGroup.OneSideLightPlaceInfos.LightPlaceInfos;
                var otherSideLaneLights = laneGroup.AnotherSideLightPlaceInfos.LightPlaceInfos;

                if (oneSideLaneLights.Count == 0 && otherSideLaneLights.Count == 0)
                    continue;

                PipeLighterInfos.Add(new PipeLighterInfo(lanePoly, oneSideLaneLights, otherSideLaneLights));
            }
        }
    }
}
