using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Pipe.Service
{
    public class ThTopFloorBaseCircleService
    {
        private List<ThIfcSpace> BaseCircles { get; set; }
        private ThIfcSpace Space { get; set; }
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        private ThTopFloorBaseCircleService(
           ThIfcSpace space,
           List<ThIfcSpace> baseCircles)
        {
            BaseCircles = baseCircles;
            Space = space;
            var objs = new DBObjectCollection();
            SpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
        }
        public static List<ThIfcSpace> Find(
            ThIfcSpace space,
            List<ThIfcSpace> baseCircles)
        {
            var service = new ThTopFloorBaseCircleService(space, baseCircles);
            return service.Find(space);
        }
        private List<ThIfcSpace> Find(ThIfcSpace FirstFloorSpace)
        {
            BaseCircles = new List<ThIfcSpace>();
            var noTagSubSpaces = FirstFloorSpace.SubSpaces.Where(o => o.Tags.Count == 0).ToList();
            if (noTagSubSpaces.Count != 0)
            {
                if (noTagSubSpaces.Count == 1)
                {
                    if (IsValidSpaceArea(noTagSubSpaces[0]))
                    {
                        BaseCircles.Add(noTagSubSpaces[0]);
                    }
                }
                else
                {
                    BaseCircles.Add(noTagSubSpaces.OrderBy(o => o.Boundary.Area).First());
                }
            }
            return BaseCircles;
        }
        private bool IsValidSpaceArea(ThIfcSpace thIfcSpace)
        {
            double area = GetSpaceArea(thIfcSpace);
            return area >= ThWPipeCommon.MAX_BASECIRCLE_AREA;
        }
        private double GetSpaceArea(ThIfcSpace thIfcSpace)
        {
            return thIfcSpace.Boundary.Area / (1000 * 1000);
        }
    }
}

