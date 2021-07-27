using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using ThMEPEngineCore.Model.Electrical;

namespace ThMEPEngineCore.GeojsonExtractor.Model
{
    public class ThEStoreyInfo : ThStoreyInfo
    {
        public ThEStoreys Storey { get; set; }
        public ThEStoreyInfo(ThEStoreys eStorey)
        {
            Storey = eStorey;
            Id = Guid.NewGuid().ToString();
            Parse();
        }
        private void Parse()
        {
            StoreyRange = GetFloorRange(Storey.ObjectId);
            OriginFloorNumber = GetFloorNumber(Storey.ObjectId);
            StoreyNumber = string.Join(",", Storey.Storeys);
            Boundary = GetBoundary(Storey.ObjectId);
            BasePoint = GetBasePoint(Storey.ObjectId);
            StoreyType = Storey.StoreyTypeString;
        }
    }
}
