using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Model
{
    public class ThGeometry
    {
        public Dictionary<string, object> Properties { get; set; }

        public List<Line> Segments { get; set; }
        public ThGeometry()
        {
            Segments = new List<Line>();
            Properties = new Dictionary<string, object>();
        }
    }
}
