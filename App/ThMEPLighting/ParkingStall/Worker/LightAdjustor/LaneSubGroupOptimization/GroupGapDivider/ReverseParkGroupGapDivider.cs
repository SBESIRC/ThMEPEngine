using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.ParkingStall.Model;

namespace ThMEPLighting.ParkingStall.Worker.LightAdjustor
{
    class ReverseParkGroupGapDivider : ParkGroupGapDivider
    {
        public ReverseParkGroupGapDivider(List<LightPlaceInfo> lightPlaceInfos, double gapDistance)
            : base(lightPlaceInfos, gapDistance)
        {
        }

        public static List<List<LightPlaceInfo>> MakeReverseParkGroupGapDivider(List<LightPlaceInfo> lightPlaceInfos, double gapDistance)
        {
            var reverseParkGroupGapDivider = new ReverseParkGroupGapDivider(lightPlaceInfos, gapDistance);
            reverseParkGroupGapDivider.Do();
            return reverseParkGroupGapDivider.LightPlaceGroupLst;
        }

        public override void Do()
        {
            PreProcess();

            GenerateGroup();
        }

        protected override bool IsValidRelatedInfo(LightPlaceInfo firstLightpLaceInfo, LightPlaceInfo secondLightPlaceInfo, double gapDistance)
        {
            return IsValidNearParkGroup(firstLightpLaceInfo.SmallProfileShortLines, secondLightPlaceInfo.SmallProfileShortLines, gapDistance);
        }
    }
}
