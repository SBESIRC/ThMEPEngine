using System.Collections.Generic;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.Pipe.Model
{
    public class ThWTopFloorRoom : ThWRoom
    {
        /// <summary>
        /// 顶层空间
        /// </summary>
        public ThIfcSpace FirstFloor { get; set; }
        /// <summary>
        /// 基点圆
        /// </summary>
        public List<ThIfcSpace> BaseCircles { get; set; }
        /// <summary>
        /// 组合阳台空间
        /// </summary>
        public List<ThWCompositeBalconyRoom> CompositeBalconyRooms { get; set; }
        /// <summary>
        /// 组合厨卫空间
        /// </summary>
        public List<ThWCompositeRoom> CompositeRooms { get; set; }
        public ThWTopFloorRoom()
        {
            FirstFloor = null;
            BaseCircles = new List<ThIfcSpace>();
            CompositeBalconyRooms = new List<ThWCompositeBalconyRoom>();
            CompositeRooms = new List<ThWCompositeRoom>();
        }
    }
}