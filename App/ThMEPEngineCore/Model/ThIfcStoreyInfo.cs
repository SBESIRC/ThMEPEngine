using System;

namespace ThMEPEngineCore.Model
{
    public class ThIfcStoreyInfo
    {
        private string _id = "";
        private double _height;
        private double _top_Elevation;
        private double _bottom_Elevation;
        public ThIfcStoreyInfo()
        {
            _id = Guid.NewGuid().ToString();
        }
        public string Id => _id;
        public string FloorNo { get; set; } = "";
        public string StdFlrNo { get; set; } = "";
        public string StoreyName { get; set; } = "";
        public string Description { get; set; } = "";
        /// <summary>
        /// 表示标高
        /// </summary>
        public double Elevation { get; set; }
        public double Top_Elevation
        {
            get => _top_Elevation;
            set
            {
                _top_Elevation=value;
                _bottom_Elevation = _top_Elevation - _height;
            }
        }
        public double Bottom_Elevation
        {
            get => _bottom_Elevation;
            set
            {
                _bottom_Elevation = value;
                _top_Elevation = _bottom_Elevation + _height;
            }
        }
        public double Height 
        {
            get => _height;
            set
            {
                _height=value;
                _top_Elevation = _bottom_Elevation + _height;
            }
        }       
    }
}
