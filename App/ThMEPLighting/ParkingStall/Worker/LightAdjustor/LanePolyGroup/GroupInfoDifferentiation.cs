using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.ParkingStall.Assistant;
using ThMEPLighting.ParkingStall.Model;

namespace ThMEPLighting.ParkingStall.Worker.LightAdjustor
{
    /// <summary>
    /// 过滤，再分组
    /// </summary>
    class GroupInfoDifferentiation
    {
        private LaneGroup m_laneGroup;

        public GroupInfoDifferentiation(LaneGroup laneGroup)
        {
            m_laneGroup = laneGroup;
        }

        public static void MakeGroupInfoDifferentiation(LaneGroup laneGroup)
        {
            var groupInfoDifferentiation = new GroupInfoDifferentiation(laneGroup);
            groupInfoDifferentiation.Do();
        }

        public void Do()
        {
            // 车道线方向
            var laneDirection = CalculateDirection();

            // define one side type
            CalculateLightPlaceInfos(laneDirection, m_laneGroup.OneSideLightPlaceInfos.LightPlaceInfos);

            // define another side type
            CalculateLightPlaceInfos(laneDirection, m_laneGroup.AnotherSideLightPlaceInfos.LightPlaceInfos);


            // 再次细分
            CalculateParkingStallTypeInfos();
            // debug
            //DrawGroupInfo(m_laneGroup);
            DrawPileGroupInfo(m_laneGroup);
        }

        private void CalculateParkingStallTypeInfos()
        {
            CalculateLaneParkingSide(m_laneGroup.OneSideLightPlaceInfos);
            CalculateLaneParkingSide( m_laneGroup.AnotherSideLightPlaceInfos);
        }

        private void CalculateLaneParkingSide(LaneParkingStallSide laneParkingStallSide)
        {
            var totalLightPlaceInfos = laneParkingStallSide.LightPlaceInfos;

            var reverseParkingStalls = new List<LightPlaceInfo>();
            var parallelParkingStalls = new List<LightPlaceInfo>();
            foreach (var lightPlaceInfo in totalLightPlaceInfos)
            {
                if (lightPlaceInfo.ParkingSpace_TypeInfo == ParkingSpace_Type.Reverse_stall_Parking)
                {
                    reverseParkingStalls.Add(lightPlaceInfo);
                }
                else if (lightPlaceInfo.ParkingSpace_TypeInfo == ParkingSpace_Type.Parallel_Parking)
                {
                    parallelParkingStalls.Add(lightPlaceInfo);
                }
            }

            laneParkingStallSide.ReverseParkingStall = new ParkingStallTypeInfo(reverseParkingStalls);
            laneParkingStallSide.ParallelParkingStall = new ParkingStallTypeInfo(parallelParkingStalls);

            CalculateDividerGroupInfo(laneParkingStallSide.ReverseParkingStall);
            CalculateDividerGroupInfo(laneParkingStallSide.ParallelParkingStall);
        }


        private void CalculateDividerGroupInfo(ParkingStallTypeInfo parkingStallTypeInfo)
        {
            parkingStallTypeInfo.parkingDividerGroupInfos = NearLightPlaceCalculator.MakeNearLightPlaceCalculator(parkingStallTypeInfo.LightPlaceInfos);
        }

        private void DrawGroupInfo(LaneGroup laneGroup)
        {
            var onePlaceInfos = laneGroup.OneSideLightPlaceInfos;
            var anotherPlaceInfos = laneGroup.AnotherSideLightPlaceInfos;

            var lightGroupPolys = new List<Curve>();
            lightGroupPolys.AddRange(GetLightCurves(onePlaceInfos.LightPlaceInfos));
            lightGroupPolys.AddRange(GetLightCurves(anotherPlaceInfos.LightPlaceInfos));

            lightGroupPolys.Add(laneGroup.LanePoly);
            var twoSideGroupIds = DrawUtils.DrawProfileDebug(lightGroupPolys, "lightGroupPolys", Color.FromRgb(255, 0, 0));
            if (twoSideGroupIds.Count == 0)
                return;

            var totalIds = new ObjectIdList();
            totalIds.AddRange(twoSideGroupIds);
            var groupName = twoSideGroupIds.First().ToString();
            using (var db = AcadDatabase.Active())
            {
                GroupTools.CreateGroup(db.Database, groupName, totalIds);
            }
        }

        private void DrawPileGroupInfo(LaneGroup laneGroup)
        {
            var curPoly = laneGroup.LanePoly;
            LaneParkingStallSide oneSide = laneGroup.OneSideLightPlaceInfos;
            LaneParkingStallSide anotherSide = laneGroup.AnotherSideLightPlaceInfos;
            var totalIds = new ObjectIdList();
            var oneSideCurves = DrawLaneParkingStallSide(oneSide);
            var otherSideCurves = DrawLaneParkingStallSide(anotherSide);
            totalIds.AddRange(oneSideCurves);
            totalIds.AddRange(otherSideCurves);
            var curPolyIds = DrawUtils.DrawProfileDebug(new List<Curve> { curPoly }, "DrawPileGroupInfo", Color.FromRgb(255, 0, 0));

            totalIds.AddRange(curPolyIds);
            using (var db = AcadDatabase.Active())
            {
                GroupTools.CreateGroup(db.Database, Guid.NewGuid().ToString(), totalIds);
            }
        }

        private ObjectIdList DrawLaneParkingStallSide(LaneParkingStallSide laneParkingStallSide)
        {
            ParkingStallTypeInfo reverseParkingStalls = laneParkingStallSide.ReverseParkingStall;
            ParkingStallTypeInfo parallelParkingStalls = laneParkingStallSide.ParallelParkingStall;

            var totalIds = new ObjectIdList();
            var reverseCurveIds = DrawParkingStallTypeInfo(reverseParkingStalls);
            var parallelCurveIds = DrawParkingStallTypeInfo(parallelParkingStalls);
            totalIds.AddRange(reverseCurveIds);
            totalIds.AddRange(parallelCurveIds);
            if (totalIds.Count == 0)
                return totalIds;


            using (var db = AcadDatabase.Active())
            {
                GroupTools.CreateGroup(db.Database, Guid.NewGuid().ToString(), totalIds);
            }

            return totalIds;
        }

        private ObjectIdList DrawParkingStallTypeInfo(ParkingStallTypeInfo parkingStallTypeInfo)
        {
            var totalIds = new ObjectIdList();

            foreach (ParkingDividerGroupInfo parkingDividerGroupInfo in parkingStallTypeInfo.parkingDividerGroupInfos)
            {
                var parkingDividerGroupCurveIds = DrawParkingDividerGroupInfo(parkingDividerGroupInfo);
                totalIds.AddRange(parkingDividerGroupCurveIds);
            }

            if (totalIds.Count == 0)
                return totalIds;

            using (var db = AcadDatabase.Active())
            {
                GroupTools.CreateGroup(db.Database, Guid.NewGuid().ToString(), totalIds);
            }

            return totalIds;
        }

        private ObjectIdList DrawParkingDividerGroupInfo(ParkingDividerGroupInfo parkingDividerGroupInfo)
        {
            var curves = new List<Curve>();
            Color color = Color.FromRgb(255, 0, 0);
            foreach (LightPlaceInfo lightPlaceInfo in parkingDividerGroupInfo.DividerLightPlaceInfos)
            {
                if (lightPlaceInfo.ParkingSpace_TypeInfo == ParkingSpace_Type.Parallel_Parking)
                    color = Color.FromRgb(0, 255, 0);
                curves.Add(lightPlaceInfo.BigGroupInfo.BigGroupPoly);
            }

            var ids = DrawUtils.DrawProfileDebug(curves, "lightGroup", color);
            var totalIds = new ObjectIdList();
            totalIds.AddRange(ids);

            if (totalIds.Count == 0)
                return totalIds;

            using (var db = AcadDatabase.Active())
            {
                GroupTools.CreateGroup(db.Database, Guid.NewGuid().ToString(), totalIds);
            }

            return totalIds;
        }

        public List<Curve> GetLightCurves(List<LightPlaceInfo> lightPlaceInfos)
        {
            var curves = new List<Curve>();
            foreach (var lightPlaceInfo in lightPlaceInfos)
            {
                curves.Add(lightPlaceInfo.BigGroupInfo.BigGroupPoly);
            }
            return curves;
        }

        /// <summary>
        /// 定义类型，消除无效的车位块信息
        /// </summary>
        /// <param name="laneDirection"></param>
        /// <param name="lightPlaceInfos"></param>
        private void CalculateLightPlaceInfos(Vector3d laneDirection, List<LightPlaceInfo> lightPlaceInfos)
        {
            var invalidLightPlaceInfos = new List<LightPlaceInfo>();

            foreach (var lightPlaceInfo in lightPlaceInfos)
            {
                if (lightPlaceInfo.IsUsed)
                {
                    invalidLightPlaceInfos.Add(lightPlaceInfo);
                    continue;
                }

                var parkingTypeLine = lightPlaceInfo.LongDirLength;
                var longLineDirection = (parkingTypeLine.EndPoint - parkingTypeLine.StartPoint).GetNormal();

                var rad = laneDirection.GetAngleTo(longLineDirection);
                var angle = rad / Math.PI * 180;

                if (angle > 45 && angle < 135)
                {
                    // 倒车入库
                    lightPlaceInfo.ParkingSpace_TypeInfo = ParkingSpace_Type.Reverse_stall_Parking;
                    lightPlaceInfo.IsUsed = true;
                }
                else
                {
                    // 侧方停车
                    // 长度或者宽度异常
                    var parkingStallGroupLongLine = lightPlaceInfo.BigGroupInfo.BigGroupLongLine;
                    var parkingStallGroupShortLine = lightPlaceInfo.BigGroupInfo.BigGroupShortLine;
                    
                    if (parkingStallGroupLongLine.Length > ParkingStallCommon.ParkingStallGroupLengthRestrict
                        || parkingStallGroupShortLine.Length > ParkingStallCommon.ParkingStallGroupWidthRestrict)
                    {
                        invalidLightPlaceInfos.Add(lightPlaceInfo);
                        continue;
                    }

                    lightPlaceInfo.ParkingSpace_TypeInfo = ParkingSpace_Type.Parallel_Parking;
                    lightPlaceInfo.IsUsed = true;
                }
            }

            foreach (var invalidLight in invalidLightPlaceInfos)
            {
                lightPlaceInfos.Remove(invalidLight);
            }
        }
        

        private Vector3d CalculateDirection()
        {
            var lanePoly = m_laneGroup.LanePoly;
            var ptStart = lanePoly.StartPoint;
            var ptEnd = lanePoly.EndPoint;

            var vec = (ptEnd - ptStart).GetNormal();
            return vec;
        }
    }
}
