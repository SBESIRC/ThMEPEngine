using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;

namespace ThMEPArchitecture.ParkingStallArrangement.Model
{
    public class Ramps
    {
        public Point3d InsertPt { get; set; }
        public Polyline Ramp { get; set; }
        public Ramps(Point3d insertPt, BlockReference ramp)
        {
            InsertPt = insertPt;
            Ramp = ((Extents3d)(ramp.Bounds)).ToRectangle();
        }
    }
}
