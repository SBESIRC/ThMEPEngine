using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;

namespace ThMEPArchitecture.ParkingStallArrangement.Method
{
    public static class CircleTools
    {
        public static Circle CreateCircle(Point3d Center, double Radius)
        {
            var circle = new Circle();
            circle.Center = Center;
            circle.Radius = Radius;
            return circle;
        }
    }
}
