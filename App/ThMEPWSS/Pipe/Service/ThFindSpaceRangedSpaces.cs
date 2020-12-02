using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.Pipe.Service
{
    public class ThFindSpaceRangedSpaces
    {
        public List<ThIfcSpace> SearchSpaces = new List<ThIfcSpace>();
        private ThIfcSpace Space { get; set; }
        private List<ThIfcSpace> NeibourSpaces { get; set; }
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        private double ClosedDis { get; set; }
        private ThFindSpaceRangedSpaces(ThIfcSpace space,List<ThIfcSpace> neibourSpaces, double closedDis=0.0)
        {
            Space = space;
            ClosedDis = closedDis;
            NeibourSpaces = neibourSpaces;
            DBObjectCollection dbObjs = new DBObjectCollection();
            NeibourSpaces.ForEach(o => dbObjs.Add(o.Boundary));
            SpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
        }
        public static ThFindSpaceRangedSpaces Find(ThIfcSpace space, List<ThIfcSpace> neibourSpaces,double closedDis=0.0)
        {
            var instance = new ThFindSpaceRangedSpaces(space, neibourSpaces, closedDis);
            instance.Find();
            return instance;
        }
        private void Find()
        {
            List<ThIfcSpace> results = new List<ThIfcSpace>();            
            var selObjs = SpatialIndex.SelectCrossingPolygon(Space.Boundary as Polyline);
            SearchSpaces.AddRange(NeibourSpaces.Where(o => selObjs.Contains(o.Boundary)));
            var outsideObjs = NeibourSpaces.Where(o => !selObjs.Contains(o.Boundary)).ToList();
            SearchSpaces.AddRange(outsideObjs.Where(o => IsCloseCurrentSpace(o)));
        }
        private bool IsCloseCurrentSpace(ThIfcSpace neibourSpace)
        {
            double dis = neibourSpace.Boundary.Distance(Space.Boundary);
            return dis <= ClosedDis;
        }
    }
}
