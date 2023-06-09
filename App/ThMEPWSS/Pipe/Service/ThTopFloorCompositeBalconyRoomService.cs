﻿using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.Pipe.Geom;

namespace ThMEPWSS.Pipe.Service
{
    public class ThTopFloorCompositeBalconyRoomService
    {
        private List<ThWCompositeBalconyRoom> CompositeBalconyRoom { get; set; }
        private ThIfcSpace Space { get; set; }
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        private ThTopFloorCompositeBalconyRoomService(
           ThIfcSpace space,
           List<ThWCompositeBalconyRoom> compositeBalconyRoom)
        {
            CompositeBalconyRoom = compositeBalconyRoom;
            Space = space;
            var objs = new DBObjectCollection();
            SpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
        }
        public static List<ThWCompositeBalconyRoom> Find(
            ThIfcSpace space,
            List<ThWCompositeBalconyRoom> compositeBalconyRoom)
        {          
            return Findspace(space, compositeBalconyRoom);
        }
        private static List<ThWCompositeBalconyRoom> Findspace(ThIfcSpace FirstFloorSpace, List<ThWCompositeBalconyRoom> compositeBalconyRoom)
        {
            var balconyroom = new List<ThWCompositeBalconyRoom>();
            foreach (var room in compositeBalconyRoom)
            {
                var bboundary = FirstFloorSpace.Boundary as Polyline;
                if (GeomUtils.PtInLoop(bboundary, room.Balcony.Space.Boundary.GetCenter()))
                {
                    balconyroom.Add(room);
                }
            }
            return balconyroom;
        }
    
    }
}

