using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPTCH.TCHArchDataConvert.THStructureEntity
{
    public class THStructureBeam : THStructureEntity
    {
        public double RelativeBG { get; set; }

        public double Width { get; set; }

        public double Length { get; set; }

        public Point3d Origin { get; set; }

        public Vector3d XVector { get; set; }
    }
}
