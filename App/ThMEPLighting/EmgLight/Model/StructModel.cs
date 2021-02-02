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
            geometry = polyline;
            centerPt = StructUtils.GetStructCenter(geometry);

        }

        public Polyline geometry { get;}

        public Point3d centerPt { get; }

        public Vector3d layoutDirection { get; set; }

        //private  Point3d GetStructCenter(Polyline polyline)
        //{
        //    List<Point3d> points = new List<Point3d>();
        //    for (int i = 0; i < polyline.NumberOfVertices; i++)
        //    {
        //        points.Add(polyline.GetPoint3dAt(i));
        //    }

        //    double maxX = points.Max(x => x.X);
        //    double minX = points.Min(x => x.X);
        //    double maxY = points.Max(x => x.Y);
        //    double minY = points.Min(x => x.Y);

        //    return new Point3d((maxX + minX) / 2, (maxY + minY) / 2, 0);
        //}

        //public Circle protectRadius { get; set; }


    }
}
