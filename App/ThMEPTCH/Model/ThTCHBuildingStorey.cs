using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.Model;

namespace ThMEPTCH.Model
{
    public class ThTCHBuildingStorey : ThIfcBuildingStorey
    {
        /// <summary>
        /// 编号
        /// </summary>
        public string Number { get; set; }
        /// <summary>
        /// 标高
        /// </summary>
        public double Elevation { get; set; }
        /// <summary>
        /// 层高
        /// </summary>
        public double Height { get; set; }
        /// <summary>
        /// 基点
        /// </summary>
        public Point3d Origin { get; set; }
        public ThTCHBuildingStorey()
        {
            Walls = new List<ThTCHWall>();
            Doors = new List<ThTCHDoor>();
            Slabs = new List<ThTCHSlab>();
            Windows = new List<ThTCHWindow>();
            Railings = new List<ThTCHRailing>();
        }
        public List<ThTCHWall> Walls { get; set; }
        public List<ThTCHWindow> Windows { get; set; }
        public List<ThTCHDoor> Doors { get; set; }
        public List<ThTCHSlab> Slabs { get; set; }
        public List<ThTCHRailing> Railings { get; set; }
    }
}
