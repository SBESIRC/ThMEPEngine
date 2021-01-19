using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.ParkingStall.Model;

namespace ThMEPLighting.ParkingStall.Worker.LightAdjustor
{
    public class SubGroupPosOptimization
    {
        private List<LaneGroup> m_laneGroups;
        public SubGroupPosOptimization(List<LaneGroup> laneGroups)
        {
            m_laneGroups = laneGroups;
        }

        public static void MakeSubGroupPosOptimization(List<LaneGroup> laneGroups)
        {
            var subGroupPosOptimization = new SubGroupPosOptimization(laneGroups);
            subGroupPosOptimization.Do();
        }

        public void Do()
        {
            foreach (var laneGroup in m_laneGroups)
            {
                OptimizeOneSideInfo(laneGroup.LanePoly, laneGroup.OneSideLightPlaceInfos);
                OptimizeOneSideInfo(laneGroup.LanePoly, laneGroup.AnotherSideLightPlaceInfos);
            }
        }

        private void OptimizeOneSideInfo(Polyline lanePoly, LaneParkingStallSide laneParkingStallSide)
        {
            if (laneParkingStallSide.LightPlaceInfos.Count == 0)
                return;

            OptimizeParkingStallTypeInfo(lanePoly, laneParkingStallSide.ParallelParkingStall);
            OptimizeParkingStallTypeInfo(lanePoly, laneParkingStallSide.ReverseParkingStall);
        }

        /// <summary>
        /// 优化某一种车位类型
        /// </summary>
        /// <param name="lanePoly"></param>
        /// <param name="parkingStallTypeInfo"></param>
        private void OptimizeParkingStallTypeInfo(Polyline lanePoly, ParkingStallTypeInfo parkingStallTypeInfo)
        {
            if (parkingStallTypeInfo.LightPlaceInfos.Count == 0)
                return;

            foreach (var dividerGroupInfo in parkingStallTypeInfo.parkingDividerGroupInfos)
            {
                OptimizeParkingDividerGroupInfo(lanePoly, dividerGroupInfo);
            }
        }

        private void OptimizeParkingDividerGroupInfo(Polyline lanePoly, ParkingDividerGroupInfo parkingDividerGroupInfo)
        {
            if (parkingDividerGroupInfo.DividerLightPlaceInfos.Count == 0)
                return;

            var parkingStallType = parkingDividerGroupInfo.DividerLightPlaceInfos.First().ParkingSpace_TypeInfo;

            if (parkingStallType == ParkingSpace_Type.Reverse_stall_Parking)
            {
                AdjustorReverseLightPlaceInfos(lanePoly, parkingDividerGroupInfo.DividerLightPlaceInfos);
            }
            else if (parkingStallType == ParkingSpace_Type.Parallel_Parking)
            {
                AdjustorParallelLightPlaceInfos(lanePoly, parkingDividerGroupInfo.DividerLightPlaceInfos);
            }
        }

        /// <summary>
        /// 细分组倒车入库调整
        /// </summary>
        /// <param name="lanePoly"></param>
        /// <param name="lightPlaceInfos"></param>
        private void AdjustorReverseLightPlaceInfos(Polyline lanePoly, List<LightPlaceInfo> lightPlaceInfos)
        {
            // 根据距离差再分组
            var reversecGroupLst = ReverseParkGroupGapDivider.MakeReverseParkGroupGapDivider(lightPlaceInfos, ParkingStallCommon.ReverseGapGroup);

            foreach (var reverseGroup in reversecGroupLst)
            {
                ReverseSubGroupMover.MakeReverseSubGroupMover(lanePoly, reverseGroup, ParkingStallCommon.SubGroupPosTolerance);
            }
        }

        /// <summary>
        /// 细分组侧方停车调整
        /// </summary>
        /// <param name="lanePoly"></param>
        /// <param name="lightPlaceInfos"></param>
        private void AdjustorParallelLightPlaceInfos(Polyline lanePoly, List<LightPlaceInfo> lightPlaceInfos)
        {
            // 根据距离差再分组
            var parallelGroupLst = ParallelParkGroupGapDivider.MakeParallelParkGroupGapDivider(lightPlaceInfos, ParkingStallCommon.ParallelGapGroup);

            foreach (var parallelGroup in parallelGroupLst)
            {
                ParallelSubGroupMover.MakeParallelSubGroupMover(lanePoly, parallelGroup, ParkingStallCommon.SubGroupPosTolerance);
            }
        }
    }
}
