using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;

namespace ThMEPTCH.Model
{
    public class ThTCHSprinklerDim
    {
        public Point3d FirstPoint { get; set; }

        public double Rotation { get; set; }

        public double Dist2DimLine { get; set; }

        public double Scale { get; set; }

        public double LayoutRotation { get; set; }

        public List<double> SegmentValues { get; set; } = new List<double>();
        public string System { get; set; }

    }
}
