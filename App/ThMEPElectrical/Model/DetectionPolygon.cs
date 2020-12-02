using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.Model
{
    /// <summary>
    /// 纯数据
    /// </summary>
    public class DetectionPolygon
    {
        public Polyline Shell = null;
        public List<Polyline> Holes = new List<Polyline>();

        public DetectionPolygon(Polyline shell)
        {
            Shell = shell;
        }

        public DetectionPolygon(Polyline shell, List<Polyline> holes)
        {
            Shell = shell;

            Holes.AddRange(holes);
        }
    }
}
