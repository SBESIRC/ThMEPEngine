using System;
using ThMEPEngineCore.Model.Electrical;

namespace ThMEPElectrical.FireAlarm.Model
{
    public class EStoreyInfo : StoreyInfo
    {
        public ThEStoreys Storey { get; set; }

        public EStoreyInfo(ThEStoreys storey)
        {
            Storey = storey;
            Id = Guid.NewGuid().ToString();
            Parse();
        }
        private void Parse()
        {
            StoreyRange = GetFloorRange();
            OriginFloorNumber = GetFloorNumber();
            StoreyNumber = string.Join(",", Storey.Storeys);
            Boundary = GetBoundary();
            BasePoint = GetBasePoint();
            StoreyType = Storey.StoreyTypeString;
        }
    }
}
