using Autodesk.AutoCAD.Geometry;
using ProtoBuf;
using System.Collections.Generic;
using ThMEPEngineCore.Model;

namespace ThMEPTCH.Model
{
    [ProtoContract]
    public class ThTCHBuildingStorey : ThTCHElement
    {
        [ProtoMember(98)]
        public string MemoryStoreyId { get; set; }
        [ProtoMember(99)]
        public Matrix3d MemoryMatrix3d { get; set; }
        /// <summary>
        /// 编号
        /// </summary>
        [ProtoMember(11)]
        public string Number { get; set; }
        /// <summary>
        /// 标高
        /// </summary>
        [ProtoMember(12)]
        public double Elevation { get; set; }
        /// <summary>
        /// 基点
        /// </summary>
        [ProtoMember(13)]
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
        [ProtoMember(14)]
        public List<ThTCHWall> Walls { get; set; }
        [ProtoMember(15)]
        public List<ThTCHWindow> Windows { get; set; }
        [ProtoMember(16)]
        public List<ThTCHDoor> Doors { get; set; }
        [ProtoMember(17)]
        public List<ThTCHSlab> Slabs { get; set; }
        [ProtoMember(18)]
        public List<ThTCHRailing> Railings { get; set; }
    }
}
