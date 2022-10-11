using System;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPTCH.PropertyServices.PropertyEnums;

namespace ThMEPTCH.Model
{
    public class ThTCHWall : ThTCHElement, ICloneable
    {
        /// <summary>
        /// 门
        /// </summary>
        public List<ThTCHDoor> Doors { get; private set; }
        /// <summary>
        /// 窗
        /// </summary>
        public List<ThTCHWindow> Windows { get; private set; }
        /// <summary>
        /// 开洞
        /// </summary>
        public List<ThTCHOpening> Openings { get; private set; }

        private ThTCHWall()
        {

        }

        public ThTCHWall(Point3d startPt, Point3d endPt, double width, double height)
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
            EnumMaterial = EnumTCHWallMaterial.Aeratedconcrete.GetDescription();
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
                    cloneWall.Width = this.Width;
                }
            }
            else
            {
                var sp = this.Origin - this.XVector.MultiplyBy(Length / 2);
                var ep = this.Origin + this.XVector.MultiplyBy(Length / 2);
                cloneWall = new ThTCHWall(sp, ep, this.Width, this.Height);
            }
            if (cloneWall != null)
            {
                cloneWall.Uuid = this.Uuid;
                cloneWall.EnumMaterial = this.EnumMaterial;
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
