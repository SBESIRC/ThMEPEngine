using System.Linq;
using System.Collections.Generic;
using ThCADCore.NTS;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;

namespace ThMEPWSS.Pipe.Service
{
    public abstract class ThDrainwellService
    {
        public double ClosedDistance { get; set; } = 500.0;
        public List<ThIfcRoom> Drainwells { get; set; }
        public List<ThIfcRoom> Pypes { get; set; }
        protected List<ThIfcRoom> Spaces { get; set; }
        protected ThCADCoreNTSSpatialIndex SpaceSpatialIndex;
        protected ThRoomSpatialPredicateService SpacePredicateService { get; set; }
        protected ThDrainwellService()
        {
            Spaces = new List<ThIfcRoom>();
            Drainwells = new List<ThIfcRoom>();
            Pypes= new List<ThIfcRoom>();
            SpacePredicateService = new ThRoomSpatialPredicateService(Spaces);
        }
        protected ThDrainwellService(List<ThIfcRoom> spaces, 
            ThCADCoreNTSSpatialIndex spaceSpatialIndex=null)
        {
            Spaces = spaces;
            Drainwells = new List<ThIfcRoom>();
            Pypes = new List<ThIfcRoom>();
            SpaceSpatialIndex = spaceSpatialIndex;
            if (SpaceSpatialIndex == null)
            {
                DBObjectCollection dbObjs = new DBObjectCollection();
                Spaces.ForEach(o => dbObjs.Add(o.Boundary));
                SpaceSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
            }
            SpacePredicateService = new ThRoomSpatialPredicateService(Spaces);
        }
        /// <summary>
        /// 找到空间相邻的且含有排水管井的阳台
        /// </summary>
        /// <param name="toiletSpace"></param>
        /// <returns></returns>
        protected List<ThIfcRoom> FindNeighbouringBalconyWithDrainwell(ThIfcRoom space,double bufferDis)
        {
            //空间轮廓往外括500
            var bufferObjs = ThCADCoreNTSOperation.Buffer(space.Boundary as Polyline, bufferDis);
            if (bufferObjs.Count == 0)
            {
                return new List<ThIfcRoom>();
            }           

            var crossObjs = SpaceSpatialIndex.SelectCrossingPolygon(bufferObjs[0] as Polyline);
            //获取偏移后，能框选到的空间
            var crossSpaces = Spaces.Where(o => crossObjs.Contains(o.Boundary));
            //找到含有阳台的空间
            var balconies = crossSpaces.Where(m => m.Tags.Where(n => n.Contains("阳台")).Any());
            //找到含有排水管井的阳台空间
            var incluedrainwellBalconies = balconies.Where(m => SpacePredicateService.Contains(m).Where(n => n.Tags.Count == 0).Any()).ToList();
            //
            return incluedrainwellBalconies;
        }
        /// <summary>
        /// 找到厨房相邻的且含有排水管井的卫生间
        /// </summary>
        /// <param name="toiletSpace"></param>
        /// <returns></returns>
        protected List<ThIfcRoom> FindNeighbouringToiletWithDrainwell(ThIfcRoom kitchenSpace, double bufferDis)
        {
            //空间轮廓往外括500
            var bufferObjs = ThCADCoreNTSOperation.Buffer(kitchenSpace.Boundary as Polyline, bufferDis);
            if (bufferObjs.Count == 0)
            {
                return null;
            }
            var crossObjs = SpaceSpatialIndex.SelectCrossingPolygon(bufferObjs[0] as Polyline);
            //获取偏移后，能框选到的空间
            var crossSpaces = Spaces.Where(o => crossObjs.Contains(o.Boundary));
            //找到含有卫生间的空间
            var toilets = crossSpaces.Where(m => m.Tags.Where(n => n.Contains("卫生间")).Any());
            //找到含有排水管井的阳台空间
            var incluedrainwellToilets = toilets.Where(m => SpacePredicateService.Contains(m).Where(n => n.Tags.Count == 0).Any()).ToList();
            //
            return incluedrainwellToilets;
        }
        /// <summary>
        /// 获取距离空间边界指定距离范围内排水管井
        /// </summary>
        /// <param name="space"></param>
        /// <param name="drainwells"></param>
        /// <returns></returns>
        protected List<ThIfcRoom> FilterDistancedDrainwells(ThIfcRoom space, List<ThIfcRoom> drainwells)
        {
            var filterInstance = ThFindSpaceRangedSpaces.Find(space, drainwells, ClosedDistance);
            return filterInstance.SearchSpaces;
        }
    }
}
