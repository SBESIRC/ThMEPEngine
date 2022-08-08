using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ProtoBuf;
using System;
using System.Collections.Generic;

namespace ThMEPTCH.Model
{
    [ProtoContract]
    public class ThTCHBeam : ThTCHElement, ICloneable
    {
        private ThTCHBeam()
        {

        }
        public ThTCHBeam(Point3d startPt, Point3d endPt, double width, double height)
        {
            Init();
            Width = width;
            Height = height;
            Length = startPt.DistanceTo(endPt);
            XVector = (endPt - startPt).GetNormal();
            Origin = startPt + XVector.MultiplyBy(Length / 2);
        }
        public ThTCHBeam(Polyline outPline, double height)
        {
            Init();
            XVector = Vector3d.XAxis;
            Outline = outPline;
            Height = height;
        }
        void Init()
        {
            ExtrudedDirection = Vector3d.ZAxis;
        }

        public object Clone()
        {
            if (this == null)
                return null;
            ThTCHBeam cloneBeam = null;
            if (Outline != null)
            {
                if (this.Outline is Polyline polyline)
                {
                    var cloneLine = polyline.Clone() as Polyline;
                    cloneBeam = new ThTCHBeam(cloneLine, this.Height);
                }
            }
            else
            {
                var sp = this.Origin - this.XVector.MultiplyBy(Length / 2);
                var ep = this.Origin + this.XVector.MultiplyBy(Length/2);
                cloneBeam = new ThTCHBeam(sp, ep, this.Width, this.Height);
            }
            if (cloneBeam != null)
            {
                cloneBeam.Uuid = this.Uuid;
            }
            return cloneBeam;
        }
    }
}
