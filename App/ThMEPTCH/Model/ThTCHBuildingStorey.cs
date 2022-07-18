using Autodesk.AutoCAD.Geometry;
using ProtoBuf;
using System.Collections.Generic;
using ThMEPEngineCore.Model;

namespace ThMEPTCH.Model
{
    [ProtoContract]
    public class ThTCHBuildingStorey : ThIfcBuildingStorey
    {
        [ProtoMember(10)]
        public string MemoryStoreyId { get; set; }
        [ProtoMember(11)]
        public Matrix3d MemoryMatrix3d { get; set; }
        /// <summary>
        /// 编号
        /// </summary>
        [ProtoMember(1)]
        public string Number { get; set; }
        /// <summary>
        /// 标高
        /// </summary>
        [ProtoMember(2)]
        public double Elevation { get; set; }
        /// <summary>
        /// 层高
        /// </summary>
        [ProtoMember(3)]
        public double Height { get; set; }
        /// <summary>
        /// 基点
        /// </summary>
        [ProtoMember(4)]
        public Point3d Origin { get; set; }
        public ThTCHBuildingStorey()
        {
            Walls = new List<ThTCHWall>();
            Doors = new List<ThTCHDoor>();
            Slabs = new List<ThTCHSlab>();
            Windows = new List<ThTCHWindow>();
            Railings = new List<ThTCHRailing>();
            MemoryStoreyId = null;
        }
        [ProtoMember(5)]
        public List<ThTCHWall> Walls { get; set; }
        [ProtoMember(6)]
        public List<ThTCHWindow> Windows { get; set; }
        [ProtoMember(7)]
        public List<ThTCHDoor> Doors { get; set; }
        [ProtoMember(8)]
        public List<ThTCHSlab> Slabs { get; set; }
        [ProtoMember(9)]
        public List<ThTCHRailing> Railings { get; set; }
    }
}
