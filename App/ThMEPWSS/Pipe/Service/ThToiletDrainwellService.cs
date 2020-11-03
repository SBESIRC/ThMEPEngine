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
    public class ThToiletDrainwellService
    {
        public  ThIfcSpace Drainwell { get; set; } //目前只支持查找一个
        private List<ThIfcSpace> Spaces;
        private ThIfcSpace ToiletSpace;
        private ThCADCoreNTSSpatialIndex SpaceSpatialIndex;
        private const double ToiletBufferDistance = 500.0;
        public bool IsFinded
        {
            get
            {
                return Drainwell != null;
            }
        }
        private ThToiletDrainwellService(
            List<ThIfcSpace> spaces, 
            ThIfcSpace toiletSpace, 
            ThCADCoreNTSSpatialIndex spaceSpatialIndex)
        {
            Spaces = spaces;
            ToiletSpace = toiletSpace;
            SpaceSpatialIndex = spaceSpatialIndex;
            if(SpaceSpatialIndex==null)
            {
                DBObjectCollection dbObjs = new DBObjectCollection();
                Spaces.ForEach(o => dbObjs.Add(o.Boundary));
                SpaceSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
            }
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
                Drainwell = noTagSubSpaces.Count == 1 ? noTagSubSpaces[0] : null;
            }
            else
            {
                var neibourBalcony = FindToiletNeighbouringbalconyWithDrainwell(ToiletSpace);
                if (neibourBalcony != null)
                {
                    //从相邻的阳台内部空间中
                    //查找只包含一个没有名字的空间(就认为是排水管井)
                    noTagSubSpaces = neibourBalcony.SubSpaces.Where(o => o.Tags.Count == 0).ToList();
                    Drainwell = noTagSubSpaces.Count == 1 ? noTagSubSpaces[0] : null;
                }
            }
        }
        /// <summary>
        /// 找到卫生间相邻的且含有排水管井的阳台
        /// </summary>
        /// <param name="toiletSpace"></param>
        /// <returns></returns>
        private ThIfcSpace FindToiletNeighbouringbalconyWithDrainwell(ThIfcSpace toiletSpace)
        {
            //卫生间轮廓往外括500
            var bufferObjs = ThCADCoreNTSOperation.Buffer(toiletSpace.Boundary as Polyline, ToiletBufferDistance);
            if (bufferObjs.Count == 0)
            {
                return null;
            }
            var crossObjs = SpaceSpatialIndex.SelectCrossingPolygon(bufferObjs[0] as Polyline);
            //获取偏移后，能框选到的空间
            var crossSpaces = Spaces.Where(o => crossObjs.Contains(o.Boundary));
            //找到含有阳台的空间
            var balconies = crossSpaces.Where(m => m.Tags.Where(n => n.Contains("阳台")).Any());
            //找到含有排水管井的阳台空间
            var incluedrainwellBalconies = balconies.Where(m => m.SubSpaces.Where(n => n.Tags.Count == 0).Any()).ToList();
            //
            return incluedrainwellBalconies.Count == 1 ? incluedrainwellBalconies[0] : null;
        }
    }
}
