﻿using Autodesk.AutoCAD.Geometry;
using ProtoBuf;
using System.Collections.Generic;

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
        [ProtoMember(21)]
        public string Number { get; set; }
        /// <summary>
        /// 标高
        /// </summary>
        [ProtoMember(22)]
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
        [ProtoMember(31)]
        public List<ThTCHWall> Walls { get; set; }
        [ProtoMember(32)]
        public List<ThTCHWindow> Windows { get; set; }
        [ProtoMember(33)]
        public List<ThTCHDoor> Doors { get; set; }
        [ProtoMember(34)]
        public List<ThTCHSlab> Slabs { get; set; }
        [ProtoMember(35)]
        public List<ThTCHRailing> Railings { get; set; }
        [ProtoMember(36)]
        public List<ThTCHColumn> Columns { get; set; }
        [ProtoMember(37)]
        public List<ThTCHBeam> Beams { get; set; }
        [ProtoMember(38)]
        public List<ThTCHSpace> Rooms { get; set; }
    }
}
