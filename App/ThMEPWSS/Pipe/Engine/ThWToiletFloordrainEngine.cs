using System;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;

namespace ThMEPWSS.Pipe.Engine
{
    public class ThWToiletFloordrainEngine : IDisposable
    {
        public void Dispose()
        {
        }
        public Point3dCollection Floordrain { get; set; }
        public ThWToiletFloordrainEngine()
        {
            Floordrain = new Point3dCollection();
        }
        public void Run(List<BlockReference> tfloordrain, Polyline tboundary)
        {   for (int i = 0; i < tfloordrain.Count; i++)
            {
                if (Isinside(tfloordrain[i], tboundary))
                {
                    Floordrain.Add(tfloordrain[i].Position);
                }
            }
        }
        private static bool Isinside(BlockReference tfloordrain_, Polyline tboundary)
        {
            var pts = new Point3dCollection();
            var basepoint = tfloordrain_.Position;
            var center = tboundary.GetCenter();
            Line line = new Line(center, basepoint);
            tboundary.IntersectWith(line, Intersect.ExtendArgument, pts, (IntPtr)0, (IntPtr)0);
            if ((pts[0].GetVectorTo(basepoint)).IsCodirectionalTo(basepoint.GetVectorTo(pts[1])))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
      
    }
}
