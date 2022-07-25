using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ProtoBuf;
using System;
using System.Collections.Generic;

namespace ThMEPTCH.Model
{
    [ProtoContract]
    public class ThTCHWall : ThTCHElement, ICloneable
    {
        /// <summary>
        /// 门
        /// </summary>
        [ProtoMember(21)]
        public List<ThTCHDoor> Doors { get; private set; }
        /// <summary>
        /// 窗
        /// </summary>
        [ProtoMember(22)]
        public List<ThTCHWindow> Windows { get; private set; }
        /// <summary>
        /// 开洞
        /// </summary>
        [ProtoMember(23)]
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

        public object Clone()
        {
            if (this == null)
                return null;
            ThTCHWall cloneWall = null;
            if (Outline != null)
            {
                if (this.Outline is Polyline polyline) 
                {
                    var cloneLine = polyline.Clone() as Polyline;
                    cloneWall = new ThTCHWall(cloneLine, this.Height);
                }
            }
            else 
            {
                var sp = this.Origin - this.XVector.MultiplyBy(Length / 2);
                var ep = this.Origin + this.XVector.MultiplyBy(Length/2);
                cloneWall = new ThTCHWall(sp, ep,this.Width,this.Height);
            }
            if (cloneWall != null) 
            {
                foreach (var item in this.Openings) 
                {
                    cloneWall.Openings.Add(item.Clone() as ThTCHOpening);
                }
                foreach (var item in this.Doors)
                {
                    cloneWall.Doors.Add(item.Clone() as ThTCHDoor);
                }
                foreach (var item in this.Windows)
                {
                    cloneWall.Windows.Add(item.Clone() as ThTCHWindow);
                }
            }
            return cloneWall;
        }
    }
}
