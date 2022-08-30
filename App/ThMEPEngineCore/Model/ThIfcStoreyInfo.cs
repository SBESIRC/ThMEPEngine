using System;
using ThCADExtension;

namespace ThMEPEngineCore.Model
{
    public class ThIfcStoreyInfo
    {
        public ThIfcStoreyInfo()
        {
            Id = Guid.NewGuid().ToString();
        }
        public string Id { get; private set; } = "";
        public string StoreyName { get; set; } = "";
        public string Elevation { get; set; } = "";
        public string Top_Elevation { get; set; } = "";
        public string Bottom_Elevation { get; set; } = "";
        public string Description { get; set; } = "";
        public string FloorNo { get; set; } = "";
        public string Height { get; set; } = "";
        public string StdFlrNo { get; set; } = "";

        public double Height_Value => ConvertToDouble(Height);
        public double Elevation_Value => ConvertToDouble(Elevation);
        public double Top_Elevation_Value => ConvertToDouble(Top_Elevation);
        public double Bottom_Elevation_Value => ConvertToDouble(Bottom_Elevation);

        private double ConvertToDouble(string doubleVal)
        {
            var newDoubleVal = doubleVal.Trim();
            if (!string.IsNullOrEmpty(newDoubleVal) && ThStringTools.IsDouble(newDoubleVal))
            {
                return double.Parse(newDoubleVal);
            }
            else
            {
                return 0.0;
            }
        }
    }
}
