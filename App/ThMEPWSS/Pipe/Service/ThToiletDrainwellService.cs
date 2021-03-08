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
            else if(FindNeighbourDrainwell(ToiletSpace, ThWPipeCommon.TOILET_BUFFER_DISTANCE).Count>0)
            {
                Drainwells.AddRange(FindNeighbourDrainwell(ToiletSpace, ThWPipeCommon.TOILET_BUFFER_DISTANCE));
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
            var balconies = crossSpaces.Where(m =>( m.Tags.Count==0&& m.Boundary.Area<1e6)).ToList();
            //找到含有排水管井的阳台空间          
            //
            return balconies;
        }
    }
}
