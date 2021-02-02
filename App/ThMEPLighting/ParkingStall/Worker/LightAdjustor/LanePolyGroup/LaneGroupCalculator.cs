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

            // group 联合比较再定位
            CalculateLightPlaceInfoOwner();

            EraseInvalidParkLights(laneGroups);

            foreach (var drawLaneGroup in laneGroups)
            {
                LaneGroupDrawer.MakeDrawLaneGroup(drawLaneGroup);
            }
        }

        private void EraseInvalidParkLights(List<LaneGroup> laneGroups)
        {
            foreach (var laneGroup in laneGroups)
            {
                EraseInvalidParkingStallSide(laneGroup.AnotherSideLightPlaceInfos);
                EraseInvalidParkingStallSide(laneGroup.OneSideLightPlaceInfos);
            }
        }

        private void EraseInvalidParkingStallSide(LaneParkingStallSide laneParkingStallSide)
        {
            var totalLaneParkingStallLights = laneParkingStallSide.LightPlaceInfos;
            EraseInvalidParkingStallTypeInfo(laneParkingStallSide.ParallelParkingStall, totalLaneParkingStallLights);
            EraseInvalidParkingStallTypeInfo(laneParkingStallSide.ReverseParkingStall, totalLaneParkingStallLights);
        }

        private void EraseInvalidParkingStallTypeInfo(ParkingStallTypeInfo parkingStallTypeInfo, List<LightPlaceInfo> lightPlaceInfos)
        {
            var invalidLights = new List<LightPlaceInfo>();

            foreach (var lightInfo in parkingStallTypeInfo.LightPlaceInfos)
            {
                if (!lightPlaceInfos.Contains(lightInfo))
                {
                    invalidLights.Add(lightInfo);
                }
            }

            foreach (var invalidLightInfo in invalidLights)
            {
                parkingStallTypeInfo.LightPlaceInfos.Remove(invalidLightInfo);
            }

            foreach (var dividerGroupInfo in parkingStallTypeInfo.parkingDividerGroupInfos)
            {
                EraseInvalidParkingDividerGroupInfo(dividerGroupInfo, parkingStallTypeInfo.LightPlaceInfos);
            }
        }


        private void EraseInvalidParkingDividerGroupInfo(ParkingDividerGroupInfo parkingDividerGroupInfo, List<LightPlaceInfo> lightPlaceInfos)
        {
            var invalidLights = new List<LightPlaceInfo>();
            foreach (var light in parkingDividerGroupInfo.DividerLightPlaceInfos)
            {
                if (!lightPlaceInfos.Contains(light))
                    invalidLights.Add(light);
            }

            foreach (var invalidLight in invalidLights)
                parkingDividerGroupInfo.DividerLightPlaceInfos.Remove(invalidLight);
        }


        private void CalculateLightPlaceInfoOwner()
        {
            var lightPlaceInfoLst = new List<LightPlaceInfo>();

            foreach (var lightPlaceInfo in m_lightPlaceInfos)
            {
                if (lightPlaceInfo.lightsLst.Count > 1)
                    lightPlaceInfoLst.Add(lightPlaceInfo);
            }

            foreach (var lightInfo in lightPlaceInfoLst)
            {
                var lightsLst = lightInfo.lightsLst;

                var validIndex = ValidPlaceOneSideIndex(lightsLst);

                if (validIndex != -1)
                {
                    for (int i = 0; i < lightsLst.Count; i++)
                    {
                        if (i != validIndex)
                        {
                            lightsLst[i].LightPlaceInfos.Remove(lightInfo);
                        }
                    }
                }
            }
        }

        private int ValidPlaceOneSideIndex(List<LightPlaceOneSide> lightPlaceOneSides)
        {
            for (int i = 0; i < lightPlaceOneSides.Count; i++)
            {
                var curPlaceOneSide = lightPlaceOneSides[i];
                if (curPlaceOneSide.ParkType == ParkingSpace_Type.Reverse_stall_Parking)
                    return i;
            }

            return -1;
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
