﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.CAD;


using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Model.Hvac;
using ThMEPWSS.HydrantLayout.tmp.Model;
using ThMEPWSS.HydrantLayout.Model;
using NFox.Cad;

namespace ThMEPWSS.HydrantLayout.tmp.Service
{
    class CreateBoundaryService
    {
        public static void FindLineOfRectangle(Polyline rec ,ref double shortside,ref double longside)
        {
            var tol = new Tolerance(10, 10);

            Point3d pt1 = rec.GetPoint3dAt(0);
            Point3d pt2 = rec.GetPoint3dAt(0);
            Point3d pt3 = rec.GetPoint3dAt(0);

            Vector3d vec1 = pt2 - pt1;
            Vector3d vec2 = pt3 - pt2;
            if (vec1.Length > vec2.Length)
            {
                longside = vec1.Length;
                shortside = vec2.Length;
            }
            else 
            {
                longside = vec2.Length;
                shortside = vec1.Length;
            }
        }

        public static Polyline CreateBoundary(Point3d center, double shortSide, double longSide, Vector3d dir)
        {
            var tol = new Tolerance(10, 10);
            var shortDir = dir.GetNormal();
            var longDir = new Vector3d(-shortDir.Y, shortDir.X, 0);

            //if (shortSide.StartPoint.IsEqualTo(basePt, tol))
            //{
            //    shortDir = -shortDir;
            //}
            //var longDir = (longSide.EndPoint - longSide.StartPoint).GetNormal();
            //if (longSide.EndPoint.IsEqualTo(basePt, tol))
            //{
            //    longDir = -longDir;
            //}

            //openDir = -1;
            //if (shortDir.GetAngleTo(longDir, Vector3d.ZAxis) <= Math.PI)
            //{
            //    openDir = 0;
            //}
            //else
            //{
            //    openDir = 1;
            //}

            var pt1 = center + 0.5 * shortSide * shortDir + 0.5 * longDir * longSide;
            var pt2 = pt1 - shortDir * shortSide;
            var pt3 = pt2 - longDir * longSide;
            var pt4 = pt3 + shortDir * shortSide;
            var boundary = new Polyline();
            boundary.Closed = true;

            boundary.AddVertexAt(0, pt4.ToPoint2D(), 0, 0, 0);
            boundary.AddVertexAt(1, pt1.ToPoint2D(), 0, 0, 0);
            boundary.AddVertexAt(2, pt2.ToPoint2D(), 0, 0, 0);
            boundary.AddVertexAt(3, pt3.ToPoint2D(), 0, 0, 0);

            return boundary;
        }

    }
}
