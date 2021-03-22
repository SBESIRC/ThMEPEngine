using System.Linq;
using ThMEPEngineCore.Model;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThRoomSpatialPredicateService
    {
        public List<ThIfcRoom> Rooms { get; set; }
        private ThContourRelationQueryService QueryService { get; set; }
        public ThRoomSpatialPredicateService(List<ThIfcRoom> rooms)
        {
            Rooms = rooms;
            QueryService = new ThContourRelationQueryService(
                rooms.Where(o=>o.Boundary is Polyline).Select(o => o.Boundary as Polyline).ToList());
        }

        /// <summary>
        /// 包含关系
        /// </summary>
        /// <param name="room"></param>
        /// <returns></returns>
        public List<ThIfcRoom> Contains(ThIfcRoom room)
        {
            var rooms = new List<ThIfcRoom>();
            if (room.Boundary is Polyline outline)
            {
                var boundaries = QueryService.Contains(outline);
                rooms = Rooms.Where(o => boundaries.Contains(o.Boundary as Polyline)).ToList();
            }
            return rooms;
        }

        /// <summary>
        /// 包含关系
        /// </summary>
        /// <param name="room"></param>
        /// <returns></returns>
        public List<ThIfcRoom> LoosenContains(ThIfcRoom room,double tolerance=-5.0)
        {
            var rooms = new List<ThIfcRoom>();
            if (room.Boundary is Polyline outline)
            {
                var boundaries = QueryService.LoosenContains(outline, tolerance);
                rooms = Rooms.Where(o => boundaries.Contains(o.Boundary as Polyline)).ToList();
            }
            return rooms;
        }

        /// <summary>
        /// 附近
        /// </summary>
        /// <param name="room"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public List<ThIfcRoom> Nears(ThIfcRoom room, double distance)
        {
            var rooms = new List<ThIfcRoom>();
            if (room.Boundary is Polyline outline)
            {
                var boundaries = QueryService.Nears(outline, distance);
                rooms = Rooms.Where(o => boundaries.Contains(o.Boundary as Polyline)).ToList();
            }
            return rooms;
        }
    }
}
