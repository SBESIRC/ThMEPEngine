using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.Model
{
    public class VerticalPipeModel
    {
        public Point3d Position { get; set; }

        public Entity PipeEntity { get; set; }

        public List<Line> LeadLines { get; set; }

        public List<DBText> BText { get; set; }
    }
}
