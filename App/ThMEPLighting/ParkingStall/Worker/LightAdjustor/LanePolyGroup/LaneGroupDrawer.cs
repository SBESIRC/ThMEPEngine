using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
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
    public class LaneGroupDrawer
    {
        private LaneGroup m_laneGroup;
        public LaneGroupDrawer(LaneGroup laneGroup)
        {
            m_laneGroup = laneGroup;
        }

        public static void MakeDrawLaneGroup(LaneGroup laneGroup)
        {
            var laneGroupDrawer = new LaneGroupDrawer(laneGroup);
            laneGroupDrawer.DrawPileGroupInfo();
        }

        private void DrawPileGroupInfo()
        {
            var curPoly = m_laneGroup.LanePoly;
            LaneParkingStallSide oneSide = m_laneGroup.OneSideLightPlaceInfos;
            LaneParkingStallSide anotherSide = m_laneGroup.AnotherSideLightPlaceInfos;
            var totalIds = new ObjectIdList();
            var oneSideCurves = DrawLaneParkingStallSide(oneSide);
            var otherSideCurves = DrawLaneParkingStallSide(anotherSide);
            totalIds.AddRange(oneSideCurves);
            totalIds.AddRange(otherSideCurves);
            var curPolyIds = DrawUtils.DrawProfileDebug(new List<Curve> { curPoly }, "DrawPileGroupInfo", Color.FromRgb(255, 0, 0));

            totalIds.AddRange(curPolyIds);
            if (totalIds == null || totalIds.Count < 1)
                return;
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
    }
}
