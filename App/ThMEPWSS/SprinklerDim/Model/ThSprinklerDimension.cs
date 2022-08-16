using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.SprinklerDim.Model
{
    public class ThSprinklerDimension
    {
        public List<Point3d> DimPts { get; set; } = new List<Point3d>();
        public Vector3d Dirrection { get; set; } = new Vector3d();
        public double Distance { get; set; } = 0.0;

        public ThSprinklerDimension(List<Point3d> dimPts, Vector3d dirrection, double distance)
        {
            DimPts = dimPts;
            Dirrection = dirrection;
            Distance = distance;
        }

        public ThSprinklerDimension(List<Point3d> dimPts, double distance)
        {
            DimPts = dimPts;
            Distance = distance;
        }

    }
}
