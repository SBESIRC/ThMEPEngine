using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.BuildRoom.Interface
{
    public interface IRoomFilter
    {
        /// <summary>
        /// 未被过滤的轮廓
        /// </summary>
        List<Entity> Results { get; }
        /// <summary>
        /// 把在房间边界上的轮廓过滤掉
        /// </summary>
        /// <param name="boundaries">房间边界</param>
        /// <param name="generatedPolygons">通过Polygonize加工生成的轮廓</param>
        void Filter(List<Entity> boundaries,List<Entity> generatedPolygons);
    }
}
