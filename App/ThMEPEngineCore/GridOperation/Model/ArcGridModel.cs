using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.GridOperation.Model
{
    public class ArcGridModel : GridModel
    {
        public List<Arc> arcLines = new List<Arc>();

        public List<Line> lines = new List<Line>();
    }
}
