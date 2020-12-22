using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPLighting.Garage.Model
{
    public class ThWireOffsetData
    {
        public Line Center { get; set;}
        public Line First { get; set;}
        public Line Second { get; set; }     
        public bool IsDX { get; set; }
    }
}
