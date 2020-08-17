using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Model
{
    public class ThIfcLineBeam :ThIfcBeam
    {
        public Vector3d Direction { get; set; }
    }
}
