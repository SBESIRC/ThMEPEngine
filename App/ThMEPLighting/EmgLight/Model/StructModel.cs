using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPLighting.EmgLight.Service;
using System.Collections.Generic;


namespace ThMEPLighting.EmgLight.Model
{
   public  class StructModel
    {
        public StructModel(Polyline polyline)
        {
            columnPoly = polyline;
            columnCenterPt = StructUtils.GetStructCenter(columnPoly);
        }

        public Polyline columnPoly { get; set; }

        public Point3d columnCenterPt { get; }

        public Dictionary <Point3d, Vector3d> layoutPoint { get; set; }

        //public Vector3d layoutDirection { get; set; }

        //public Circle protectRadius { get; set; }

       
    }
}
