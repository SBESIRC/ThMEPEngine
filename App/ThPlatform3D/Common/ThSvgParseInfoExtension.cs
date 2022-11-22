using System.Linq;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.IO.SVG;
using ThPlatform3D.StructPlane.Service;

namespace ThPlatform3D.Common
{
    public static class ThSvgParseInfoExtension
    {
        public static void MoveToOrigin(this ThSvgParseInfo parseInfo)
        {
            var originOffset = parseInfo.DocProperties.GetOriginOffset();
            var vector = originOffset.OffsetVector().Negate();
            if(vector.Length==0.0)
            {
                return;
            }
            else
            {
                var mt = Matrix3d.Displacement(vector);
                parseInfo.ComponentInfos.ForEach(c => c.Transform(mt));
                parseInfo.Geos.Where(o => o.Boundary != null).ForEach(o => o.Boundary.TransformBy(mt));
            }
        }

        public static Vector3d OffsetVector(this string originOffset)
        {
            if(string.IsNullOrEmpty(originOffset))
            {
                return new Vector3d();
            }
            else
            {
                var xy = originOffset.GetDoubles();
                if (xy.Count == 2)
                {
                    return new Vector3d(xy[0], xy[1], 0.0);
                }
                else
                {
                    return new Vector3d();
                }
            }
        }
    }
}
