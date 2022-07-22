using Autodesk.AutoCAD.DatabaseServices;
using ProtoBuf;
using System;
using System.Collections.Generic;

namespace ThMEPTCH.Model
{
    [ProtoContract]
    [ProtoInclude(100, typeof(ThTCHProject))]
    [ProtoInclude(101, typeof(ThTCHSite))]
    [ProtoInclude(102, typeof(ThTCHBuilding))]
    [ProtoInclude(103, typeof(ThTCHBuildingStorey))]
    [ProtoInclude(104, typeof(ThTCHWall))]
    [ProtoInclude(105, typeof(ThTCHDoor))]
    [ProtoInclude(106, typeof(ThTCHSlab))]
    [ProtoInclude(107, typeof(ThTCHWindow))]
    [ProtoInclude(108, typeof(ThTCHRailing))]
    [ProtoInclude(109, typeof(ThTCHOpening))]
    public class ThTCHElement
    {
        /*这里预留10个序列数据，外部序列数字冲11开始*/
        [ProtoMember(1)]
        public string Name { get; set; }
        public string Spec { get; set; }
        public string Useage { get; set; }
        [ProtoMember(2)]
        public string Uuid { get; set; }
        [ProtoMember(3)]
        public Entity Outline { get; set; }
        [ProtoMember(4)]
        public double Height { get; set; }
        public Dictionary<string, object> Properties { get; }
        public ThTCHElement()
        {
            Uuid = Guid.NewGuid().ToString();
            Properties = new Dictionary<string, object>();
        }
    }
}
