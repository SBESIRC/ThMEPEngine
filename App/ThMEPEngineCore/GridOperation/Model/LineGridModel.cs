using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

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
