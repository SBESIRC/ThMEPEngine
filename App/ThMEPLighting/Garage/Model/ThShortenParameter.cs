using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.Garage.Model
{
    public class ThShortenParameter
    {
        public Polyline Border { get; set; }
        public List<Line> DxLines { get; set; }
        public List<Line> FdxLines { get; set; }
        public double Distance { get; set; }
        public ThShortenParameter()
        {
            DxLines = new List<Line>();
            FdxLines = new List<Line>();
        }

        public bool IsValid
        {
            get
            {
                return CheckParameter();
            }
        }
        private bool CheckParameter()
        {
            if(Border==null || Border.IsDisposed)
            {
                return false;
            }
            if(DxLines.Count==0)
            {
                return false;
            }
            return true;     
        }
    }
}
