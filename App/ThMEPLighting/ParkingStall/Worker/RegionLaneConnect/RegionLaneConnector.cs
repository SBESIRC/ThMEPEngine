using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.ParkingStall.Model;

namespace ThMEPLighting.ParkingStall.Worker.RegionLaneConnect
{
    /// <summary>
    /// 单侧车道线的灯组之间的区域连接线
    /// </summary>
    class RegionLaneConnector
    {
        private List<PipeLighterPolyInfo> m_pipeLighterPolyInfos;

        public RegionLaneConnector(List<PipeLighterPolyInfo> pipeLighterPolyInfos)
        {
            m_pipeLighterPolyInfos = pipeLighterPolyInfos;
        }

        public static void MakeRegionConnector(List<PipeLighterPolyInfo> pipeLighterPolyInfos)
        {
            var regionLaneConnector = new RegionLaneConnector(pipeLighterPolyInfos);
            regionLaneConnector.Do();
        }

        public void Do()
        {

        }
    }
}
