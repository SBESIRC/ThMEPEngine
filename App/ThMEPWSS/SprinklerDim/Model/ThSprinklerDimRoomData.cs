using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPWSS.SprinklerDim.Model
{
    public class ThSprinklerDimRoomData
    {
        public List<Line> TchPipe { get; private set; } = new List<Line>();
        public List<Polyline> TchPipeText { get; private set; } = new List<Polyline>();
        public List<Polyline> Column { get; set; } = new List<Polyline>();
        public List<Polyline> Wall { get; set; } = new List<Polyline>();
       // public List<Polyline> Room { get; set; } = new List<Polyline>();
        public List<MPolygon> RoomM { get; set; } = new List<MPolygon>();
        public List<Line> AxisCurves { get; set; } = new List<Line>();
        public List<Point3d> SprinklerPt { get; set; } = new List<Point3d>();

    }
}
