using System.Linq;
using ThMEPEngineCore.Model;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThSpaceSpatialPredicateService
    {
        public List<ThIfcSpace> Spaces { get; set; }
        private ThContourRelationQueryService QueryService { get; set; }
        public ThSpaceSpatialPredicateService(List<ThIfcSpace> spaces)
        {
            Spaces = spaces;
            QueryService = new ThContourRelationQueryService(
                Spaces.Where(o=>o.Boundary is Polyline).Select(o => o.Boundary as Polyline).ToList());
        }

        /// <summary>
        /// 包含关系
        /// </summary>
        /// <param name="room"></param>
        /// <returns></returns>
        public List<ThIfcSpace> Contains(ThIfcSpace space)
        {
            var spaces = new List<ThIfcSpace>();
            if (space.Boundary is Polyline outline)
            {
                var boundaries = QueryService.Contains(outline);
                spaces = Spaces.Where(o => boundaries.Contains(o.Boundary as Polyline)).ToList();
            }
            return spaces;
        }

        /// <summary>
        /// 包含关系
        /// </summary>
        /// <param name="room"></param>
        /// <returns></returns>
        public List<ThIfcSpace> LoosenContains(ThIfcSpace space, double tolerance = -5.0)
        {
            var spaces = new List<ThIfcSpace>();
            if (space.Boundary is Polyline outline)
            {
                var boundaries = QueryService.LoosenContains(outline, tolerance);
                spaces = Spaces.Where(o => boundaries.Contains(o.Boundary as Polyline)).ToList();
            }
            return spaces;
        }


        /// <summary>
        /// 附近
        /// </summary>
        /// <param name="room"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public List<ThIfcSpace> Nears(ThIfcSpace space, double distance)
        {
            var spaces = new List<ThIfcSpace>();
            if (space.Boundary is Polyline outline)
            {
                var boundaries = QueryService.Nears(outline, distance);
                spaces = Spaces.Where(o => boundaries.Contains(o.Boundary as Polyline)).ToList();
            }
            return spaces;
        }
    }
}
