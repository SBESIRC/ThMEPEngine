using System.Linq;
using System.Collections.Generic;
using ThCADCore.NTS;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.Pipe.Service
{
    public class ThFindSpaceRangedSpaces
    {
        public List<ThIfcRoom> SearchSpaces = new List<ThIfcRoom>();
        private ThIfcRoom Space { get; set; }
        private List<ThIfcRoom> NeibourSpaces { get; set; }
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        private double ClosedDis { get; set; }
        private ThFindSpaceRangedSpaces(ThIfcRoom space,List<ThIfcRoom> neibourSpaces, double closedDis=0.0)
        {
            Space = space;
            ClosedDis = closedDis;
            NeibourSpaces = neibourSpaces;
            DBObjectCollection dbObjs = new DBObjectCollection();
            NeibourSpaces.ForEach(o => dbObjs.Add(o.Boundary));
            SpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
        }
        public static ThFindSpaceRangedSpaces Find(ThIfcRoom space, List<ThIfcRoom> neibourSpaces,double closedDis=0.0)
        {
            var instance = new ThFindSpaceRangedSpaces(space, neibourSpaces, closedDis);
            instance.Find();
            return instance;
        }
        private void Find()
        {
            List<ThIfcRoom> results = new List<ThIfcRoom>();            
            var selObjs = SpatialIndex.SelectCrossingPolygon(Space.Boundary as Polyline);
            SearchSpaces.AddRange(NeibourSpaces.Where(o => selObjs.Contains(o.Boundary)));
            var outsideObjs = NeibourSpaces.Where(o => !selObjs.Contains(o.Boundary)).ToList();
            SearchSpaces.AddRange(outsideObjs.Where(o => IsCloseCurrentSpace(o)));
        }
        private bool IsCloseCurrentSpace(ThIfcRoom neibourSpace)
        {
            double dis = neibourSpace.Boundary.Distance(Space.Boundary);
            return dis <= ClosedDis;
        }
    }
}
