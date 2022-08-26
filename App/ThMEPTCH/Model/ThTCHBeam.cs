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
        public ThTCHBeam(double width, double length, double height, Vector3d xvector, Point3d origin)
        {
            Init();
            Width = width;
            Length = length;
            Height = height;
            XVector = xvector;
            Origin = origin;
        }

        void Init()
        {
            ExtrudedDirection = Vector3d.ZAxis;
        }

        public object Clone()
        {
            if (this == null)
                return null;
            ThTCHBeam cloneBeam = new ThTCHBeam(Width, Length, Height, XVector, Origin);
            if (cloneBeam != null)
            {
                cloneBeam.Uuid = this.Uuid;
            }
            return cloneBeam;
        }
    }
}
