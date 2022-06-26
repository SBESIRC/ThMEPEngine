using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPHVAC.FloorHeatingCoil.Model
{
    public class ThRoomSetModel
    {
        public List<Polyline> Door { get; set; } = new List<Polyline>();
        public List<ThFloorHeatingRoom> Room { get; set; } = new List<ThFloorHeatingRoom>();
        public ThFloorHeatingWaterSeparator WaterSeparator { get; set; }
        public List<Polyline> FurnitureObstacle { get; set; } = new List<Polyline>();
        public List<Line> RoomSeparateLine { get; set; } = new List<Line>();

        public Polyline Frame { get; set; }
    }
}
