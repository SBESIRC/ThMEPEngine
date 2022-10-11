using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;

namespace ThMEPTCH.Model
{
    public class ThTCHBuildingStorey : ThTCHElement
    {
        public string MemoryStoreyId { get; set; }
        public Matrix3d MemoryMatrix3d { get; set; }
        /// <summary>
        /// 编号
        /// </summary>
        public string Number { get; set; }
        /// <summary>
        /// 标高
        /// </summary>
        public double Elevation { get; set; }
        public ThTCHBuildingStorey()
        {
            Walls = new List<ThTCHWall>();
            Columns = new List<ThTCHColumn>();
            Beams = new List<ThTCHBeam>();
            Doors = new List<ThTCHDoor>();
            Slabs = new List<ThTCHSlab>();
            Windows = new List<ThTCHWindow>();
            Railings = new List<ThTCHRailing>();
            Rooms = new List<ThTCHSpace>();
            MemoryStoreyId = null;
        }
        public List<ThTCHWall> Walls { get; set; }
        public List<ThTCHWindow> Windows { get; set; }
        public List<ThTCHDoor> Doors { get; set; }
        public List<ThTCHSlab> Slabs { get; set; }
        public List<ThTCHRailing> Railings { get; set; }
        public List<ThTCHColumn> Columns { get; set; }
        public List<ThTCHBeam> Beams { get; set; }
        public List<ThTCHSpace> Rooms { get; set; }
    }
}
