using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPLighting.ParkingStall.Model;

namespace ThMEPLighting.ParkingStall.Worker.LightAdjustor
{
    public class LaneGroupCalculator
    {
        private List<LightPlaceInfo> m_lightPlaceInfos;
        private List<Polyline> m_extendPolylines;
        private bool m_bView;


        public List<LaneGroup> LaneGroups
        {
            get;
            set;
        }
        public List<LightPlaceInfo> NotHaveLaneLineParks 
        {
            get;
            set;
        }
        public LaneGroupCalculator(List<LightPlaceInfo> lightPlaceInfos, List<Polyline> polylines, bool bView)
        {
            m_lightPlaceInfos = lightPlaceInfos;
            m_extendPolylines = polylines;
            m_bView = bView;
            NotHaveLaneLineParks = new List<LightPlaceInfo>();
        }

        public static List<LaneGroup> MakeLaneGroupCalculator(List<LightPlaceInfo> lightPlaceInfos, List<Polyline> extendLanePolys,out List<LightPlaceInfo> noLaneLineParks, bool bView)
        {
            var laneGroupCalculator = new LaneGroupCalculator(lightPlaceInfos, extendLanePolys, bView);
            laneGroupCalculator.Do();
            noLaneLineParks = new List<LightPlaceInfo>();
            if (laneGroupCalculator.NotHaveLaneLineParks.Count > 0)
                noLaneLineParks.AddRange(laneGroupCalculator.NotHaveLaneLineParks);
            return laneGroupCalculator.LaneGroups;
        }

        public void Do()
        {
            // 初步计算再细分
            LaneGroups = CalculateLaneGroupFirstStep();
            //无归属车位基本不会再处于两个车道交界处，这里对无归属车位找车道线不在处理交界问题
            var noLaneLineParks = NoLaneLineParks();
            foreach(var item in LaneGroups) 
            {
                if (noLaneLineParks.Count < 1)
                    break;
                var laneGroup = IndexerCalculator.MakeLaneGroupInfo(item.LanePoly, noLaneLineParks, 8000.0);
                if (laneGroup.OneSideLightPlaceInfos.LightPlaceInfos.Count >0)
                {
                    var addLights = laneGroup.OneSideLightPlaceInfos.LightPlaceInfos;
                    item.OneSideLightPlaceInfos.LightPlaceInfos.AddRange(addLights);
                    noLaneLineParks = noLaneLineParks.Where(c => !addLights.Any(x => x.Position.DistanceTo(c.Position) < 1)).ToList();
                }
                if (laneGroup.AnotherSideLightPlaceInfos.LightPlaceInfos.Count > 0) 
                {
                    var addLights = laneGroup.AnotherSideLightPlaceInfos.LightPlaceInfos;
                    item.AnotherSideLightPlaceInfos.LightPlaceInfos.AddRange(addLights);
                    noLaneLineParks = noLaneLineParks.Where(c => !addLights.Any(x => x.Position.DistanceTo(c.Position) < 1)).ToList();
                }
            }
            // 删除无效的车位块
            DifferentiationGroupInfo(LaneGroups);
            NotHaveLaneLineParks.AddRange(m_lightPlaceInfos.Where(c => !c.IsUsed).ToList());
            if (m_bView) 
            {
                foreach (var drawLaneGroup in LaneGroups)
                {
                    LaneGroupDrawer.MakeDrawLaneGroup(drawLaneGroup);
                }
            }
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
                var laneGroup = IndexerCalculator.MakeLaneGroupInfo(poly, m_lightPlaceInfos, ParkingStallCommon.LaneOffset);
                laneGroups.Add(laneGroup);
            }
            return laneGroups;
        }
        private List<LightPlaceInfo> NoLaneLineParks() 
        {
            var noLaneLineParks = new List<LightPlaceInfo>();
            if (null == m_lightPlaceInfos || m_lightPlaceInfos.Count < 1 || LaneGroups ==null || LaneGroups.Count<1)
                return noLaneLineParks;
            foreach (var park in m_lightPlaceInfos)
            {
                bool inLine = false;
                foreach (var item in LaneGroups)
                {
                    if (inLine)
                        break;
                    if (item.OneSideLightPlaceInfos.LightPlaceInfos.Any(c => c.Position.DistanceTo(park.Position) < 1))
                    {
                        inLine = true;
                        break;
                    }
                    if (item.AnotherSideLightPlaceInfos.LightPlaceInfos.Any(c => c.Position.DistanceTo(park.Position) < 1))
                    {
                        inLine = true;
                        break;
                    }
                }
                if (inLine)
                    continue;
                noLaneLineParks.Add(park);
            }
            return noLaneLineParks;
        }

    }
}
