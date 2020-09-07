using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Model
{
    public class ThIfcArcBeam : ThIfcBeam
    {
        /// <summary>
        /// 起始端
        /// </summary>
        public Vector3d StartTangent { get; set; }
        public Vector3d EndTangent { get; set; }
        public double Radius { get; set; }
        public ThIfcArcBeam()
        {
        }
    }
}
