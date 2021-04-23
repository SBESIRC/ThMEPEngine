using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.ParkingStall.Model
{
    /// <summary>
    /// 车道线和车位灯的信息
    /// </summary>
    public class PipeLighterInfo
    {
        public Polyline LanePoly;
        public List<LightPlaceInfo> OneSideLights;
        public List<LightPlaceInfo> OtherSideLights;

        public PipeLighterInfo(Polyline lanePoly, List<LightPlaceInfo> oneSideLights, List<LightPlaceInfo> otherSideLights)
        {
            LanePoly = lanePoly;
            OneSideLights = oneSideLights;
            OtherSideLights = otherSideLights;
        }
    }
}
