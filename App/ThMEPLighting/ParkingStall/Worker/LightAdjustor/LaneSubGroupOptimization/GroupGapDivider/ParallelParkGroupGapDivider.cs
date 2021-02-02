using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.ParkingStall.Model;

namespace ThMEPLighting.ParkingStall.Worker.LightAdjustor
{
    class ParallelParkGroupGapDivider : ParkGroupGapDivider
    {
        public ParallelParkGroupGapDivider(List<LightPlaceInfo> lightPlaceInfos, double gapDistance)
            : base(lightPlaceInfos, gapDistance)
        {

        }

        public static List<List<LightPlaceInfo>> MakeParallelParkGroupGapDivider(List<LightPlaceInfo> lightPlaceInfos, double gapDistance)
        {
            var parallelParkGroupGapDivider = new ParallelParkGroupGapDivider(lightPlaceInfos, gapDistance);
            parallelParkGroupGapDivider.Do();
            return parallelParkGroupGapDivider.LightPlaceGroupLst;
        }

        public override void Do()
        {
            PreProcess();

            GenerateGroup();
        }

        protected override bool IsValidRelatedInfo(LightPlaceInfo firstLightpLaceInfo, LightPlaceInfo secondLightPlaceInfo, double gapDistance)
        {
            return IsValidNearParkGroup(firstLightpLaceInfo.SmallProfileLongLines, secondLightPlaceInfo.SmallProfileLongLines, gapDistance);
        }
    }
}
