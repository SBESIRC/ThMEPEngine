using System.Collections.Generic;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Pipe.Model
{
    /// <summary>
    /// 顶层
    /// </summary>
    public class ThWTopFloorRoom : ThIfcRoom
    {
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
        /// <summary>
        /// 分割线
        /// </summary>
        public List<Line> DivisionLines { get; set; }
        public ThWTopFloorRoom()
        {
            BaseCircles = new List<ThIfcSpace>();
            CompositeBalconyRooms = new List<ThWCompositeBalconyRoom>();
            CompositeRooms = new List<ThWCompositeRoom>();
            DivisionLines = new List<Line>();
        }
    }
}