using System;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Pipe
{
    public class ThWCompositePipeEngine : IDisposable
    {
        public void Dispose()
        {
        }
        public ThWToiletPipeEngine ToiletPipeEngine { get; set; }
        public ThWKitchenPipeEngine KitchenPipeEngine { get; set; }

        public Point3dCollection KitchenPipes
        {
            get
            {
                return KitchenPipeEngine.Pipes;
            }
        }

        public Point3dCollection ToiletPipes
        {
            get
            {
                return ToiletPipeEngine.Pipes;
            }
        }

        public ThWCompositePipeEngine(ThWKitchenPipeEngine kitchenPipeEngine, ThWToiletPipeEngine toiletPipeEngine)
        {
            ToiletPipeEngine = toiletPipeEngine;
            KitchenPipeEngine = kitchenPipeEngine;
        }

        public void Run(Polyline boundary, Polyline outline, BlockReference basinline, Polyline pype,Polyline boundary1, Polyline outline1, Polyline urinal)
        {
            KitchenPipeEngine.Run(boundary, outline, basinline, pype);
            ToiletPipeEngine.Run(boundary1, outline1, urinal);
        }
    }
}
