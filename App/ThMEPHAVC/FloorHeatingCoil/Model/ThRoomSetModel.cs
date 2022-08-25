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
        //output
        public List<Polyline> Door { get; set; } = new List<Polyline>();
        public ThFloorHeatingWaterSeparator WaterSeparator { get; set; }
        public List<ThFloorHeatingBathRadiator> BathRadiators { get; set; } = new List<ThFloorHeatingBathRadiator>();
        public List<Polyline> FurnitureObstacle { get; set; } = new List<Polyline>();
        public List<Line> RoomSeparateLine { get; set; } = new List<Line>();
        public List<ThFloorHeatingRoom> Room { get; set; } = new List<ThFloorHeatingRoom>();

        public Polyline Frame { get; set; }

        public bool HasHoleRoom { get; set; } = false;
    }
}
