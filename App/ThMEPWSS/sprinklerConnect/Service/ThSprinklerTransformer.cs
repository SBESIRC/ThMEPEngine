using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThCADExtension;
using ThMEPEngineCore.Algorithm;

namespace ThMEPWSS.SprinklerConnect.Service
{
    public  class ThSprinklerTransformer
    {
        public static ThMEPOriginTransformer GetTransformer(Point3dCollection pts)
        {
            var center = pts.Envelope().CenterPoint();
            var transformer = new ThMEPOriginTransformer(center);
            return transformer;
        }
    }
}
