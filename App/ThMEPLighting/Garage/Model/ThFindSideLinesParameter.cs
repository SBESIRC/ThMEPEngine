using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Model
{
    public class ThFindSideLinesParameter
    {
        public List<Line> CenterLines { get; set; }
        public List<Line> SideLines { get; set; }
        /// <summary>
        /// 中心线一侧的宽度
        /// </summary>
        public double HalfWidth { get; set; }
        public ThFindSideLinesParameter()
        {
            CenterLines = new List<Line>();
            SideLines =new List<Line>();
        }
        public bool IsValid() 
        {
            return CenterLines.Count > 0 && SideLines.Count > 0 && HalfWidth > 0.0;
        }
    }
}
