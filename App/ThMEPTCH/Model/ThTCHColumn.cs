using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ProtoBuf;
using System;
using System.Collections.Generic;

namespace ThMEPTCH.Model
{
    [ProtoContract]
    public class ThTCHColumn : ThTCHElement, ICloneable
    {
        private ThTCHColumn()
        {

        }
        public ThTCHColumn(Point3d startPt, Point3d endPt, double width, double height)
        {
            Init();
            Width = width;
            Height = height;
            Length = startPt.DistanceTo(endPt);
            XVector = (endPt - startPt).GetNormal();
            Origin = startPt + XVector.MultiplyBy(Length / 2);
        }
        public ThTCHColumn(Polyline outPline, double height)
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
            ThTCHColumn cloneColumn = null;
            if (Outline != null)
            {
                if (this.Outline is Polyline polyline)
                {
                    var cloneLine = polyline.Clone() as Polyline;
                    cloneColumn = new ThTCHColumn(cloneLine, this.Height);
                }
            }
            else
            {
                var sp = this.Origin - this.XVector.MultiplyBy(Length / 2);
                var ep = this.Origin + this.XVector.MultiplyBy(Length/2);
                cloneColumn = new ThTCHColumn(sp, ep, this.Width, this.Height);
            }
            if (cloneColumn != null)
            {
                cloneColumn.Uuid = this.Uuid;
            }
            return cloneColumn;
        }
    }
}
