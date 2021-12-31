using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.GridOperation.Model
{
    public class LineGridModel : GridModel
    {
        public Vector3d vecter 
        {
            get; set; 
        }

        public List<Line> xLines = new List<Line>();

        public List<Line> yLines = new List<Line>();
    }
}
