using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ProtoBuf;
using System.Collections.Generic;
using ThMEPEngineCore.Model;

namespace ThMEPTCH.Model
{
    [ProtoContract]
    public class ThTCHWall : ThIfcWall
    {
        /// <summary>
        /// 宽度
        /// </summary>
        [ProtoMember(1)]
        public double Width { get; set; }
        /// <summary>
        /// 高度
        /// </summary>
        [ProtoMember(2)]
        public double Height { get; set; }
        /// <summary>
        /// 长度
        /// </summary>
        [ProtoMember(3)]
        public double Length { get; }
        /// <summary>
        /// 拉伸方向
        /// </summary>
        [ProtoMember(4)]
        public Vector3d ExtrudedDirection { get; private set; }
        /// <summary>
        /// 中线方向
        /// </summary>
        [ProtoMember(5)]
        public Vector3d XVector { get; }
        /// <summary>
        /// 中线中点
        /// </summary>
        [ProtoMember(6)]
        public Point3d Origin { get; }
        /// <summary>
        /// 门
        /// </summary>
        [ProtoMember(7)]
        public List<ThTCHDoor> Doors { get; private set; }
        /// <summary>
        /// 窗
        /// </summary>
        [ProtoMember(8)]
        public List<ThTCHWindow> Windows { get; private set; }
        /// <summary>
        /// 开洞
        /// </summary>
        [ProtoMember(9)]
        public List<ThTCHOpening> Openings { get; private set; }

        private ThTCHWall()
        {

        }
        public ThTCHWall(Point3d startPt,Point3d endPt,double width,double height) 
        {
            Init();
            Width = width;
            Height = height;
            Length = startPt.DistanceTo(endPt);
            XVector = (endPt - startPt).GetNormal();
            Origin = startPt + XVector.MultiplyBy(Length / 2);
        }
        public ThTCHWall(Polyline outPline, double height) 
        {
            Init();
            XVector = Vector3d.XAxis;
            Outline = outPline;
            Height = height;
        }
        void Init()
        {
            Doors = new List<ThTCHDoor>();
            Windows = new List<ThTCHWindow>();
            Openings = new List<ThTCHOpening>();
            ExtrudedDirection = Vector3d.ZAxis;
        }
    }
}
