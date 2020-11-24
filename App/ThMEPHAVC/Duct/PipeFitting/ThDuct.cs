using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPHAVC.Duct.PipeFitting
{
    public class ThDuctParameters
    {
        public double DuctLength { get; set; }
        public double DuctSectionWidth { get; set; }
        public double DuctSectionHeight { get; set; }
        public double DuctStartPositionX { get; set; }
        public double DuctStartPositionY { get; set; }
        public double DuctEndPositionX { get; set; }
        public double DuctEndPositionY { get; set; }
    }
    public class ThDuct
    {
        public DBObjectCollection Geometries { get; set; }
        public ThDuctParameters DuctParameters { get; set; }
        public ThDuct(ThDuctParameters ductparameters)
        {
            DuctParameters = ductparameters;
        }
    }
}
