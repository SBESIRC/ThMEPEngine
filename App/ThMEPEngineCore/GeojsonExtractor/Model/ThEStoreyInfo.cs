using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using System.Collections.Generic;
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
            StoreyNumber = ParseStoreyNumber(Storey.Storeys);
            Boundary = GetBoundary(Storey.ObjectId);
            BasePoint = GetBasePoint(Storey.ObjectId);
            StoreyType = Storey.StoreyTypeString;
        }
        private string ParseStoreyNumber(List<string> floorNumbers)
        {
            List<string> parseString = new List<string>();
            floorNumbers.ForEach(o => 
            {
                string[] s = o.Split('M');
                if (s.Length == 1)
                    parseString.Add(s[0]);
                else if (s.Length == 2)
                    parseString.Add(s[0] + ".5F");
            });
            return string.Join(",", parseString);
        }
    }
}
