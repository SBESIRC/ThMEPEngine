using System;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPTCH.Model
{
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
