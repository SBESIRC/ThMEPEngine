using System.Linq;
using System.Collections.Generic;
using ThCADCore.NTS;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.Pipe.Service
{
    public class ThToiletDrainwellService : ThDrainwellService
    {       
        private ThIfcSpace ToiletSpace;       
        private ThToiletDrainwellService() : base()
        {
        }
        private ThToiletDrainwellService(
            List<ThIfcSpace> spaces, 
            ThIfcSpace toiletSpace, 
            ThCADCoreNTSSpatialIndex spaceSpatialIndex):base(spaces, spaceSpatialIndex)
        {
            ToiletSpace = toiletSpace;
        }
        public static ThToiletDrainwellService Find(
            List<ThIfcSpace> spaces, 
            ThIfcSpace toiletSpace, 
            ThCADCoreNTSSpatialIndex spaceSpatialIndex=null)
        {
            var instance = new ThToiletDrainwellService(spaces, toiletSpace, spaceSpatialIndex);
            instance.Find();
            return instance;
        }
        private void Find()
        {
            var noTagSubSpaces = ToiletSpace.SubSpaces.Where(o => o.Tags.Count == 0).ToList();
            if (noTagSubSpaces.Count > 0)
            {
                //卫生间内有包含没有名字的空间
                //如果有一个（表示为排水管井），卫生间必然用这个
                //如果大于一个，表示绘制不合理，Dead
                Drainwells.AddRange(noTagSubSpaces);
            }
            else
            {
                var neibourBalconies = FindNeighbouringBalconyWithDrainwell(ToiletSpace, ThWPipeCommon.TOILET_BUFFER_DISTANCE);
                if (neibourBalconies.Count==1)
                {
                    //从相邻的阳台内部空间中
                    //查找只包含一个没有名字的空间(就认为是排水管井)
                    noTagSubSpaces = neibourBalconies[0].SubSpaces.Where(o => o.Tags.Count == 0).ToList();
                    if(noTagSubSpaces.Count>1)
                    {
                        Drainwells.AddRange(noTagSubSpaces);
                    }
                    else
                    {
                        Drainwells.AddRange(FilterDistancedDrainwells(ToiletSpace, noTagSubSpaces));
                    }
                }
            }
        }
    }
}
