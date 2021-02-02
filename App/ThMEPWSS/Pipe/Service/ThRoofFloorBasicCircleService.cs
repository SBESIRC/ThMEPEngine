using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Pipe.Service
{
    public class ThRoofFloorBaseCircleService
    {
        private List<ThIfcSpace> BaseCircles { get; set; }
        private ThIfcSpace Space { get; set; }
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        private ThRoofFloorBaseCircleService(
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
            var service = new ThRoofFloorBaseCircleService(space, baseCircles);
            return service.Find(space);
        }
        private List<ThIfcSpace> Find(ThIfcSpace FirstFloorSpace)
        {
            var TagSubSpaces = FirstFloorSpace.SubSpaces.ToList();
            if (TagSubSpaces.Count != 0)
            {         
                foreach(var TagSubSpace in TagSubSpaces)    
                if (IsValidSpaceArea(TagSubSpace))
                {
                   BaseCircles.Add(TagSubSpace);
                    break;
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
