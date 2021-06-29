using System.Linq;
using ThMEPEngineCore.CAD;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Hydrant.Service
{
    public class ThRoomSplitAreaBelongedService
    {
        private List<Entity> SplitAreas { get; set; }
        private List<Entity> CoverAreas { get; set; }
        private double PolygonBufferLength = 5.0;
        public Dictionary<Entity, List<Entity>> Results { get; private set; }
        public ThRoomSplitAreaBelongedService(List<Entity> splitAreas, List<Entity> coverAreas)
        {
            SplitAreas = splitAreas;
            CoverAreas = coverAreas;
            Results = new Dictionary<Entity, List<Entity>>();
        }
        public void Classify()
        {
            //对分割的Polygons进行内缩,用于判断哪些区域属于房间
            var polygonBufferDic =  ThHydrantUtils.BufferPolygon(SplitAreas, -1.0 * PolygonBufferLength);
            SplitAreas.ForEach(o =>
            {
                var belongedAreas = BelongedProtectArea(polygonBufferDic[o]);
                Results.Add(o, belongedAreas);
            });
        }        
        private List<Entity> BelongedProtectArea(Entity splitArea)
        {
            return CoverAreas.Where(o => o.IsContains(splitArea)).ToList();
        }
    }
}
