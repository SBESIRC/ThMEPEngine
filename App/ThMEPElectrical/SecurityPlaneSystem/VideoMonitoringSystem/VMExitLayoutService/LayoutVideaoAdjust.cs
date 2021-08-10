using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.SecurityPlaneSystem.VideoMonitoringSystem.Model;
using ThMEPEngineCore.Model;

namespace ThMEPElectrical.SecurityPlaneSystem.VideoMonitoringSystem.VMExitLayoutService
{
    public class LayoutVideaoAdjust
    {
        readonly double distance = 500;
        readonly double angleTol = Math.PI / 180 * 45;

        /// <summary>
        /// 删除一个房间内距离过近且角度小于45°的摄像头
        /// </summary>
        /// <param name="videaos"></param>
        public List<LayoutModel> ClearClostVideao(Dictionary<ThIfcRoom, List<LayoutModel>> layoutInfos)
        {
            List<LayoutModel> resModels = new List<LayoutModel>();
            foreach (var videaos in layoutInfos.Values)
            {
                if (videaos.Count <= 0)
                {
                    continue;
                }
                var tempVideaos = new List<LayoutModel>(videaos);
                var firV = tempVideaos.First();
                while (tempVideaos.Count > 0)
                {
                    tempVideaos.Remove(firV);
                    resModels.Add(firV);
                    var otherVideaos = tempVideaos.Where(x => x.layoutPt.DistanceTo(firV.layoutPt) < distance).ToList();
                    foreach (var videao in otherVideaos)
                    {
                        var angle = firV.layoutDir.GetAngleTo(videao.layoutDir);
                        if (Math.PI < angle)
                        {
                            angle = 2 * Math.PI - angle;
                            if (angleTol > angle)
                            {
                                tempVideaos.Remove(videao);
                            }
                        }
                    }

                    if (tempVideaos.Count > 0)
                    {
                        firV = tempVideaos.First();
                    }
                }

                resModels.AddRange(tempVideaos);
            }

            return resModels;
        }
    }
}
