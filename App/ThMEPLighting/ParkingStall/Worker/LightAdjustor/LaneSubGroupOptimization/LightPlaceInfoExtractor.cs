using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.ParkingStall.Model;

namespace ThMEPLighting.ParkingStall.Worker.LightAdjustor
{
    public class LightPlaceInfoExtractor
    {
        private List<LaneGroup> m_laneGroups;

        public List<LightPlaceInfo> LightPlaceInfos
        {
            get;
            set;
        } = new List<LightPlaceInfo>();

        public LightPlaceInfoExtractor(List<LaneGroup> laneGroups)
        {
            m_laneGroups = laneGroups;
        }

        public static List<LightPlaceInfo> MakeLightPlaceInfoExtractor(List<LaneGroup> laneGroups)
        {
            var lightPlaceInfoExtractor = new LightPlaceInfoExtractor(laneGroups);
            lightPlaceInfoExtractor.Do();
            return lightPlaceInfoExtractor.LightPlaceInfos;
        }

        public void Do()
        {
            foreach (var laneGroup in m_laneGroups)
            {
                LightPlaceInfos.AddRange(laneGroup.OneSideLightPlaceInfos.LightPlaceInfos);
                LightPlaceInfos.AddRange(laneGroup.AnotherSideLightPlaceInfos.LightPlaceInfos);
            }
        }
    }
}
