using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPStructure.GirderConnect.SecondaryBeamConnect.Model
{
    public class LayoutResult
    {
        public LayoutResult()
        {
            vector=Vector3d.ZAxis;
            edges=new List<BeamEdge>();
            SecondaryBeamLines=new List<Line>();
        }
        public Vector3d vector { get; set; }
        public List<BeamEdge> edges { get; set; }

        /// <summary>
        /// 次梁布置线
        /// </summary>
        public List<Line> SecondaryBeamLines { get; set; }
    }
}
