using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThMEPEngineCore.Algorithm;

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

        //public Polyline Frame { get; set; }

        public bool HasHoleRoom { get; set; } = false;

        public void Reset(ThMEPOriginTransformer transformer)
        {
            Door.ForEach(x => transformer.Reset(x));
            WaterSeparator.Reset(transformer);
            BathRadiators.ForEach(x => x.Reset(transformer));
            FurnitureObstacle.ForEach(x => transformer.Reset(x));
            RoomSeparateLine.ForEach(x => transformer.Reset(x));
            Room.ForEach (x=>x.Reset(transformer));
        }
    }
}
