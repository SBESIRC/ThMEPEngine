using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.ParkingStall.Model
{
    // 按照1度偏差细分类型
    public class ParkingDividerGroupInfo
    {
        public List<LightPlaceInfo> DividerLightPlaceInfos;

        public ParkingDividerGroupInfo(List<LightPlaceInfo> lightPlaceInfos)
        {
            DividerLightPlaceInfos = lightPlaceInfos;
        }
    }

    // 侧方或者倒库类型
    public class ParkingStallTypeInfo
    {
        public List<LightPlaceInfo> LightPlaceInfos;

        public List<ParkingDividerGroupInfo> parkingDividerGroupInfos = new List<ParkingDividerGroupInfo>();

        public ParkingStallTypeInfo(List<LightPlaceInfo> lightPlaceInfos)
        {
            LightPlaceInfos = lightPlaceInfos;
        }
    }

    public class LaneParkingStallSide
    {
        public List<LightPlaceInfo> LightPlaceInfos;

        public ParkingStallTypeInfo ReverseParkingStall;

        public ParkingStallTypeInfo ParallelParkingStall;
    }

    public class LaneGroup
    {
        public LaneParkingStallSide OneSideLightPlaceInfos = new LaneParkingStallSide();

        public LaneParkingStallSide AnotherSideLightPlaceInfos = new LaneParkingStallSide();
        public Polyline LanePoly;

        public LaneGroup(Polyline polyline, List<LightPlaceInfo> onePlaceInfos, List<LightPlaceInfo> anotherPlaceInfos)
        {
            LanePoly = polyline;
            OneSideLightPlaceInfos.LightPlaceInfos = onePlaceInfos;
            AnotherSideLightPlaceInfos.LightPlaceInfos = anotherPlaceInfos;
        }
    }
}
