using System.Linq;
using System.Collections.Generic;
using ThCADCore.NTS;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.Pipe.Service
{
    public class ThKitchenDrainwellService: ThDrainwellService
    {
        private ThIfcSpace KitchenSpace;      
        public ThKitchenDrainwellService():base()
        {
            Pypes = new List<ThIfcSpace>();
        }
        private ThKitchenDrainwellService(
            List<ThIfcSpace> spaces, 
            ThIfcSpace kitchenSpace, 
            ThCADCoreNTSSpatialIndex spaceSpatialIndex):base(spaces, spaceSpatialIndex)
        {
            Pypes = new List<ThIfcSpace>();
            KitchenSpace = kitchenSpace;
        }
        public static ThKitchenDrainwellService Find(
            List<ThIfcSpace> spaces, 
            ThIfcSpace kitchenSpace, 
            ThCADCoreNTSSpatialIndex spaceSpatialIndex=null)
        {
            var instance = new ThKitchenDrainwellService(spaces, kitchenSpace, spaceSpatialIndex);
            instance.Find();
            return instance;
        }
        private void Find()
        {
            bool goToFindNeibourDrainwell = false;
            var noTagSubSpaces = KitchenSpace.SubSpaces.Where(o => o.Tags.Count == 0).ToList();
            if (noTagSubSpaces.Count == 0)
            {
                goToFindNeibourDrainwell = true;
            }
            else if(noTagSubSpaces.Count == 1)
            {
                if(IsValidSpaceArea(noTagSubSpaces[0]))
                {
                    Drainwells.Add(noTagSubSpaces[0]);
                }
                else
                {
                    Pypes.Add(noTagSubSpaces[0]);
                    goToFindNeibourDrainwell = true;
                }
            }
            else if (noTagSubSpaces.Count == 2)
            {
                Drainwells.Add(noTagSubSpaces.OrderBy(o => o.Boundary.Area).First());
                Pypes.Add(noTagSubSpaces.OrderBy(o => o.Boundary.Area).Last());
            }
            else
            {
                goToFindNeibourDrainwell = true;
            }
            if(goToFindNeibourDrainwell)
            {
                if(FindNeighbourDrainwell(KitchenSpace, ThWPipeCommon.KITCHEN_BUFFER_DISTANCE).Count>0)
                {
                    Drainwells.AddRange(FindNeighbourDrainwell(KitchenSpace, ThWPipeCommon.KITCHEN_BUFFER_DISTANCE));
                }
                else
                {
                    Drainwells.AddRange(FindDrainwells());
                }          
            }
        }
        private List<ThIfcSpace> FindDrainwells()
        {
            List<ThIfcSpace> drainwellSpaces = FindNeighbourToiletDrainwells();
            if (drainwellSpaces.Count==0)
            {
                drainwellSpaces = FindNeighbourBalconyDrainwells();
            }
            return drainwellSpaces;
        }
        private List<ThIfcSpace> FindNeighbourToiletDrainwells()
        {
            List<ThIfcSpace> drainwellSpaces = new List<ThIfcSpace>();
            var neibourToilets = FindNeighbouringToiletWithDrainwell(KitchenSpace, ThWPipeCommon.KITCHEN_BUFFER_DISTANCE);
            if (neibourToilets.Count > 1)
            {
                //厨房相邻的卫生间有多个，异常，Dead
                return drainwellSpaces;
            }
            else if (neibourToilets.Count == 0)
            {
                //厨房附近没有卫生间
                return drainwellSpaces;
            }
            else if (neibourToilets.Count == 1)
            {
                var noTagSubSpaces = neibourToilets[0].SubSpaces.Where(o => o.Tags.Count == 0).ToList();
                if (noTagSubSpaces.Count > 1)
                {
                    //表示相邻卫生有多个没有Tag的空间(无效,继续查找)
                    return drainwellSpaces;
                }
                else
                {
                    drainwellSpaces.AddRange(FilterDistancedDrainwells(KitchenSpace, noTagSubSpaces));
                }                
            }
            return drainwellSpaces;
        }
        private List<ThIfcSpace> FindNeighbourBalconyDrainwells()
        {
            List<ThIfcSpace> drainwellSpaces = new List<ThIfcSpace>();
            var neibourBalconies = FindNeighbouringBalconyWithDrainwell(KitchenSpace, ThWPipeCommon.KITCHEN_BUFFER_DISTANCE);
            if (neibourBalconies.Count > 1)
            {
                //厨房相邻的阳台有多个，异常，Dead
                return drainwellSpaces;
            }
            else if (neibourBalconies.Count == 0)
            {
                //厨房附近没有卫生间
                return drainwellSpaces;
            }
            else if (neibourBalconies.Count == 1)
            {
                var noTagSubSpaces = neibourBalconies[0].SubSpaces.Where(o => o.Tags.Count == 0).ToList();
                if (noTagSubSpaces.Count > 1)
                {
                    //表示相邻卫生有多个没有Tag的空间(无效,继续查找)
                    return drainwellSpaces;
                }
                else
                {
                    drainwellSpaces.AddRange(FilterDistancedDrainwells(KitchenSpace, noTagSubSpaces));
                }
            }
            return drainwellSpaces;
        }
        private bool IsValidSpaceArea(ThIfcSpace thIfcSpace)
        {
            double area= GetSpaceArea(thIfcSpace);
            return area <= ThWPipeCommon.WELLS_MAX_AREA;            
        }
        private double GetSpaceArea(ThIfcSpace thIfcSpace)
        {
            return thIfcSpace.Boundary.Area / (1000 * 1000);//mm单位
        }
        private List<ThIfcSpace> FindNeighbourDrainwell(ThIfcSpace space, double bufferDis)
        {
            //空间轮廓往外括500
            var bufferObjs = ThCADCoreNTSOperation.Buffer(space.Boundary as Polyline, bufferDis);
            if (bufferObjs.Count == 0)
            {
                return new List<ThIfcSpace>();
            }
            var crossObjs = SpaceSpatialIndex.SelectCrossingPolygon(bufferObjs[0] as Polyline);
            //获取偏移后，能框选到的空间
            var crossSpaces = Spaces.Where(o => crossObjs.Contains(o.Boundary));
            //找到含有阳台的空间
            var balconies = crossSpaces.Where(m => (m.Tags.Count==0&& IsValidSpaceArea(m))).ToList();
            //找到含有排水管井的阳台空间          
            //
            return balconies;
        }
    }
}
